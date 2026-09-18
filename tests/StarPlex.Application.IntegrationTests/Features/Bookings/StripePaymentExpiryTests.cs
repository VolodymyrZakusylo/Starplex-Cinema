using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StarPlex.Application.Common.Models;
using StarPlex.Infrastructure.Services;
using Stripe;

namespace StarPlex.Application.IntegrationTests.Features.Bookings;

[Collection("IntegrationTestCollection")]
public class StripePaymentExpiryTests
{
    [Theory]
    [InlineData("succeeded", null, PaymentIntentExpiryResult.AlreadySucceeded)]
    [InlineData("canceled", null, PaymentIntentExpiryResult.Cancelled)]
    [InlineData("requires_payment_method", "canceled", PaymentIntentExpiryResult.Cancelled)]
    [InlineData("processing", "processing", PaymentIntentExpiryResult.Indeterminate)]
    public async Task ExpireIntent_RequiresProviderConfirmedCancellation(string initialStatus, string? cancelStatus, PaymentIntentExpiryResult expected)
    {
        var requests = new List<string>();
        var handler = new StubHandler(request =>
        {
            requests.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
            var status = request.Method == HttpMethod.Get ? initialStatus : cancelStatus;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"id\":\"pi_expiry\",\"object\":\"payment_intent\",\"status\":\"{status}\"}}")
            };
        });

        var result = await RunAsync(handler);

        result.Should().Be(expected);
        if (cancelStatus == null)
            requests.Should().Equal("GET /v1/payment_intents/pi_expiry");
        else
            requests.Should().Equal("GET /v1/payment_intents/pi_expiry", "POST /v1/payment_intents/pi_expiry/cancel");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderFailure_IncludingPaymentWinningCancelRace_IsIndeterminate(bool failCancel)
    {
        var requests = 0;
        var handler = new StubHandler(request =>
        {
            requests++;
            if (failCancel && request.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"pi_expiry\",\"object\":\"payment_intent\",\"status\":\"requires_payment_method\"}")
                };

            return new HttpResponseMessage(failCancel ? HttpStatusCode.BadRequest : HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"error\":{\"type\":\"invalid_request_error\",\"message\":\"Unable to cancel\"}}")
            };
        });

        (await RunAsync(handler)).Should().Be(PaymentIntentExpiryResult.Indeterminate);
        requests.Should().Be(failCancel ? 2 : 1);
    }

    private static async Task<PaymentIntentExpiryResult> RunAsync(HttpMessageHandler handler)
    {
        var originalKey = StripeConfiguration.ApiKey;
        var originalClient = StripeConfiguration.StripeClient;
        using var httpClient = new HttpClient(handler);
        try
        {
            var service = new StripePaymentService(
                Options.Create(new StripeSettings { SecretKey = "sk_test_expiry_stub" }),
                NullLogger<StripePaymentService>.Instance);
            StripeConfiguration.StripeClient = new StripeClient("sk_test_expiry_stub",
                httpClient: new SystemNetHttpClient(httpClient, maxNetworkRetries: 0));
            return await service.ExpirePaymentIntentAsync("pi_expiry");
        }
        finally
        {
            StripeConfiguration.ApiKey = originalKey;
            StripeConfiguration.StripeClient = originalClient;
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
