using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models.Subscriptions;
using ElectronicLive.Api.Services.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests.Endpoints;

public class SubscriptionEndpointsTests
{
    private readonly ISubscriptionService _subscriptionService = Substitute.For<ISubscriptionService>();

    [Fact]
    public async Task Should_ReturnCreated_WhenSubscriptionIsCreated()
    {
        var subId = Guid.NewGuid();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request.Email, request.ArtistName, request.City, Arg.Any<CancellationToken>())
            .Returns(new SubscribeResult(SubscribeStatus.Created, subId, "Subscribed successfully"));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var created = result.Result.ShouldBeOfType<Created<SubscribeResponse>>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        created.Value!.SubscriptionId.ShouldBe(subId);
        created.Location.ShouldBe($"/api/subscriptions/{subId}");
    }

    [Theory]
    [InlineData(SubscribeStatus.AlreadySubscribed, "Already subscribed to Bicep in London.")]
    [InlineData(SubscribeStatus.Reactivated, "Subscription to Bicep in London reactivated successfully.")]
    public async Task Should_ReturnOk_WhenSubscriptionAlreadyExistsOrReactivated(SubscribeStatus status, string message)
    {
        var subId = Guid.NewGuid();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request.Email, request.ArtistName, request.City, Arg.Any<CancellationToken>())
            .Returns(new SubscribeResult(status, subId, message));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var ok = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        ok.Value!.SubscriptionId.ShouldBe(subId);
        ok.Value.Message.ShouldBe(message);
    }

    [Theory]
    [InlineData(SubscribeStatus.InvalidEmail, "email", "A valid email address is required.")]
    [InlineData(SubscribeStatus.InvalidArtist, "artistName", "Artist name is required.")]
    [InlineData(SubscribeStatus.ArtistNotVerified, "artistName", "Artist 'Fake' could not be verified.")]
    public async Task Should_ReturnValidationProblem_WhenValidationOrVerificationFails(
        SubscribeStatus status,
        string expectedKey,
        string errorMessage
    )
    {
        var request = new SubscribeRequest("fan@electroniclive.com", "Fake", "London");
        _subscriptionService
            .SubscribeAsync(request.Email, request.ArtistName, request.City, Arg.Any<CancellationToken>())
            .Returns(new SubscribeResult(status, ErrorMessage: errorMessage));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey(expectedKey);
        problem.ProblemDetails.Errors[expectedKey].ShouldContain(errorMessage);
    }

    [Fact]
    public async Task Should_Return400BadRequestHtml_WhenUnsubscribeTokenIsMissing()
    {
        _subscriptionService
            .UnsubscribeAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new UnsubscribeResult(UnsubscribeStatus.MissingToken, ErrorMessage: "Token is required."));

        var result = await SubscriptionEndpoints.Unsubscribe(null, null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status400BadRequest, "Token is required.");
    }

    [Fact]
    public async Task Should_Return404NotFoundHtml_WhenUnsubscribeTokenIsInvalid()
    {
        _subscriptionService
            .UnsubscribeAsync("bad-token", null, Arg.Any<CancellationToken>())
            .Returns(new UnsubscribeResult(UnsubscribeStatus.InvalidToken, ErrorMessage: "Invalid or expired link."));

        var result = await SubscriptionEndpoints.Unsubscribe("bad-token", null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status404NotFound, "Invalid or expired link.");
    }

    [Theory]
    [InlineData(UnsubscribeStatus.Success, "unsubscribed from alerts for Bicep")]
    [InlineData(UnsubscribeStatus.AllSuccess, "unsubscribed from all artist alerts")]
    [InlineData(UnsubscribeStatus.NotSubscribed, "not currently subscribed to alerts for Bicep")]
    public async Task Should_Return200OkHtml_WhenUnsubscribeSucceeds(UnsubscribeStatus status, string message)
    {
        _subscriptionService
            .UnsubscribeAsync("valid-token", "Bicep", Arg.Any<CancellationToken>())
            .Returns(new UnsubscribeResult(status, Message: message));

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token", "Bicep", _subscriptionService);

        AssertHtml(result, StatusCodes.Status200OK, message);
    }

    [Fact]
    public async Task Should_PropagateCancellationToken_OnSubscribeAndUnsubscribe()
    {
        using var cts = new CancellationTokenSource();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request.Email, request.ArtistName, request.City, cts.Token)
            .Returns(new SubscribeResult(SubscribeStatus.Created, Guid.NewGuid()));
        _subscriptionService
            .UnsubscribeAsync("token", "Bicep", cts.Token)
            .Returns(new UnsubscribeResult(UnsubscribeStatus.Success, "Done"));

        await SubscriptionEndpoints.Subscribe(request, _subscriptionService, cts.Token);
        await SubscriptionEndpoints.Unsubscribe("token", "Bicep", _subscriptionService, cts.Token);

        await _subscriptionService
            .Received(1)
            .SubscribeAsync(request.Email, request.ArtistName, request.City, cts.Token);
        await _subscriptionService.Received(1).UnsubscribeAsync("token", "Bicep", cts.Token);
    }

    private static void AssertHtml(ContentHttpResult result, int expectedStatusCode, string expectedSnippet)
    {
        result.StatusCode.ShouldBe(expectedStatusCode);
        result.ContentType.ShouldBe("text/html; charset=utf-8");
        result.ResponseContent.ShouldNotBeNull();
        result.ResponseContent.ShouldContain(expectedSnippet);
    }
}
