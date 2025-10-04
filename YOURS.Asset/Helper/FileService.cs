using System.Drawing;
using System.Drawing.Imaging;

public interface IFileService
{
  Task<string?> CopyAsync(string directory, string sourceFilePath);
  Task DeleteAsync(string directory, string fileName);
  bool Exists(string directory, string fileName);
  Task MoveAsync(string sourceDirectory, string targetDirectory, string fileName);
  Task<string?> SaveAsync(IFormFile file, string directory, int quality = 100);
  Task<string?> SaveAsync(Bitmap bitmap, string directory, int quality = 100);
}

public class FileService : IFileService
{
  private readonly IWebHostEnvironment _env;

  public FileService(IWebHostEnvironment env)
  {
    _env = env;
  }

  internal static class FilesHelper
  {
    public enum FileType
    {
      Unknown,
      Jpeg,
      Bmp,
      Gif,
      Png,
      Pdf
    }

    private static readonly Dictionary<FileType, byte[]> KNOWN_FILE_HEADERS = new Dictionary<FileType, byte[]>()
    {
      { FileType.Jpeg, new byte[]{ 0xFF, 0xD8 }}, // JPEG
		  { FileType.Bmp, new byte[]{ 0x42, 0x4D }}, // BMP
		  { FileType.Gif, new byte[]{ 0x47, 0x49, 0x46 }}, // GIF
		  { FileType.Png, new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }}, // PNG
		  { FileType.Pdf, new byte[]{ 0x25, 0x50, 0x44, 0x46 }} // PDF
	  };

    public static FileType GetKnownFileType(ReadOnlySpan<byte> data)
    {
      foreach (var check in KNOWN_FILE_HEADERS)
      {
        if (data.Length >= check.Value.Length)
        {
          var slice = data.Slice(0, check.Value.Length);
          if (slice.SequenceEqual(check.Value))
          {
            return check.Key;
          }
        }
      }

      return FileType.Unknown;
    }
    public static bool IsBitmap(string filePath)
    {
      var data = File.ReadAllBytes(filePath);
      if (data.Length < KNOWN_FILE_HEADERS.Values.Max(x => x.Length))
      {
        return false;
      }

      var fileType = GetKnownFileType(data);
      return fileType != FileType.Unknown;
    }

    public static async Task<FileType> GetKnownFileTypeAsync(Stream data)
    {
      if (data.Length < KNOWN_FILE_HEADERS.Values.Max(x => x.Length))
      {
        return FileType.Unknown;
      }

      foreach (var check in KNOWN_FILE_HEADERS)
      {
        data.Seek(0, SeekOrigin.Begin);

        var slice = new byte[check.Value.Length];
        await data.ReadExactlyAsync(slice, 0, check.Value.Length);
        if (slice.SequenceEqual(check.Value))
        {
          return check.Key;
        }
      }

      data.Seek(0, SeekOrigin.Begin);

      return FileType.Unknown;
    }
    public static async Task<bool> IsBitmapAsync(string filePath)
    {
      using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
      {
        var fileType = await GetKnownFileTypeAsync(fs);
        return fileType != FileType.Unknown;
      }
    }
  }

  public async Task<string?> CopyAsync(string directory, string sourceFilePath)
  {
    var srcInfo = new FileInfo(BuildWebRootPath(sourceFilePath));
    if (srcInfo.Exists)
    {
      if (await FilesHelper.IsBitmapAsync(srcInfo.FullName))
      {
        return await CopyBitmapAsync(directory, sourceFilePath);
      }

      var fileName = $"{Guid.NewGuid()}.{Path.GetExtension(sourceFilePath).Trim('.')}";
      var dstInfo = new FileInfo(BuildWebRootPath(directory, fileName));

      await TimedCreateDirectoryAsync(dstInfo.FullName);

      await Task.Run(() => srcInfo.CopyTo(dstInfo.FullName, true));

      return fileName;
    }

    return null;
  }

  private async Task<string?> CopyBitmapAsync(string targetDirectory, string sourceFilePath)
  {
    var srcInfo = new FileInfo(BuildWebRootPath(sourceFilePath));
    if (srcInfo.Exists)
    {
      using (var bmp = LoadBitmap(srcInfo.FullName))
      {
        var dstInfo = new FileInfo(BuildWebRootPath(targetDirectory));
        return await SaveAsync(bmp, dstInfo.FullName);
      }
    }

    return null;
  }

  public Bitmap LoadBitmap(string filePath)
  {
    var bytes = File.ReadAllBytes(filePath);
    var ms = new MemoryStream(bytes);
    var img = Image.FromStream(ms);
    return (Bitmap)img;
  }

  public async Task DeleteAsync(string directory, string fileName)
  {
    var fileInfo = new FileInfo(BuildWebRootPath(directory, fileName));
    if (Exists(directory, fileName))
    {
      await Task.Run(() => fileInfo.Delete());
    }
  }

  public bool Exists(string directory, string fileName)
  {
    var fileInfo = new FileInfo(BuildWebRootPath(directory, fileName));
    return fileInfo.Exists;
  }

  public async Task MoveAsync(string sourceDirectory, string targetDirectory, string fileName)
  {
    var srcInfo = new FileInfo(BuildWebRootPath(sourceDirectory, fileName));
    var dstInfo = new FileInfo(BuildWebRootPath(targetDirectory, fileName));
    if (srcInfo.Exists)
    {
      await TimedCreateDirectoryAsync(dstInfo.FullName);
      await Task.Run(() => srcInfo.MoveTo(dstInfo.FullName, true));
    }
  }

  public async Task<string?> SaveAsync(IFormFile fromfile, string directory, int quality = 100)
  {
    var ext = Path.GetExtension(fromfile.FileName).Trim('.');
    var fileName = $"{Guid.NewGuid()}.{ext}";
    var filePath = BuildWebRootPath(directory, fileName);

    await TimedCreateDirectoryAsync(filePath);

    if (quality < 100)
    {
      using (var ms = new MemoryStream())
      {
        await fromfile.CopyToAsync(ms);
        ms.Position = 0;
        var fromfileData = ms.ToArray();
        using (var bmp = ByteArrayToBitmap(fromfileData))
        {
          var bmpData = CompressBitmapToByteArray(bmp);
          await SaveAsync(bmpData, filePath);
          return fileName;
        }
      }
    }
    else
    {
      using (FileStream fs = new FileStream(filePath, FileMode.CreateNew))
      {
        await fromfile.CopyToAsync(fs);
        return fileName;
      }
    }
  }

  public async Task<string?> SaveAsync(Bitmap? bitmap, string directory, int quality = 100)
  {
    if (bitmap == null)
      return null;

    var jpg = bitmap.PixelFormat == PixelFormat.Format24bppRgb &&
        bitmap.RawFormat.Guid == ImageFormat.Jpeg.Guid;
    var imageFormat = jpg ? ImageFormat.Jpeg : ImageFormat.Png;

    if (quality < 100)
    {
      var compressedData = CompressBitmapToByteArray(bitmap, imageFormat, quality);
      return await SaveAsync(compressedData, directory);
    }
    else
    {
      using (var stream = new MemoryStream())
      {
        bitmap.Save(stream, imageFormat);
        return await SaveAsync(stream.ToArray(), directory);
      }
    }
  }

  private async Task<string?> SaveAsync(byte[]? bitmapData, string directory)
  {
    if (bitmapData == null)
      return null;

    var fnameFromBytes = GetBitmapFileNameFromBytes(bitmapData);
    var fileName = $"{Guid.NewGuid()}.{Path.GetExtension(fnameFromBytes) ?? "".Trim('.')}";
    var filePath = Path.Combine(_env.WebRootPath, directory, fileName);

    await TimedCreateDirectoryAsync(filePath);

    await File.WriteAllBytesAsync(filePath, bitmapData);
    return fileName;
  }

  private async Task TimedCreateDirectoryAsync(string filePath)
  {
    var dirnfo = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? "");
    if (!dirnfo.Exists)
    {
      await Task.Run(() => dirnfo.Create());
    }
  }

  private string? GetBitmapFileNameFromBytes(byte[] bitmapBytes)
  {
    if (bitmapBytes == null)
      return null;

    using (MemoryStream ms = new MemoryStream(bitmapBytes))
    {
      using (var image = Image.FromStream(ms))
      {
        var fileName = image.Tag?.ToString() ?? Guid.NewGuid().ToString();
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext))
        {
          ext = image.RawFormat.ToString();
          if (ext?.Length > 0)
          {
            var arr = new Dictionary<string, string> { { "jpeg", "jpg" } };
            var value = arr.SingleOrDefault(m => ext.StartsWith(m.Key,
              StringComparison.OrdinalIgnoreCase)).Value;
            var extension = (value ?? ext).Trim('.');
            fileName += "." + extension.ToLower();
          }
        }
        return fileName;
      }
    }
  }

  public async Task DeleteTempFilesAsync(string directory, int olderThanSeconds = 60 * 60 * 36)
  {
    var dir = BuildWebRootPath(directory);
    if (await Task.Run(() => Directory.Exists(dir)))
    {
      var files = await Task.Run(() => new DirectoryInfo(dir).GetFiles());
      foreach (var file in files)
      {
        if (file.CreationTime < DateTime.Now.AddSeconds(-1 * Math.Abs(olderThanSeconds)))
        {
          await Task.Run(() => file.Delete());
        }
      }
    }
  }

  private string BuildWebRootPath(string directory, params string[] args)
  {
    return Path.Join(_env.WebRootPath,
      directory.Replace("/", "\\").Replace(_env.WebRootPath, "", StringComparison.OrdinalIgnoreCase),
      string.Join('\\', args.Select(s => s.Replace("/", "\\"))));
  }

  public string? GetDataUri(string? imageUrl, ImageFormat? imageFormat = null, int quality = 75)
  {
    var bmp = GetBitmapFromUrl(imageUrl).Result;
    if (bmp == null)
      return null;

    var bmpData = CompressBitmapToByteArray(bmp, imageFormat, quality);
    if (bmpData == null)
      return string.Empty;
    var bmp64 = Convert.ToBase64String(bmpData);
    var dataUri = $"data:image/{bmp.RawFormat.ToString().ToLower()};charset=utf-8;base64,{bmp64}";
    return dataUri;
  }

  public Task<Bitmap?> GetBitmapFromUrl(string? imageUrl)
  {
    if (imageUrl == null)
      return null!;

    using (HttpClient httpClient = new HttpClient())
    {
      HttpResponseMessage response = httpClient.GetAsync(imageUrl).Result;
      response.EnsureSuccessStatusCode(); // Throws an exception if the HTTP response status is an error code.
      Stream inputStream = response.Content.ReadAsStreamAsync().Result;

      // Create a Bitmap from the stream
      return Task.FromResult(new Bitmap(inputStream))!;
    }
  }

  public Bitmap? ByteArrayToBitmap(byte[] byteArray)
  {
    if (byteArray == null || byteArray.Length == 0)
    {
      throw new ArgumentNullException(nameof(byteArray), "Byte array cannot be null or empty.");
    }

    using (MemoryStream ms = new MemoryStream(byteArray))
    {
      try
      {
        // Create an Image object from the MemoryStream
        Image image = Image.FromStream(ms);

        // Convert the Image object to a Bitmap
        Bitmap bitmap = new Bitmap(image);
        return bitmap;
      }
      catch (ArgumentException ex)
      {
        // Handle cases where the byte array is not a valid image format
        Console.WriteLine($"Error converting byte array to image: {ex.Message}");
        return null;
      }
    }
  }

  public byte[]? CompressBitmapToByteArray(Bitmap? bitmap, ImageFormat? imageFormat = null, int quality = 75)
  {
    if (bitmap is null)
      return null;

    imageFormat ??= bitmap.RawFormat;
    var imageEncoder = ImageCodecInfo.GetImageDecoders()
      .SingleOrDefault(c => c.FormatID == imageFormat.Guid);

    if (imageEncoder == null)
      return null;

    //var quality = forceCompress ? (imageFormat == ImageFormat.Jpeg ? 75 : 30) : 90;
    var encoderParameters = new EncoderParameters(2);
    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
    encoderParameters.Param[1] = new EncoderParameter(Encoder.Compression, quality);

    using (MemoryStream stream = new MemoryStream())
    {
      bitmap.Save(stream, imageEncoder, encoderParameters);
      return stream.ToArray();
    }
  }

  [Obsolete("Consider using CompressBitmapToByteArray() instead.")]
  public byte[]? BitmapToByteArray(Bitmap bitmap, ImageFormat? imageFormat = null)
  {
    if (bitmap is null)
      return null;

    imageFormat ??= bitmap.RawFormat;
    using (MemoryStream stream = new MemoryStream())
    {
      // Save the bitmap to the memory stream in the specified format
      bitmap.Save(stream, imageFormat);

      // Convert the memory stream to a byte array
      return stream.ToArray();
    }
  }
}
