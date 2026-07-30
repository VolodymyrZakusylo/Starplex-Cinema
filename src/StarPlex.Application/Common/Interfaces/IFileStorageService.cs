namespace StarPlex.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string folderPath, string fileName, CancellationToken cancellationToken);
}