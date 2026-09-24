using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StarPlex.Application.Common.Interfaces;

namespace StarPlex.API.Controllers;

[Route("uploads")]
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public MediaController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    [HttpGet("{*path}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMedia(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotFound();
        }

        var normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.Contains("..") || normalizedPath.StartsWith('/') || normalizedPath.Contains(':'))
        {
            return BadRequest("Invalid path format.");
        }

        var downloadInfo = await _fileStorageService.GetFileAsync($"uploads/{path}", cancellationToken);

        if (downloadInfo == null)
        {
            return NotFound();
        }

        return File(downloadInfo.Stream, downloadInfo.ContentType);
    }
}
