public static class ApiEndPoints
{
  public static void MapEndPoints(this WebApplication app)
  {
    app.MapPost("/shared/upload", async (HttpRequest req, IWebHostEnvironment env) =>
    {
      try
      {
        var form = await req.ReadFormAsync();
        var file = form.Files["file"];
        var path = form["path"].ToString();

        if (file == null || file.Length == 0)
          return Results.BadRequest("No file selected.");

        if (file.Length > 30 * 1024 * 1024)
          return Results.BadRequest("File exceeds 30 MB limit.");

        var root = Path.Combine(env.WebRootPath, "appdata", "shared");
        var targetDir = Path.Combine(root, path ?? "");
        Directory.CreateDirectory(targetDir);

        var targetPath = Path.Combine(targetDir, Path.GetFileName(file.FileName));
        using (var stream = System.IO.File.Create(targetPath))
          await file.CopyToAsync(stream);

        return Results.Json(new { success = true, file = file.FileName });
      }
      catch (IOException ioEx)
      {
        return Results.BadRequest($"File error: {ioEx.Message}");
      }
      catch (Exception ex)
      {
        return Results.BadRequest($"Upload failed: {ex.Message}");
      }
    });
  }
}
