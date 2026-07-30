using Microsoft.AspNetCore.Hosting;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string folderPath, string fileName, CancellationToken cancellationToken)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var targetFolder = Path.Combine(webRoot, folderPath);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var filePath = Path.Combine(targetFolder, fileName);

        using (var destinationStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destinationStream, cancellationToken);
        }

        return $"/{folderPath}/{fileName}";
    }
}