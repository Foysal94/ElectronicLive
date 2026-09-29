namespace ElectronicLive.Api.Services;

public interface IArtistVerificationService
{
    Task<bool> VerifyArtistExistsAsync(string artistName, CancellationToken cancellationToken = default);
}
