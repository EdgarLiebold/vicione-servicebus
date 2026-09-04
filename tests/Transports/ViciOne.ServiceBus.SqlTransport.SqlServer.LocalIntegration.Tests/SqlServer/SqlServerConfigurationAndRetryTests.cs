using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerConfigurationAndRetryTests
{
    private static readonly DateTimeOffset StartTime = new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    public static TheoryData<int, bool> ErrorNumbers => new()
    {
        { -2, true },
        { 20, true },
        { 64, true },
        { 233, true },
        { 1205, true },
        { 10053, true },
        { 10054, true },
        { 10060, true },
        { 10928, true },
        { 10929, true },
        { 40197, true },
        { 40143, true },
        { 40501, true },
        { 40613, true },
        { 50000, false },
    };

    [Theory]
    [MemberData(nameof(ErrorNumbers))]
    [RequirementCoverage("OBL-R0-SQL-0109", "sqlserver-native-owner")]
    public void TransientErrorNumberSetIsClosed(int errorNumber, bool expected)
    {
        Assert.Equal(expected, SqlServerDbConnectionContext.IsTransientErrorNumber(errorNumber));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0105", "sqlserver-native-owner")]
    public async Task ProviderPollingDelayIgnoresQueueIdAndCompletesAtTheConfiguredBoundaryOrCancellationAsync()
    {
        var timeProvider = new FakeTimeProvider(StartTime);
        TimeSpan pollingInterval = TimeSpan.FromMinutes(2);
        Task firstQueue = SqlServerDbConnectionContext.DelayUntilMessageReadyAsync(
            17,
            pollingInterval,
            timeProvider,
            CancellationToken.None);
        Task secondQueue = SqlServerDbConnectionContext.DelayUntilMessageReadyAsync(
            9_999,
            pollingInterval,
            timeProvider,
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        Task canceled = SqlServerDbConnectionContext.DelayUntilMessageReadyAsync(
            17,
            pollingInterval,
            timeProvider,
            cancellation.Token);

        cancellation.Cancel();
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.Equal(cancellation.Token, exception.CancellationToken);

        timeProvider.Advance(pollingInterval - TimeSpan.FromTicks(1));
        Assert.False(firstQueue.IsCompleted);
        Assert.False(secondQueue.IsCompleted);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        await Task.WhenAll(firstQueue, secondQueue)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(firstQueue.IsCompletedSuccessfully);
        Assert.True(secondQueue.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0113", "sqlserver-native-owner")]
    public async Task SubSecondLockDurationIsRejectedBeforeTheEndpointCanStartAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "invalid-lock-duration",
            cancellationToken);
        string queueName = fixture.Name("input");

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.LockDuration = TimeSpan.FromMilliseconds(999);
                endpoint.Handler<ConfigurationMessage>(_ => Task.CompletedTask);
            });
        }));

        Assert.Contains(queueName, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Must be >= 1 second", exception.Message, StringComparison.Ordinal);
    }

    private sealed record ConfigurationMessage(Guid Id);
}
