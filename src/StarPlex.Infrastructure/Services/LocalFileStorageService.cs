using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;

namespace StarPlex.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    private string GetUploadsRoot()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        return Path.Combine(webRoot, "uploads");
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string folderPath, string fileName, CancellationToken cancellationToken)
    {
        var normalizedFolder = folderPath.Replace('\\', '/').Trim('/');

        if (normalizedFolder != "uploads" && !normalizedFolder.StartsWith("uploads/"))
        {
            throw new ArgumentException("Folder path must be within the uploads directory.");
        }

        var uploadsRoot = GetUploadsRoot();
        var relativeToUploads = normalizedFolder == "uploads" ? "" : normalizedFolder.Substring("uploads/".Length);

        var targetFolder = string.IsNullOrEmpty(relativeToUploads) ? uploadsRoot : Path.Combine(uploadsRoot, relativeToUploads);
        var filePath = Path.Combine(targetFolder, fileName);

        ValidatePathSafety(filePath, uploadsRoot);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        using (var destinationStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destinationStream, cancellationToken);
        }

        return string.IsNullOrEmpty(relativeToUploads) ? $"/uploads/{fileName}" : $"/uploads/{relativeToUploads}/{fileName}";
    }

    public Task<FileDownloadInfo?> GetFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var normalizedPath = filePath.Replace('\\', '/').TrimStart('/');

        if (!normalizedPath.StartsWith("uploads/"))
        {
            throw new ArgumentException("File path must be within the uploads directory.");
        }

        var uploadsRoot = GetUploadsRoot();
        var relativeToUploads = normalizedPath.Substring("uploads/".Length);

        var fullPath = Path.Combine(uploadsRoot, relativeToUploads);

        ValidatePathSafety(fullPath, uploadsRoot);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileDownloadInfo?>(null);
        }

        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<FileDownloadInfo?>(new FileDownloadInfo(stream, contentType));
    }

    private static void ValidatePathSafety(string fullPath, string uploadsRoot)
    {
        var absolutePath = Path.GetFullPath(fullPath);
        var absoluteUploadsRoot = Path.GetFullPath(uploadsRoot);
        var rootWithSeparator = absoluteUploadsRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? absoluteUploadsRoot
            : absoluteUploadsRoot + Path.DirectorySeparatorChar;

        if (!absolutePath.Equals(absoluteUploadsRoot, StringComparison.OrdinalIgnoreCase) &&
            !absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Invalid path: traversal detected or path escapes intended root.");
        }
    }
}
