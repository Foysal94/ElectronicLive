namespace ElectronicLive.Api.Background.Scanner;

public interface IWatchlistScannerService
{
    Task<ScanResult> ExecuteScanAsync(CancellationToken ct = default);
}
