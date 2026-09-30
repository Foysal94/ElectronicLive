using ElectronicLive.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLive.Api.UnitTests.Services;

public sealed class EmailDispatchingRegistrationTests
{
    [Fact]
    public void Should_RegisterResendEmailDispatcher_WhenApiKeyIsConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Resend:ApiKey"] = "re_live_secret_key_123",
                    ["Resend:FromEmail"] = "alerts@electroniclive.co.uk",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmailDispatching(configuration);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IEmailDispatcher>();

        dispatcher.ShouldNotBeNull();
        dispatcher.ShouldBeOfType<ResendEmailDispatcher>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_FallbackToLoggingEmailDispatcher_WhenApiKeyIsMissingOrBlank(string? apiKey)
    {
        var inMemory = new Dictionary<string, string?>();
        if (apiKey != null)
        {
            inMemory["Resend:ApiKey"] = apiKey;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmailDispatching(configuration);

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IEmailDispatcher>();

        dispatcher.ShouldNotBeNull();
        dispatcher.ShouldBeOfType<LoggingEmailDispatcher>();
    }
}
