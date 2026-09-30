using ElectronicLive.Api.Background.Email;
using ElectronicLive.Api.Models;
using Microsoft.Extensions.Logging;

namespace ElectronicLive.Api.UnitTests.Background.Email;

public sealed class LoggingEmailDispatcherTests : IDisposable
{
    private readonly string _testScratchDirectory;
    private readonly ILogger<LoggingEmailDispatcher> _logger;

    public LoggingEmailDispatcherTests()
    {
        _testScratchDirectory = Path.Combine(Path.GetTempPath(), "electroniclive-test-emails-" + Guid.NewGuid());
        _logger = Substitute.For<ILogger<LoggingEmailDispatcher>>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testScratchDirectory))
        {
            Directory.Delete(_testScratchDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Should_WritePreviewHtmlFile_WhenSendingDigest()
    {
        var dispatcher = new LoggingEmailDispatcher(_logger, _testScratchDirectory);
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Fred Again.. Live",
                "Alexandra Palace",
                new DateOnly(2026, 11, 20),
                null,
                "https://ticketmaster.co.uk/fred",
                EventStatus.OnSale,
                EventProvider.Ticketmaster
            ),
        };

        await dispatcher.SendDigestAsync(
            "fan@electroniclive.com",
            "Fred Again..",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=abc"
        );

        Directory.Exists(_testScratchDirectory).ShouldBeTrue();
        var generatedFiles = Directory.GetFiles(_testScratchDirectory, "*.html");
        generatedFiles.Length.ShouldBe(1);

        var fileContent = await File.ReadAllTextAsync(generatedFiles[0]);
        fileContent.ShouldContain("Fred Again..");
        fileContent.ShouldContain("Alexandra Palace");
        fileContent.ShouldContain("https://electroniclive.co.uk/unsubscribe?token=abc");
    }

    [Fact]
    public async Task Should_LogEmailSummary_WhenSendingDigest()
    {
        var dispatcher = new LoggingEmailDispatcher(_logger, _testScratchDirectory);
        var events = new List<EventResponse>
        {
            new(
                "evt-1",
                "Peggy Gou DJ Set",
                "Drumsheds",
                new DateOnly(2026, 12, 1),
                null,
                "https://ra.co/peggy",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor
            ),
        };

        await dispatcher.SendDigestAsync(
            "user@electroniclive.com",
            "Peggy Gou",
            events,
            "https://electroniclive.co.uk/unsubscribe?token=xyz"
        );

        _logger
            .ReceivedWithAnyArgs()
            .Log(LogLevel.Information, default, Arg.Any<object>(), null, Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Should_WriteToConfiguredScratchDirectory_WithMultiProviderFixtures()
    {
        var scratchDir = Path.Combine(Directory.GetCurrentDirectory(), ".scratch", "emails");
        var dispatcher = new LoggingEmailDispatcher(_logger, scratchDir);
        var events = new List<EventResponse>
        {
            new(
                "evt-bicep-drumsheds",
                "Bicep Live Chroma AV DJ Set",
                "Drumsheds, London",
                new DateOnly(2026, 11, 14),
                new TimeOnly(22, 0),
                "https://ticketmaster.co.uk/event/bicep",
                EventStatus.OnSale,
                EventProvider.Ticketmaster,
                new List<EventTicketOffer>
                {
                    new(EventProvider.Ticketmaster, "https://ticketmaster.co.uk/event/bicep", EventStatus.OnSale),
                    new(EventProvider.ResidentAdvisor, "https://ra.co/events/bicep-drumsheds", EventStatus.SoldOut),
                    new(EventProvider.Skiddle, "https://skiddle.com/events/bicep-drumsheds", EventStatus.OnSale),
                }
            ),
            new(
                "evt-overmono-roundhouse",
                "Overmono Live Tour",
                "Roundhouse, London",
                new DateOnly(2026, 12, 5),
                new TimeOnly(19, 30),
                "https://ra.co/events/overmono",
                EventStatus.OnSale,
                EventProvider.ResidentAdvisor,
                new List<EventTicketOffer>
                {
                    new(EventProvider.ResidentAdvisor, "https://ra.co/events/overmono", EventStatus.OnSale),
                    new(EventProvider.Ticketmaster, "https://ticketmaster.co.uk/event/overmono", EventStatus.OnSale),
                }
            ),
        };

        await dispatcher.SendDigestAsync(
            "fan@electroniclive.co.uk",
            "Bicep & Overmono",
            events,
            "http://localhost:5173/api/subscriptions/unsubscribe?token=sample-test-token-12345&artist=bicep"
        );

        Directory.Exists(scratchDir).ShouldBeTrue();
        var files = Directory.GetFiles(scratchDir, "*.html");
        files.Length.ShouldBeGreaterThan(0);
    }
}
