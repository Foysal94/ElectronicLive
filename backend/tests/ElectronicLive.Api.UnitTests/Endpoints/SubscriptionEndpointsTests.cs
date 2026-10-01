using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests.Endpoints;

public class SubscriptionEndpointsTests
{
    private readonly ISubscriptionService _subscriptionService = Substitute.For<ISubscriptionService>();

    [Theory]
    [InlineData(null, "Bicep", "email")]
    [InlineData("", "Bicep", "email")]
    [InlineData("   ", "Bicep", "email")]
    [InlineData("invalid-email", "Bicep", "email")]
    [InlineData("fan@electroniclive.com", null, "artistName")]
    [InlineData("fan@electroniclive.com", "", "artistName")]
    public async Task Should_ReturnValidationProblem_WhenSyntaxIsInvalid(
        string? email,
        string? artistName,
        string expectedErrorKey
    )
    {
        var request = new SubscribeRequest(email, artistName, "London");

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey(expectedErrorKey);
        await _subscriptionService.DidNotReceiveWithAnyArgs().SubscribeAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenServiceReturnsNullOutcome()
    {
        var request = new SubscribeRequest("fan@electroniclive.com", "FakeArtist", "London");
        _subscriptionService.SubscribeAsync(request, Arg.Any<CancellationToken>()).Returns((SubscribeOutcome?)null);

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey("artistName");
        problem.ProblemDetails.Errors["artistName"][0].ShouldContain("FakeArtist");
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenSubscriptionIsNew()
    {
        var subId = Guid.NewGuid();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request, Arg.Any<CancellationToken>())
            .Returns(new SubscribeOutcome(subId, IsNew: true));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var created = result.Result.ShouldBeOfType<Created<SubscribeResponse>>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        created.Value!.SubscriptionId.ShouldBe(subId);
        created.Value.Message.ShouldBe("Subscribed successfully");
        created.Location.ShouldBe($"/api/subscriptions/{subId}");
    }

    [Fact]
    public async Task Should_ReturnOk_WhenSubscriptionAlreadyExistsOrReactivated()
    {
        var subId = Guid.NewGuid();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request, Arg.Any<CancellationToken>())
            .Returns(new SubscribeOutcome(subId, IsNew: false));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService);

        var ok = result.Result.ShouldBeOfType<Ok<SubscribeResponse>>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        ok.Value!.SubscriptionId.ShouldBe(subId);
        ok.Value.Message.ShouldContain("Already subscribed");
    }

    [Fact]
    public async Task Should_ReturnBadRequestHtml_WhenUnsubscribeTokenIsMissing()
    {
        _subscriptionService
            .UnsubscribeAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(UnsubscribeOutcome.MissingToken);

        var result = await SubscriptionEndpoints.Unsubscribe(null, null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status400BadRequest, "An unsubscribe token is required.");
    }

    [Fact]
    public async Task Should_ReturnNotFoundHtml_WhenUnsubscribeTokenIsInvalid()
    {
        _subscriptionService
            .UnsubscribeAsync("bad-token", null, Arg.Any<CancellationToken>())
            .Returns(UnsubscribeOutcome.InvalidToken);

        var result = await SubscriptionEndpoints.Unsubscribe("bad-token", null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status404NotFound, "Invalid or expired unsubscribe link.");
    }

    [Fact]
    public async Task Should_ReturnOkHtml_WhenUnsubscribeSucceeds()
    {
        _subscriptionService
            .UnsubscribeAsync("valid-token", "Bicep", Arg.Any<CancellationToken>())
            .Returns(UnsubscribeOutcome.Success);

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token", "Bicep", _subscriptionService);

        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from alerts for Bicep");
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _subscriptionService
            .SubscribeAsync(request, cts.Token)
            .Returns(new SubscribeOutcome(Guid.NewGuid(), IsNew: true));
        _subscriptionService.UnsubscribeAsync("token", "Bicep", cts.Token).Returns(UnsubscribeOutcome.Success);

        await SubscriptionEndpoints.Subscribe(request, _subscriptionService, cts.Token);
        await SubscriptionEndpoints.Unsubscribe("token", "Bicep", _subscriptionService, cts.Token);

        await _subscriptionService.Received(1).SubscribeAsync(request, cts.Token);
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
