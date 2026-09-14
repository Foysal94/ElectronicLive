using ElectronicLive.Api.Models;
using ElectronicLive.Api.Services;

namespace ElectronicLive.Api.UnitTests.Services;

public class EventDeduplicatorTests
{
    private readonly EventDeduplicator _sut = new();

    [Fact]
    public void Should_ReturnEmpty_WhenEventsCollectionIsEmpty()
    {
        var result = _sut.Deduplicate([]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Should_PopulateSingleOffer_WhenEventHasNoDuplicates()
    {
        var ev = new EventResponse(
            "tm-1",
            "Fred Again..",
            "Alexandra Palace",
            new DateOnly(2026, 9, 30),
            new TimeOnly(19, 0),
            "https://ticketmaster.com/fred",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );

        var result = _sut.Deduplicate([ev]);

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
        var tmEvent = new EventResponse(
            "tm-1",
            "Four Tet Live",
            "The Drumsheds",
            new DateOnly(2026, 10, 17),
            new TimeOnly(14, 0),
            "https://ticketmaster.com/1",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Four Tet - All Day Long",
            "Drumsheds, London",
            new DateOnly(2026, 10, 17),
            new TimeOnly(13, 0),
            "https://skiddle.com/1",
            EventStatus.SoldOut,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

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
        var tmEvent = new EventResponse(
            "tm-1",
            "Bicep Live",
            "O2 Academy Brixton",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0),
            "https://ticketmaster.com/1",
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Bicep",
            "The O2 Academy Brixton, London",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0),
            "https://skiddle.com/1",
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(1);
        var offers = result[0].Offers.ShouldNotBeNull();
        offers.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_PrioritizeOnSaleProvider_WhenDuplicateEventIsSoldOutOnAnotherProvider()
    {
        var tmEvent = new EventResponse(
            "tm-1",
            "Overmono",
            "Roundhouse",
            new DateOnly(2026, 12, 5),
            new TimeOnly(20, 0),
            "https://ticketmaster.com/1",
            EventStatus.SoldOut,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Overmono Live",
            "The Roundhouse",
            new DateOnly(2026, 12, 5),
            new TimeOnly(20, 0),
            "https://skiddle.com/1",
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

        var merged = result.ShouldHaveSingleItem();
        merged.Status.ShouldBe(EventStatus.OnSale);
        merged.Provider.ShouldBe(EventProvider.Skiddle);
        merged.TicketUrl.ShouldBe("https://skiddle.com/1");
    }

    [Fact]
    public void Should_NotMergeEvents_WhenDatesDiffer()
    {
        var tmEvent = new EventResponse(
            "tm-1",
            "Bicep Live Night 1",
            "Drumsheds",
            new DateOnly(2026, 11, 20),
            new TimeOnly(19, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Bicep Live Night 2",
            "Drumsheds",
            new DateOnly(2026, 11, 21),
            new TimeOnly(19, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_NotMergeEvents_WhenVenuesDiffer()
    {
        var tmEvent = new EventResponse(
            "tm-1",
            "Bicep",
            "Fabric",
            new DateOnly(2026, 11, 20),
            new TimeOnly(23, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Bicep",
            "Ministry of Sound",
            new DateOnly(2026, 11, 20),
            new TimeOnly(23, 0),
            null,
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Should_NotMergeEvents_WhenDateIsNull()
    {
        var tmEvent = new EventResponse(
            "tm-1",
            "Bicep TBA 1",
            "Drumsheds",
            null,
            null,
            null,
            EventStatus.OnSale,
            EventProvider.Ticketmaster
        );
        var skEvent = new EventResponse(
            "sk-1",
            "Bicep TBA 2",
            "Drumsheds",
            null,
            null,
            null,
            EventStatus.OnSale,
            EventProvider.Skiddle
        );

        var result = _sut.Deduplicate([tmEvent, skEvent]);

        result.Count.ShouldBe(2);
    }
}
