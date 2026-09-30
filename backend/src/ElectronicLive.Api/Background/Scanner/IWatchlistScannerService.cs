namespace ElectronicLive.Api.Background.Scanner;

public interface IWatchlistScannerService
{
    Task<ScanResult> ExecuteScanAsync(CancellationToken ct = default);
}

public sealed record ScanResult(int ArtistsScanned, int SubscriptionsProcessed, int DigestsSent, int ErrorsCount)
{
    public static ScanResult Empty => new(0, 0, 0, 0);

    public static ScanResult operator +(ScanResult left, ArtistScanResult right) =>
        new(
            left.ArtistsScanned + 1,
            left.SubscriptionsProcessed + right.SubscriptionsProcessed,
            left.DigestsSent + right.DigestsSent,
            left.ErrorsCount + right.ErrorsCount
        );
}

public sealed record ArtistScanResult(int SubscriptionsProcessed, int DigestsSent, int ErrorsCount)
{
    public static ArtistScanResult Empty => new(0, 0, 0);
    public static ArtistScanResult Failed => new(0, 0, 1);
}

internal enum SubscriptionProcessOutcome
{
    Sent,
    NoNewEvents,
    Failed,
}
