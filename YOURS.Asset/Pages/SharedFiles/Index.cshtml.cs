using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace YOURS.Asset.Pages.SharedFiles
{
  [IgnoreAntiforgeryToken]
  public class IndexModel : PageModel
  {
    private readonly IWebHostEnvironment _env;

    public IndexModel(IWebHostEnvironment env)
    {
      _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public string CurrentPath { get; set; } = string.Empty;
    public string? ParentPath { get; set; }

    public List<(string Name, string RelativePath, bool IsDir, string DisplayPath, long Size, DateTime Modified)> Entries { get; set; } = new();

    // ✅ Extended breadcrumb tuple: Name, RelativePath, Tooltip
    public List<(string Name, string RelativePath, string Tooltip)> Breadcrumb { get; set; } = new();

    public bool IsFileView { get; set; }
    public bool IsImage { get; set; }
    public bool IsTextFile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExt { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty;

    public IActionResult OnGet(string? path)
    {
      CurrentPath = path ?? string.Empty;
      var root = Path.Combine(_env.WebRootPath, "appdata", "shared");
      var currentFullPath = Path.Combine(root, CurrentPath);

      BuildBreadcrumb(CurrentPath);

      // === FILE VIEW ===
      if (System.IO.File.Exists(currentFullPath))
      {
        IsFileView = true;
        FileName = Path.GetFileName(currentFullPath);
        FileExt = Path.GetExtension(currentFullPath).ToLower();

        var imageExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".tiff" };
        var textExts = new[] { ".txt", ".cs", ".cshtml", ".cshtml.cs", ".json", ".js", ".css", ".md" };

        if (imageExts.Contains(FileExt))
        {
          IsImage = true;
          FileUrl = "/appdata/shared/" + CurrentPath.Replace("\\", "/");
        }
        else if (textExts.Contains(FileExt))
        {
          IsTextFile = true;
          var rawText = System.IO.File.ReadAllText(currentFullPath);
          var lines = rawText.Split('\n');
          if (lines.Length > 0)
            lines[0] = lines[0].TrimStart();
          FileContent = string.Join("\n", lines);
        }
        else
        {
          return PhysicalFile(currentFullPath, "application/octet-stream", FileName);
        }

        ParentPath = Path.GetDirectoryName(CurrentPath)?.Replace("\\", "/");
        return Page();
      }

      // === FOLDER VIEW ===
      else if (System.IO.Directory.Exists(currentFullPath))
      {
        if (!string.IsNullOrEmpty(Search))
        {
          var matches = Directory.EnumerateFileSystemEntries(currentFullPath, "*", SearchOption.AllDirectories);
          foreach (var entry in matches)
          {
            var name = Path.GetFileName(entry);
            if (name.Contains(Search, StringComparison.OrdinalIgnoreCase))
            {
              bool isDir = Directory.Exists(entry);
              var relativePath = Path.GetRelativePath(root, entry).Replace("\\", "/");
              long size = isDir ? 0 : new FileInfo(entry).Length;
              DateTime modified = isDir ? Directory.GetLastWriteTime(entry) : System.IO.File.GetLastWriteTime(entry);
              Entries.Add((name, relativePath, isDir, name, size, modified));
            }
          }
        }
        else
        {
          foreach (var dir in Directory.GetDirectories(currentFullPath))
          {
            var rel = Path.GetRelativePath(root, dir).Replace("\\", "/");
            var di = new DirectoryInfo(dir);
            Entries.Add((Path.GetFileName(dir), rel, true, Path.GetFileName(dir), 0, di.LastWriteTime));
          }

          foreach (var file in Directory.GetFiles(currentFullPath))
          {
            var rel = Path.GetRelativePath(root, file).Replace("\\", "/");
            var fi = new FileInfo(file);
            Entries.Add((Path.GetFileName(file), rel, false, Path.GetFileName(file), fi.Length, fi.LastWriteTime));
          }
        }

        Entries = Entries
            .OrderBy(e => e.IsDir ? 0 : 1)
            .ThenBy(e => e.DisplayPath)
            .ToList();

        ParentPath = Path.GetDirectoryName(CurrentPath)?.Replace("\\", "/");

        // ✅ Return only the table for AJAX fetch requests
        if (Request.Headers["X-Requested-With"] == "fetch")
          return Partial("_ResultsPartial", this);

        return Page();
      }

      return NotFound();
    }

    public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, string? path)
    {
      try
      {
        if (files == null || files.Count == 0)
          return BadRequest("No files received.");

        var root = Path.Combine(_env.WebRootPath, "appdata", "shared");
        var targetDir = string.IsNullOrEmpty(path)
            ? root
            : Path.Combine(root, path);

        if (!Directory.Exists(targetDir))
          Directory.CreateDirectory(targetDir);

        foreach (var file in files)
        {
          if (file.Length > 30 * 1024 * 1024)
            return BadRequest($"File '{file.FileName}' exceeds 30 MB limit.");

          var destPath = Path.Combine(targetDir, file.FileName);

          await using var stream = new FileStream(destPath, FileMode.Create);
          await file.CopyToAsync(stream);
        }

        return new JsonResult(new { success = true });
      }
      catch (IOException ioEx)
      {
        return BadRequest($"File error: {ioEx.Message}");
      }
      catch (Exception ex)
      {
        return BadRequest($"Upload failed: {ex.Message}");
      }
    }

    private void BuildBreadcrumb(string currentPath)
    {
      Breadcrumb.Clear();

      Breadcrumb.Add((
        Name: "Root",
        RelativePath: string.Empty,
        Tooltip: "appdata/shared"
      ));

      if (string.IsNullOrEmpty(currentPath))
        return;

      var parts = currentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
      string accumulated = "";
      foreach (var part in parts)
      {
        accumulated = string.IsNullOrEmpty(accumulated) ? part : $"{accumulated}/{part}";
        Breadcrumb.Add((
          Name: part,
          RelativePath: accumulated,
          Tooltip: $"appdata/shared/{accumulated}"
        ));
      }
    }

    public static string FormatSize(long size)
    {
      if (size < 1024)
        return $"{size} B";
      double kb = size / 1024.0;
      if (kb < 1024)
        return $"{kb:F1} KB";
      double mb = kb / 1024.0;
      if (mb < 1024)
        return $"{mb:F1} MB";
      double gb = mb / 1024.0;
      return $"{gb:F1} GB";
    }
  }
}
