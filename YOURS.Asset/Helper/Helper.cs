using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

public static class core
{
  #region constants
  public class folder
  {
    public const string appdata = nameof(appdata);
    public static string members => Path.Join(nameof(appdata), nameof(members));
    public static string data => Path.Join(nameof(appdata), nameof(data));
  }

  public static string homepage { get; set; } = "/";
  public static string urdomain => $"https://yourvisioninfo.{(localhost ? "host" : "com")}";
  public static string urlibs => $"https://asset.{new Uri(core.urdomain).Host}/cdn";
  public const string urvision = "ยัวร์วิชั่นฯ";
  public const string urs = "ยัวร์ฯ";
  public const string MetaDescription = $"yourdata, your productivities and cost saving";
  public const string MetaKeywords = $"{urvision}, {urs}, yourvision, yourdata, yours, your, vision, data, " +
    $"process, inventory, production, productivity, cost saving, reduce bad parts, " +
    $"real time monitoring and notifications, make company benefits, xbar-r, " +
    $"factory, oee, reject rate, spc, statistic process control, statistical process control, " +
    $"fmea, failure mode effects analysis, failure mode analysis, pareto, " +
    $"pie chart, bar chart, line chart, graph, sale";
  //public const string RegExBlankLine1 = @"^(?([^\r\n])\s)*\r?$\r?\n";
  //public const string RegExBlankLine2 = @"^(?:[\t ]*(?:\r?\n|\r))+";
  public static bool localhost
  {
    get
    {
      if (_localhost == null)
      {
        const string LOCALHOST = "localhost";
        const string HOST = "host";
        var hostDomain = new HttpContextAccessor()?.HttpContext?.Request.Host.Host ?? LOCALHOST;
        var islocal = hostDomain.StartsWith(LOCALHOST, StringComparison.OrdinalIgnoreCase)
          || hostDomain.EndsWith(HOST, StringComparison.OrdinalIgnoreCase);
        _localhost = islocal;
      }
      return _localhost ?? true;
    }
  }
  private static bool? _localhost;
  #endregion

  #region software version
  internal static string SwRev => $"{ProductName}-{FileVersion}";

  public static string? AssemblyName => _assemblyName ??= Assembly.GetEntryAssembly()?.GetName().Name;
  private static string? _assemblyName;

  public static string? AssemblyVersion => _assemblyVersion ??= Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
  private static string? _assemblyVersion;

  public static string? FileVersion
  {
    get
    {
      if (_assemblyVersion == null)
      {
        var assemply = Assembly.GetEntryAssembly();
        if (assemply is not null && assemply.IsDefined(typeof(AssemblyFileVersionAttribute)))
        {
          _fileVersion = assemply.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        }
      }
      return _fileVersion;
    }
  }
  private static string? _fileVersion;

  public static string? InformationVersion
  {
    get
    {
      if (_informationVersion == null)
      {
        var assemply = Assembly.GetEntryAssembly();
        if (assemply is not null && assemply.IsDefined(typeof(AssemblyInformationalVersionAttribute)))
        {
          _informationVersion = assemply.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split("+").First();
        }
      }
      return _informationVersion;
    }
  }
  private static string? _informationVersion;

  public static string? ProductName
  {
    get
    {
      if (_productName == null)
      {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is not null && assembly.IsDefined(typeof(AssemblyProductAttribute)))
        {
          _productName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product!;
        }
      }
      return _productName;
    }
  }
  private static string? _productName;

  private static string? SolutionName
  {
    get
    {
      if (_solutionName == null)
      {
        var currDir = Directory.GetCurrentDirectory();
        var solutionDir = Directory.GetParent(currDir)?.FullName;
        if (solutionDir != null)
        {
          var solution = Directory.GetFiles(solutionDir)
            .SingleOrDefault(f => Path.GetExtension(f)
            .EndsWith("sln", StringComparison.OrdinalIgnoreCase));
          _solutionName = Path.GetFileNameWithoutExtension(solution);
        }
      }
      return _solutionName;
    }
  }
  private static string? _solutionName;
  #endregion

  #region utilities
  public static bool mrkornpc => _mrkornpc ??= System.Net.Dns.GetHostEntry("localhost").HostName
    .Split('.').Any(m => m.Equals("mrkorn-pc", StringComparison.OrdinalIgnoreCase));
  private static bool? _mrkornpc;

  public static bool migrations => _migrations ??= Environment.GetCommandLineArgs().First().Contains("ef.dll");
  private static bool? _migrations;

  public static T? ChangeType<T>(object? value) => value != null ? (T)Convert.ChangeType(value, typeof(T)) : default(T);

  public static async Task<string?> GenerateThumbnailDataUriAsync(string? imageUrl, int size = 32)
  {
    if (imageUrl == null)
      return null;

    Bitmap original;
    var uri = new Uri(imageUrl);
    if (uri.Scheme.StartsWith("http"))
    {
      // imageUrl is https..
      using var http = new HttpClient();
      var originalBytes = await http.GetByteArrayAsync(imageUrl);
      using var ms = new MemoryStream(originalBytes);
      original = (Bitmap)Image.FromStream(ms);
    }
    else
    {
      // imageUrl is filepath..
      using FileStream fileStream = new FileStream(imageUrl, FileMode.Open, FileAccess.Read);
      using var ms = new MemoryStream();
      fileStream.CopyTo(ms);
      original = (Bitmap)Image.FromStream(ms);
      await Task.CompletedTask;
    }

    using var thumbnail = new Bitmap(size, size);
    using (var g = Graphics.FromImage(thumbnail))
    {
      g.CompositingQuality = CompositingQuality.HighQuality;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.InterpolationMode = InterpolationMode.HighQualityBicubic;
      g.PixelOffsetMode = PixelOffsetMode.HighQuality;

      g.DrawImage(original, 0, 0, size, size);
    }
    original.Dispose();
    original = null!;

    using var outStream = new MemoryStream();
    thumbnail.Save(outStream, ImageFormat.Png);

    var base64 = Convert.ToBase64String(outStream.ToArray());
    return $"data:image/png;base64,{base64}";
  }
  #endregion
}
