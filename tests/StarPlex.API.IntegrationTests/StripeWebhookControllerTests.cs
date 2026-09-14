using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using StarPlex.API.Controllers;
using StarPlex.Application.Common.Models;
using StarPlex.Application.Features.Bookings.Commands.ConfirmBooking;
using Xunit;

namespace StarPlex.API.IntegrationTests;

public class StripeWebhookControllerTests : IClassFixture<WebApplicationFactory<StripeWebhookController>>
{
    private readonly WebApplicationFactory<StripeWebhookController> _factory;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly string _webhookSecret = "whsec_testsecret";

    public StripeWebhookControllerTests(WebApplicationFactory<StripeWebhookController> factory)
    {
        _mediatorMock = new Mock<IMediator>();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ConfirmBookingCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"StripeSettings:SecretKey", "test_sk"},
                    {"StripeSettings:PublishableKey", "test_pk"},
                    {"StripeSettings:WebhookSecret", _webhookSecret}
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IMediator>(_ => _mediatorMock.Object);
            });
        });
    }

    private string GenerateStripeSignature(string payload, string secret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signedPayload = $"{timestamp}.{payload}";
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac = new HMACSHA256(secretBytes);
        var signatureBytes = hmac.ComputeHash(payloadBytes);
        var signatureHex = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();

        return $"t={timestamp},v1={signatureHex}";
    }

    [Fact]
    public async Task HandleWebhook_WhenSignatureIsValidAndEventIsPaymentSucceeded_ShouldSendConfirmBookingCommand()
    {
        var bookingId = Guid.NewGuid();
        var payload = $$"""
        {
          "id": "evt_test",
          "type": "payment_intent.succeeded",
          "api_version": "{{Stripe.StripeConfiguration.ApiVersion}}",
          "data": {
            "object": {
              "id": "pi_test",
              "object": "payment_intent",
              "metadata": {
                "BookingId": "{{bookingId}}"
              }
            }
          }
        }
        """;

        var signature = GenerateStripeSignature(payload, _webhookSecret);

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/StripeWebhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var response = await client.SendAsync(request);

        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseContent);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ConfirmBookingCommand>(c => c.BookingId == bookingId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_WhenSignatureIsInvalid_ShouldReturnBadRequest()
    {
        var payload = "{}";
        var signature = "t=123,v1=invalidsignature"; 

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/StripeWebhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _mediatorMock.Verify(m => m.Send(It.IsAny<ConfirmBookingCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_WhenEventIsUnhandled_ShouldReturnOkWithoutSendingCommand()
    {
        var payload = $$"""
        {
          "id": "evt_test",
          "type": "payment_intent.created",
          "api_version": "{{Stripe.StripeConfiguration.ApiVersion}}",
          "data": {
            "object": {
              "id": "pi_test",
              "object": "payment_intent"
            }
          }
        }
        """;

        var signature = GenerateStripeSignature(payload, _webhookSecret);

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/StripeWebhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", signature);

        var response = await client.SendAsync(request);

        var responseContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseContent);

        _mediatorMock.Verify(m => m.Send(It.IsAny<ConfirmBookingCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}
