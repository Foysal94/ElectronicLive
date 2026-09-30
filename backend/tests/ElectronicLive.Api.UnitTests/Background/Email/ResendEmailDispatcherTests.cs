using System.Net;
using System.Text.Json;
using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Configuration;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElectronicLive.Api.UnitTests.Background.Email;

public sealed class ResendEmailDispatcherTests
{
    private static (ResendEmailDispatcher Dispatcher, CapturingHttpMessageHandler Handler) CreateDispatcher(
        string apiKey = "re_test_12345",
        string? fromEmail = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{\"id\": \"msg_123\"}"
    )
    {
        var options = Options.Create(
            new ResendOptions
            {
                ApiKey = apiKey,
                FromEmail = fromEmail ?? "ElectronicLive <alerts@electroniclive.co.uk>",
            }
        );

        var handler = new CapturingHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };

        var dispatcher = new ResendEmailDispatcher(httpClient, options, NullLogger<ResendEmailDispatcher>.Instance);

        return (dispatcher, handler);
    }

    [Fact]
    public async Task Should_PostEmailPayloadToResendEndpoint_WithBearerToken()
    {
        var (dispatcher, handler) = CreateDispatcher();
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Bicep Live",
                "Drumsheds",
                new DateOnly(2026, 11, 15),
                null,
                "https://ra.co/events/123",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };

        await dispatcher.SendDigestAsync(
            "fan@electroniclive.com",
            "Bicep",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=abc"
        );

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://api.resend.com/emails");
        handler.LastRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.ShouldBe("re_test_12345");

        handler.LastRequestBody.ShouldNotBeNull();
        var json = JsonDocument.Parse(handler.LastRequestBody);
        var root = json.RootElement;

        root.GetProperty("from").GetString().ShouldBe("ElectronicLive <alerts@electroniclive.co.uk>");
        root.GetProperty("to").EnumerateArray().First().GetString().ShouldBe("fan@electroniclive.com");
        root.GetProperty("subject").GetString().ShouldBe("New shows announced: Bicep");
        root.GetProperty("html").GetString()!.ShouldContain("Bicep");
        root.GetProperty("html").GetString()!.ShouldContain("Drumsheds");
    }

    [Fact]
    public async Task Should_UseConfiguredFromEmail_WhenSpecified()
    {
        var (dispatcher, handler) = CreateDispatcher(fromEmail: "ElectronicLive Sandbox <onboarding@resend.dev>");
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Four Tet",
                "Alexandra Palace",
                new DateOnly(2026, 12, 1),
                null,
                "https://ticketmaster.co.uk/event/1",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };

        await dispatcher.SendDigestAsync(
            "test@electroniclive.com",
            "Four Tet",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=xyz"
        );

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequestBody.ShouldNotBeNull();
        var json = JsonDocument.Parse(handler.LastRequestBody);
        json.RootElement.GetProperty("from").GetString().ShouldBe("ElectronicLive Sandbox <onboarding@resend.dev>");
    }

    [Fact]
    public async Task Should_ThrowHttpRequestException_WhenResendApiReturnsError()
    {
        var (dispatcher, _) = CreateDispatcher(
            statusCode: HttpStatusCode.UnprocessableEntity,
            responseBody: "{\"statusCode\": 422, \"message\": \"Domain not verified\"}"
        );
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Overmono",
                "KOKO",
                new DateOnly(2026, 10, 10),
                null,
                "https://skiddle.com/1",
                EventStatus.OnSale,
                EventProvider.Skiddle
            ),
        };

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            dispatcher.SendDigestAsync(
                "unverified@example.com",
                "Overmono",
                events,
                "https://electroniclive.co.uk/unsubscribe"
            )
        );

        exception.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        exception.Message.ShouldContain("Domain not verified");
    }
}
