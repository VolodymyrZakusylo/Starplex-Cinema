using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StarPlex.Application.Common.Interfaces;
using StarPlex.Application.Common.Models;
using System.Net;

namespace StarPlex.API.IntegrationTests;

public class MediaControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;

    public MediaControllerTests(WebApplicationFactory<Program> factory)
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped(_ => _fileStorageServiceMock.Object);
            });
        });
    }

    [Fact]
    public async Task GetMedia_WhenFileExists_ReturnsFile()
    {
        var content = "test content"u8.ToArray();
        var stream = new MemoryStream(content);
        var downloadInfo = new FileDownloadInfo(stream, "image/jpeg");

        _fileStorageServiceMock
            .Setup(s => s.GetFileAsync("uploads/posters/test.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(downloadInfo);

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/uploads/posters/test.jpg");

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.ToString().Should().Be("image/jpeg");
        
        var returnedBytes = await response.Content.ReadAsByteArrayAsync();
        returnedBytes.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task GetMedia_WhenFileDoesNotExist_ReturnsNotFound()
    {
        _fileStorageServiceMock
            .Setup(s => s.GetFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileDownloadInfo?)null);

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/uploads/missing.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../posters/test.jpg")]
    [InlineData("posters/../../test.jpg")]
    [InlineData("%2Fposters%2Ftest.jpg")]
    [InlineData("c%3A%5Cposters%5Ctest.jpg")]
    [InlineData("test:file.jpg")]
    public async Task GetMedia_WhenPathIsInvalid_ReturnsBadRequestOrNotFound(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/uploads/{path}");

        response.StatusCode.Should().Match(s => s == HttpStatusCode.BadRequest || s == HttpStatusCode.NotFound);
    }
}
