namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class SqlServerConfigurationAndRetryTests
{
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
    [RequirementCoverage("OBL-R0-SQL-0113", "sqlserver-native-owner")]
    public async Task SubSecondLockDurationIsRejectedBeforeTheEndpointCanStart()
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
