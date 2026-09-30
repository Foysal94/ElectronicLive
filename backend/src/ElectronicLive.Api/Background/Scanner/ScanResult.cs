namespace ElectronicLive.Api.Background.Scanner;

public sealed record ScanResult(int ArtistsScanned, int SubscriptionsProcessed, int DigestsSent, int ErrorsCount);
