using ElectronicLive.Api.Endpoints;
using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.UnitTests.Endpoints;

public class SubscriptionEndpointsTests
{
    private readonly ISubscriptionService _subscriptionService = Substitute.For<ISubscriptionService>();
    private readonly IArtistVerificationService _artistVerificationService =
        Substitute.For<IArtistVerificationService>();

    [Theory]
    [InlineData(null, "Bicep", "email")]
    [InlineData("", "Bicep", "email")]
    [InlineData("   ", "Bicep", "email")]
    [InlineData("invalid-email", "Bicep", "email")]
    [InlineData("user@", "Bicep", "email")]
    [InlineData("fan@electroniclive.com", null, "artistName")]
    [InlineData("fan@electroniclive.com", "", "artistName")]
    [InlineData("fan@electroniclive.com", "   ", "artistName")]
    public async Task Should_ReturnValidationProblem_WhenInputIsInvalid(
        string? email,
        string? artistName,
        string expectedErrorKey
    )
    {
        var request = new SubscribeRequest(email, artistName, "London");

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey(expectedErrorKey);
        await _artistVerificationService.DidNotReceiveWithAnyArgs().VerifyArtistExistsAsync(default!, default);
        await _subscriptionService.DidNotReceiveWithAnyArgs().SubscribeAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnValidationProblem_WhenArtistVerificationFails()
    {
        var request = new SubscribeRequest("fan@electroniclive.com", "FakeArtist", "London");
        _artistVerificationService.VerifyArtistExistsAsync("FakeArtist", Arg.Any<CancellationToken>()).Returns(false);

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService, _artistVerificationService);

        var problem = result.Result.ShouldBeOfType<ValidationProblem>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.ProblemDetails.Errors.ShouldContainKey("artistName");
        problem.ProblemDetails.Errors["artistName"][0].ShouldContain("FakeArtist");
        await _subscriptionService.DidNotReceiveWithAnyArgs().SubscribeAsync(default!, default);
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenSubscriptionIsNew()
    {
        var subId = Guid.NewGuid();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);
        _subscriptionService.SubscribeAsync(request, Arg.Any<CancellationToken>()).Returns((subId, IsNew: true));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService, _artistVerificationService);

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
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", Arg.Any<CancellationToken>()).Returns(true);
        _subscriptionService.SubscribeAsync(request, Arg.Any<CancellationToken>()).Returns((subId, IsNew: false));

        var result = await SubscriptionEndpoints.Subscribe(request, _subscriptionService, _artistVerificationService);

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
            .Returns((Success: false, Message: "An unsubscribe token is required."));

        var result = await SubscriptionEndpoints.Unsubscribe(null, null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status400BadRequest, "An unsubscribe token is required.");
    }

    [Fact]
    public async Task Should_ReturnNotFoundHtml_WhenUnsubscribeTokenIsInvalid()
    {
        _subscriptionService
            .UnsubscribeAsync("bad-token", null, Arg.Any<CancellationToken>())
            .Returns((Success: false, Message: "Invalid or expired unsubscribe link."));

        var result = await SubscriptionEndpoints.Unsubscribe("bad-token", null, _subscriptionService);

        AssertHtml(result, StatusCodes.Status404NotFound, "Invalid or expired unsubscribe link.");
    }

    [Fact]
    public async Task Should_ReturnOkHtml_WhenUnsubscribeSucceeds()
    {
        _subscriptionService
            .UnsubscribeAsync("valid-token", "Bicep", Arg.Any<CancellationToken>())
            .Returns((Success: true, Message: "You have successfully unsubscribed from alerts for Bicep."));

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token", "Bicep", _subscriptionService);

        AssertHtml(result, StatusCodes.Status200OK, "unsubscribed from alerts for Bicep");
    }

    [Fact]
    public async Task Should_ReturnOkNotSubscribedHtml_WhenUserIsNotSubscribedToArtist()
    {
        _subscriptionService
            .UnsubscribeAsync("valid-token", "Bicep", Arg.Any<CancellationToken>())
            .Returns((Success: true, Message: "You are not currently subscribed to alerts for Bicep."));

        var result = await SubscriptionEndpoints.Unsubscribe("valid-token", "Bicep", _subscriptionService);

        AssertHtml(result, StatusCodes.Status200OK, "not currently subscribed");
    }

    [Fact]
    public async Task Should_PropagateCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var request = new SubscribeRequest("fan@electroniclive.com", "Bicep", "London");
        _artistVerificationService.VerifyArtistExistsAsync("Bicep", cts.Token).Returns(true);
        _subscriptionService.SubscribeAsync(request, cts.Token).Returns((Guid.NewGuid(), IsNew: true));
        _subscriptionService.UnsubscribeAsync("token", "Bicep", cts.Token).Returns((Success: true, Message: "Done"));

        await SubscriptionEndpoints.Subscribe(request, _subscriptionService, _artistVerificationService, cts.Token);
        await SubscriptionEndpoints.Unsubscribe("token", "Bicep", _subscriptionService, cts.Token);

        await _artistVerificationService.Received(1).VerifyArtistExistsAsync("Bicep", cts.Token);
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
