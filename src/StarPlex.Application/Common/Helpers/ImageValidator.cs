using StarPlex.Application.Common.Exceptions;

namespace StarPlex.Application.Common.Helpers;

public static class ImageValidator
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] WebpSignatureRiff = { 0x52, 0x49, 0x46, 0x46 };
    private static readonly byte[] WebpSignatureWebp = { 0x57, 0x45, 0x42, 0x50 };

    public static async Task<string> ValidateAndGetSafeExtensionAsync(Stream stream, string originalFileName)
    {
        if (stream == null || stream.Length == 0)
            throw new BusinessRuleException("The uploaded file is empty.");

        if (stream.Length > MaxFileSizeBytes)
            throw new BusinessRuleException("Poster file size exceeds the 5 MB limit.");

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
            throw new BusinessRuleException("File must have an extension.");

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!validExtensions.Contains(extension))
            throw new BusinessRuleException("Invalid file extension. Only .jpg, .jpeg, .png, and .webp are allowed.");

        byte[] buffer = new byte[12];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (bytesRead < 4)
            throw new BusinessRuleException("File is too small to be a valid image.");

        bool isValidSignature = false;

        if ((extension == ".jpg" || extension == ".jpeg") && bytesRead >= 3)
        {
            isValidSignature = buffer.Take(3).SequenceEqual(JpegSignature);
        }
        else if (extension == ".png" && bytesRead >= 8)
        {
            isValidSignature = buffer.Take(8).SequenceEqual(PngSignature);
        }
        else if (extension == ".webp" && bytesRead >= 12)
        {
            isValidSignature = buffer.Take(4).SequenceEqual(WebpSignatureRiff) && 
                               buffer.Skip(8).Take(4).SequenceEqual(WebpSignatureWebp);
        }

        if (!isValidSignature)
            throw new BusinessRuleException("File content does not match its extension.");

        return extension == ".jpeg" ? ".jpg" : extension;
    }
}
