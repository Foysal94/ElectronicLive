using ElectronicLive.Api.Models;

namespace ElectronicLive.Api.Services;

internal static class EventDeduplicator
{
    internal static List<EventResponse> Deduplicate(IEnumerable<EventResponse> events)
    {
        var deduplicated = new List<EventResponse>();
        var datedGroups = new Dictionary<(DateOnly Date, string Venue), List<EventResponse>>();

        foreach (var ev in events)
        {
            // Events without a confirmed date cannot be safely correlated across providers
            // without risking false-positive merges for different tour stops.
            if (!ev.Date.HasValue)
            {
                deduplicated.Add(EnsureOffers(ev));
                continue;
            }

            var key = (Date: ev.Date.Value, Venue: NormalizeVenue(ev.VenueName));
            if (!datedGroups.TryGetValue(key, out var group))
            {
                group = [];
                datedGroups[key] = group;
            }

            group.Add(ev);
        }

        foreach (var group in datedGroups.Values)
        {
            if (group.Count == 1)
            {
                deduplicated.Add(EnsureOffers(group[0]));
                continue;
            }

            deduplicated.Add(MergeEvents(group));
        }

        return deduplicated;
    }

    private static EventResponse MergeEvents(List<EventResponse> duplicates)
    {
        // Prioritize the offer with the highest availability status (e.g. OnSale over SoldOut)
        // so the primary ticket link provides immediate buying capability to the caller.
        var primary = duplicates
            .OrderByDescending(e => StatusPriority(e.Status))
            .ThenBy(e => ProviderPriority(e.Provider))
            .First();

        // Prefer earlier start/door time if one provider specifies doors and another show start
        var times = duplicates.Where(e => e.Time.HasValue).Select(e => e.Time!.Value).ToList();
        TimeOnly? earliestTime = times.Count > 0 ? times.Min() : null;

        var offers = duplicates
            .SelectMany(e =>
                e.Offers ?? [new EventTicketOffer(Provider: e.Provider, TicketUrl: e.TicketUrl, Status: e.Status)]
            )
            .DistinctBy(o => o.Provider)
            .ToList();

        return new EventResponse(
            Id: primary.Id,
            Name: primary.Name,
            VenueName: primary.VenueName,
            Date: primary.Date,
            Time: earliestTime,
            TicketUrl: primary.TicketUrl,
            Status: primary.Status,
            Provider: primary.Provider,
            Offers: offers
        );
    }

    private static EventResponse EnsureOffers(EventResponse ev) =>
        ev.Offers is not null
            ? ev
            : ev with
            {
                Offers = [new EventTicketOffer(Provider: ev.Provider, TicketUrl: ev.TicketUrl, Status: ev.Status)],
            };

    private static int StatusPriority(EventStatus status) =>
        status switch
        {
            EventStatus.OnSale => 5,
            EventStatus.SoldOut => 4,
            EventStatus.Postponed => 3,
            EventStatus.Cancelled => 2,
            EventStatus.Unknown => 1,
            _ => 0,
        };

    private static int ProviderPriority(EventProvider provider) =>
        provider switch
        {
            EventProvider.Ticketmaster => 0,
            EventProvider.Skiddle => 1,
            EventProvider.ResidentAdvisor => 2,
            _ => 99,
        };

    // Normalizes venue names by stripping leading articles ("The "), common city suffixes,
    // and non-alphanumeric punctuation to match cross-vendor differences (e.g., "The Drumsheds" vs "Drumsheds, London").
    internal static string NormalizeVenue(string venue)
    {
        if (string.IsNullOrWhiteSpace(venue))
        {
            return string.Empty;
        }

        var v = venue.Trim().ToLowerInvariant();
        if (v.StartsWith("the ", StringComparison.Ordinal))
        {
            v = v[4..].Trim();
        }

        var suffixes = new[] { ", london", " london", ", uk", ", england", ", manchester", " manchester" };
        var matchedSuffix = suffixes.FirstOrDefault(s => v.EndsWith(s, StringComparison.Ordinal));
        if (matchedSuffix != null)
        {
            v = v[..^matchedSuffix.Length].Trim();
        }

        return string.Concat(v.Where(char.IsLetterOrDigit));
    }
}
