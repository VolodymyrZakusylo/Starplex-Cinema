using FluentAssertions;
using StarPlex.Application.Common.Exceptions;
using StarPlex.Application.Common.Helpers;

namespace StarPlex.Application.IntegrationTests.Helpers;

public class ImageValidatorTests
{
    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_ValidJpeg_ReturnsJpg()
    {
        // Arrange
        byte[] jpegBytes = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01 };
        using var stream = new MemoryStream(jpegBytes);
        var fileName = "poster.jpeg";

        // Act
        var extension = await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        extension.Should().Be(".jpg");
        stream.Position.Should().Be(0); // Stream position should be restored
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_ValidPng_ReturnsPng()
    {
        // Arrange
        byte[] pngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
        using var stream = new MemoryStream(pngBytes);
        var fileName = "IMAGE.PNG";

        // Act
        var extension = await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        extension.Should().Be(".png");
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_ValidWebp_ReturnsWebp()
    {
        // Arrange
        byte[] webpBytes = { 0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20 };
        using var stream = new MemoryStream(webpBytes);
        var fileName = "test.webp";

        // Act
        var extension = await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        extension.Should().Be(".webp");
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_Svg_ThrowsBusinessRuleException()
    {
        // Arrange
        byte[] svgBytes = System.Text.Encoding.UTF8.GetBytes("<svg></svg>");
        using var stream = new MemoryStream(svgBytes);
        var fileName = "malicious.svg";

        // Act
        Func<Task> act = async () => await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Invalid file extension*");
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_Html_ThrowsBusinessRuleException()
    {
        // Arrange
        byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>");
        using var stream = new MemoryStream(htmlBytes);
        var fileName = "test.html";

        // Act
        Func<Task> act = async () => await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_FakeJpgWithHtml_ThrowsBusinessRuleException()
    {
        // Arrange
        // File ends in .jpg but starts with HTML
        byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes("<html><body>Malicious content</body></html>");
        using var stream = new MemoryStream(htmlBytes);
        var fileName = "fake.jpg";

        // Act
        Func<Task> act = async () => await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("File content does not match its extension.");
    }

    [Fact]
    public async Task ValidateAndGetSafeExtensionAsync_OversizedFile_ThrowsBusinessRuleException()
    {
        // Arrange
        byte[] largeBytes = new byte[5 * 1024 * 1024 + 1]; // 5MB + 1 byte
        using var stream = new MemoryStream(largeBytes);
        var fileName = "large.png";

        // Act
        Func<Task> act = async () => await ImageValidator.ValidateAndGetSafeExtensionAsync(stream, fileName);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Poster file size exceeds the 5 MB limit.");
    }
}
