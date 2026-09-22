using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using StarPlex.Infrastructure.Configuration;
using System.Net;

namespace StarPlex.Infrastructure.Services;

public class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public AzureBlobFileStorageService(BlobServiceClient blobServiceClient, IOptions<AzureBlobStorageSettings> settings)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient(settings.Value.ContainerName);
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string folderPath, string fileName, CancellationToken cancellationToken)
    {
        var normalizedFolder = folderPath.Replace('\\', '/').Trim('/');

        if (normalizedFolder != "uploads" && !normalizedFolder.StartsWith("uploads/"))
        {
            throw new ArgumentException("Folder path must be within the uploads directory.");
        }

        var relativeToUploads = normalizedFolder == "uploads" ? "" : normalizedFolder.Substring("uploads/".Length);
        var relativePath = string.IsNullOrEmpty(relativeToUploads) ? $"uploads/{fileName}" : $"uploads/{relativeToUploads}/{fileName}";

        if (relativePath.Contains(".."))
        {
            throw new ArgumentException("Invalid path: traversal detected.");
        }

        var blobClient = _containerClient.GetBlobClient(relativePath);

        if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        };

        await blobClient.UploadAsync(fileStream, options, cancellationToken);

        return $"/{relativePath}";
    }

    public async Task<FileDownloadInfo?> GetFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var blobName = filePath.Replace('\\', '/').TrimStart('/');

        if (!blobName.StartsWith("uploads/"))
        {
            throw new ArgumentException("File path must be within the uploads directory.");
        }
        
        if (blobName.Contains(".."))
        {
            throw new ArgumentException("Invalid path: traversal detected.");
        }

        var blobClient = _containerClient.GetBlobClient(blobName);

        try
        {
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new FileDownloadInfo(response.Value.Content, response.Value.Details.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
