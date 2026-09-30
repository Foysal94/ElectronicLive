namespace ElectronicLive.Api.Background.Scanning;

public interface IWatchlistScannerService
{
    Task<ScanResult> ExecuteScanAsync(CancellationToken ct = default);
}

public sealed record ScanResult(int ArtistsScanned, int SubscriptionsProcessed, int DigestsSent, int ErrorsCount);
