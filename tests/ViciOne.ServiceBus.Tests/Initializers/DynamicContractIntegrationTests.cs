using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class DynamicContractIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "anonymous-values-in-memory-send")]
    public async Task AnonymousValues_SendAsAnInterfaceWithEveryValueAndContextIntactAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<ProxyContract> handler = harness.AddHandler<ProxyContract>();
        Guid correlationId = Guid.Parse("beec3a3c-1df8-4d44-aade-e787133d64a8");
        var address = new Uri("https://example.test/proxy/42");

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync<ProxyContract>(
                new
                {
                    CorrelationId = correlationId,
                    Number = 42,
                    Text = "complete",
                    Address = address,
                    Nested = new { Name = "nested", Enabled = true },
                },
                cancellationToken);
            ConsumeContext<ProxyContract> consumed =
                (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal(correlationId, consumed.Message.CorrelationId);
            Assert.Equal(42, consumed.Message.Number);
            Assert.Equal("complete", consumed.Message.Text);
            Assert.Equal(address, consumed.Message.Address);
            Assert.Equal("nested", consumed.Message.Nested.Name);
            Assert.True(consumed.Message.Nested.Enabled);
            Assert.Equal(correlationId, consumed.CorrelationId);
            Assert.Equal(harness.InputQueueAddress, consumed.DestinationAddress);
            Assert.Equal(harness.BusAddress, consumed.SourceAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "raw-json-interface-roundtrip")]
    public async Task RawSystemTextJson_RoundTripsAnEmittedInterfaceAndNestedContractAsync()
    {
        Guid correlationId = Guid.Parse("063e46c5-9c94-4444-925b-33f14e33318f");
        InitializeContext<ProxyContract> initialized = await MessageInitializerCache<ProxyContract>.InitializeAsync(
            new
            {
                CorrelationId = correlationId,
                Number = 7,
                Text = "raw",
                Address = new Uri("https://example.test/raw/7"),
                Nested = new { Name = "child", Enabled = false },
            },
            TestContext.Current.CancellationToken);

        Serialization.SystemTextJsonRawRoundTripResult<ProxyContract> result =
            Serialization.SystemTextJsonRoundTrip.ExecuteRawWithContext(initialized.Message);

        Assert.Equal("application/json", result.ContentType);
        Assert.Equal(correlationId, result.Message.CorrelationId);
        Assert.Equal(7, result.Message.Number);
        Assert.Equal("raw", result.Message.Text);
        Assert.Equal(new Uri("https://example.test/raw/7"), result.Message.Address);
        Assert.Equal("child", result.Message.Nested.Name);
        Assert.False(result.Message.Nested.Enabled);
        Assert.NotEmpty(result.Bytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DYNAMIC-CONTRACT", "generic-interface-wrapper-flow")]
    public async Task ConsumerProducedGenericWrapper_PreservesItsNestedContractsAndCredentialsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<ExecuteSql> command = harness.AddHandler<ExecuteSql>(context =>
            context.Advanced().SendAsync<SecureCommand<ExecuteSql>>(
                harness.InputQueueAddress,
                new
                {
                    Command = context.Message,
                    Credentials = new { Username = "service", Password = "not-a-real-secret" },
                }));
        HandlerTestHarness<SecureCommand<ExecuteSql>> secure = harness.AddHandler<SecureCommand<ExecuteSql>>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new ExecuteSqlCommand("DROP TABLE [TemporaryData]"),
                cancellationToken);
            ConsumeContext<ExecuteSql> commandContext =
                (await command.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;
            ConsumeContext<SecureCommand<ExecuteSql>> secureContext =
                (await secure.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken)).Context;

            Assert.Equal("DROP TABLE [TemporaryData]", commandContext.Message.SqlText);
            Assert.Equal(commandContext.Message.SqlText, secureContext.Message.Command.SqlText);
            Assert.Equal("service", secureContext.Message.Credentials.Username);
            Assert.Equal("not-a-real-secret", secureContext.Message.Credentials.Password);
            Assert.Equal(commandContext.ConversationId, secureContext.ConversationId);
            Assert.Equal(harness.InputQueueAddress, secureContext.SourceAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"dynamic-contract-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public interface ProxyContract : CorrelatedBy<Guid>
    {
        int Number { get; }

        string Text { get; }

        Uri Address { get; }

        NestedContract Nested { get; }
    }

    public interface NestedContract
    {
        string Name { get; }

        bool Enabled { get; }
    }

    public interface SecureCommand<out T>
        where T : class
    {
        T Command { get; }

        UserCredentials Credentials { get; }
    }

    public interface ExecuteSql
    {
        string SqlText { get; }
    }

    public sealed record ExecuteSqlCommand(string SqlText) : ExecuteSql;

    public interface UserCredentials
    {
        string Username { get; }

        string Password { get; }
    }
}
