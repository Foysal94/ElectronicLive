using ElectronicLive.Api.Background.Scanning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace ElectronicLive.Api.UnitTests.Background.Scanning;

public sealed class JobRunnerTests
{
    [Theory]
    [InlineData(new[] { "--job", "scan-watchlist" }, "scan-watchlist", true)]
    [InlineData(new[] { "--JOB", "SCAN-WATCHLIST" }, "scan-watchlist", true)]
    [InlineData(new[] { "--job=scan-watchlist" }, "scan-watchlist", true)]
    [InlineData(new[] { "--other", "flag", "--job", "scan-watchlist" }, "scan-watchlist", true)]
    [InlineData(new[] { "--job", "other-job" }, "scan-watchlist", false)]
    [InlineData(new string[0], "scan-watchlist", false)]
    [InlineData(new[] { "--job" }, "scan-watchlist", false)]
    public void Should_DetectJobInvocationCorrectly(string[] args, string jobName, bool expectedResult)
    {
        var result = Program.IsJobInvocation(args, jobName);
        result.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task Should_ReturnZero_WhenScannerSucceedsWithNoErrors()
    {
        var scanner = Substitute.For<IWatchlistScannerService>();
        scanner
            .ExecuteScanAsync(Arg.Any<CancellationToken>())
            .Returns(new ScanResult(ArtistsScanned: 2, SubscriptionsProcessed: 2, DigestsSent: 2, ErrorsCount: 0));

        var services = new ServiceCollection();
        services.AddScoped(_ => scanner);
        services.AddSingleton<ILogger<Program>>(NullLogger<Program>.Instance);
        var provider = services.BuildServiceProvider();

        var exitCode = await Program.ExecuteScanWatchlistJobAsync(provider);

        exitCode.ShouldBe(0);
        await scanner.Received(1).ExecuteScanAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnZero_WhenScannerReportsErrorsHandledGracefully()
    {
        var scanner = Substitute.For<IWatchlistScannerService>();
        scanner
            .ExecuteScanAsync(Arg.Any<CancellationToken>())
            .Returns(new ScanResult(ArtistsScanned: 2, SubscriptionsProcessed: 2, DigestsSent: 1, ErrorsCount: 1));

        var services = new ServiceCollection();
        services.AddScoped(_ => scanner);
        services.AddSingleton<ILogger<Program>>(NullLogger<Program>.Instance);
        var provider = services.BuildServiceProvider();

        var exitCode = await Program.ExecuteScanWatchlistJobAsync(provider);

        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Should_ReturnOne_WhenScannerThrowsUnhandledException()
    {
        var scanner = Substitute.For<IWatchlistScannerService>();
        scanner
            .ExecuteScanAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Fatal database connection crash"));

        var services = new ServiceCollection();
        services.AddScoped(_ => scanner);
        services.AddSingleton<ILogger<Program>>(NullLogger<Program>.Instance);
        var provider = services.BuildServiceProvider();

        var exitCode = await Program.ExecuteScanWatchlistJobAsync(provider);

        exitCode.ShouldBe(1);
    }
}
