using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventDeduplicatorTests
{
    private static EventResponse CreateSampleEvent(
        string id = "e-1",
        string name = "Bicep Live",
        string venue = "The Drumsheds",
        DateOnly? date = null,
        TimeOnly? time = null,
        EventStatus status = EventStatus.OnSale,
        EventProvider provider = EventProvider.Ticketmaster,
        string? ticketUrl = "https://tickets.com/1",
        bool explicitDate = false
    ) =>
        new(
            id,
            name,
            venue,
            explicitDate ? date : (date ?? new DateOnly(2026, 12, 10)),
            time ?? new TimeOnly(21, 0),
            ticketUrl,
            status,
            provider
        );

    [Fact]
    public void Should_ReturnEmpty_WhenEventsCollectionIsEmpty()
    {
        var result = EventDeduplicator.Deduplicate([]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_PopulateSingleOffer_WhenEventHasNoDuplicates()
    {
        var ev = CreateSampleEvent(
            "tm-1",
            "Fred Again..",
            "Alexandra Palace",
            new DateOnly(2026, 9, 30),
            new TimeOnly(19, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/fred"
        );

        var result = EventDeduplicator.Deduplicate([ev]);

        var item = result.ShouldHaveSingleItem();
        var offers = item.Offers.ShouldNotBeNull();
        var offer = offers.ShouldHaveSingleItem();
        offer.Provider.ShouldBe(EventProvider.Ticketmaster);
        offer.TicketUrl.ShouldBe("https://ticketmaster.com/fred");
        offer.Status.ShouldBe(EventStatus.OnSale);
    }

    [Fact]
    public void Should_MergeDuplicateEventsAcrossProviders_WhenDateAndNormalizedVenueMatch()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Four Tet Live",
            "The Drumsheds",
            new DateOnly(2026, 10, 17),
            new TimeOnly(14, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/1"
        );
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Four Tet - All Day Long",
            "Drumsheds, London",
            new DateOnly(2026, 10, 17),
            new TimeOnly(13, 0),
            EventStatus.SoldOut,
            EventProvider.Skiddle,
            "https://skiddle.com/1"
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        var merged = result.ShouldHaveSingleItem();
        merged.Date.ShouldBe(new DateOnly(2026, 10, 17));
        merged.Time.ShouldBe(new TimeOnly(13, 0)); // Earlier time (doors)
        merged.Status.ShouldBe(EventStatus.OnSale); // OnSale over SoldOut
        merged.Provider.ShouldBe(EventProvider.Ticketmaster);
        merged.TicketUrl.ShouldBe("https://ticketmaster.com/1");

        var offers = merged.Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(2);
        offers.ShouldContain(o => o.Provider == EventProvider.Ticketmaster && o.Status == EventStatus.OnSale);
        offers.ShouldContain(o => o.Provider == EventProvider.Skiddle && o.Status == EventStatus.SoldOut);
    }

    [Fact]
    public void Should_NormalizeVenueVariations_WhenMatchingDuplicates()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Bicep Live",
            "O2 Academy Brixton",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Bicep",
            "The O2 Academy Brixton, London",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0),
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(1);
        var offers = result[0].Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_PrioritizeOnSaleProvider_WhenDuplicateEventIsSoldOutOnAnotherProvider()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Overmono",
            "Roundhouse",
            new DateOnly(2026, 12, 5),
            new TimeOnly(20, 0),
            EventStatus.SoldOut,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/1"
        );
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Overmono Live",
            "The Roundhouse",
            new DateOnly(2026, 12, 5),
            new TimeOnly(20, 0),
            EventStatus.OnSale,
            EventProvider.Skiddle,
            "https://skiddle.com/1"
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        var merged = result.ShouldHaveSingleItem();
        merged.Status.ShouldBe(EventStatus.OnSale);
        merged.Provider.ShouldBe(EventProvider.Skiddle);
        merged.TicketUrl.ShouldBe("https://skiddle.com/1");
    }

    [Fact]
    public void Should_PrioritizeVerifiedSoldOut_OverUnverifiedUnknown()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Bicep Live",
            "Drumsheds",
            new DateOnly(2026, 11, 26),
            new TimeOnly(19, 0),
            EventStatus.SoldOut,
            EventProvider.Ticketmaster
        );
        var raEvent = CreateSampleEvent(
            "ra-1",
            "Bicep Live",
            "Drumsheds",
            new DateOnly(2026, 11, 26),
            new TimeOnly(19, 0),
            EventStatus.Unknown,
            EventProvider.ResidentAdvisor
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, raEvent]);

        var ev = result.ShouldHaveSingleItem();
        ev.Status.ShouldBe(EventStatus.SoldOut);
        ev.Provider.ShouldBe(EventProvider.Ticketmaster);
    }

    [Fact]
    public void Should_MergeThreeProviders_IntoCompositeOffers()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Bicep Live",
            "The Drumsheds",
            new DateOnly(2026, 11, 26),
            new TimeOnly(19, 0),
            EventStatus.OnSale,
            EventProvider.Ticketmaster,
            "https://ticketmaster.com/bicep"
        );
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Bicep Live at Drumsheds",
            "Drumsheds, London",
            new DateOnly(2026, 11, 26),
            new TimeOnly(18, 30),
            EventStatus.OnSale,
            EventProvider.Skiddle,
            "https://skiddle.com/bicep"
        );
        var raEvent = CreateSampleEvent(
            "ra-1",
            "Bicep",
            "Drumsheds",
            new DateOnly(2026, 11, 26),
            new TimeOnly(18, 0),
            EventStatus.OnSale,
            EventProvider.ResidentAdvisor,
            "https://ra.co/events/1"
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent, raEvent]);

        var ev = result.ShouldHaveSingleItem();
        ev.Offers.ShouldNotBeNull();
        ev.Offers.Count.ShouldBe(3);
        ev.Time.ShouldBe(new TimeOnly(18, 0));
        ev.Offers.Select(o => o.Provider)
            .ShouldBe(
                [EventProvider.Ticketmaster, EventProvider.Skiddle, EventProvider.ResidentAdvisor],
                ignoreOrder: true
            );
    }

    [Fact]
    public void Should_NotMergeEvents_WhenDatesDiffer()
    {
        var tmEvent = CreateSampleEvent(
            "tm-1",
            "Bicep Night 1",
            "Drumsheds",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0)
        );
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Bicep Night 2",
            "Drumsheds",
            new DateOnly(2026, 11, 21),
            new TimeOnly(19, 0)
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_NotMergeEvents_WhenVenuesDiffer()
    {
        var tmEvent = CreateSampleEvent("tm-1", "Bicep", "Fabric", new DateOnly(2026, 11, 20), new TimeOnly(23, 0));
        var skEvent = CreateSampleEvent(
            "sk-1",
            "Bicep",
            "Ministry of Sound",
            new DateOnly(2026, 11, 20),
            new TimeOnly(23, 0)
        );

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_NotMergeEvents_WhenDateIsNull()
    {
        var tmEvent = CreateSampleEvent("tm-1", "Bicep TBA 1", "Drumsheds", null, null, explicitDate: true);
        var skEvent = CreateSampleEvent("sk-1", "Bicep TBA 2", "Drumsheds", null, null, explicitDate: true);

        var result = EventDeduplicator.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }
}
