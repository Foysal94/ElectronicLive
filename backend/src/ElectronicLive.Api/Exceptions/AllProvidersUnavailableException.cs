namespace ElectronicLive.Api.Exceptions;

public sealed class AllProvidersUnavailableException : Exception
{
    public AllProvidersUnavailableException(string query, int providerCount)
        : base($"All {providerCount} configured event providers failed to return results for query '{query}'.")
    {
        Query = query;
        ProviderCount = providerCount;
    }

    public AllProvidersUnavailableException(string? query, string? genre, int providerCount)
        : base(
            $"All {providerCount} configured event providers failed to return results for search '{(query ?? genre)}'."
        )
    {
        Query = query ?? genre ?? string.Empty;
        ProviderCount = providerCount;
    }

    public string Query { get; }
    public int ProviderCount { get; }
}
