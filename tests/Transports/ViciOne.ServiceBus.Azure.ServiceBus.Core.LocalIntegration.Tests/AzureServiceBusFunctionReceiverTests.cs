using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests;

public sealed class AzureServiceBusFunctionReceiverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTION-RECEIVER", "retry-exhaustion-rethrows-and-publishes-one-fault")]
    public async Task FaultingFunctionConsumer_RetriesExactlyAndPublishesOneTerminalFault()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("function");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        var services = new ServiceCollection();
        services.AddSingleton<AttemptLedger>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddAzureFunctionsTestComponents();
            configuration.AddConsumer<FaultingFunctionConsumer, FaultingFunctionConsumerDefinition>();
            configuration.UsingAzureServiceBus((_, bus) =>
            {
                bus.Host(new Uri("sb://localhost/"), client, admin);
                bus.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
                bus.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
                bus.UseRawJsonDeserializer(isDefault: true);
                bus.Publish<Fault>(topology => topology.DefaultMessageTimeToLive = TimeSpan.FromHours(1));
                bus.Publish<Fault<FunctionMessage>>(topology => topology.DefaultMessageTimeToLive = TimeSpan.FromHours(1));
            });
        });
        ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        ITestHarness harness = provider.GetTestHarness();
        AttemptLedger ledger = provider.GetRequiredService<AttemptLedger>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            FunctionFailureException exception = await Assert.ThrowsAsync<FunctionFailureException>(
                () => harness.HandleConsumer<FaultingFunctionConsumer>(new FunctionMessage())
                    .WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.Same(ledger.Failure, exception);
            Assert.Equal(4, ledger.Attempts);
            IPublishedMessage<Fault<FunctionMessage>> fault = Assert.Single(
                harness.Published.Select<Fault<FunctionMessage>>(new CancellationToken(canceled: true)));
            Assert.Equal(ledger.Failure.Message, Assert.Single(fault.Context.Message.Exceptions).Message);
        }
        finally
        {
            try
            {
                await provider.DisposeAsync().AsTask().WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            }
            finally
            {
                await fixture.CleanupAsync(admin);
            }
        }
    }

    public sealed class FunctionMessage;

    public sealed class AttemptLedger
    {
        int _attempts;

        public FunctionFailureException Failure { get; } = new("function-receiver-failure");

        public int Attempts => Volatile.Read(ref _attempts);

        public void Record() => Interlocked.Increment(ref _attempts);
    }

    public sealed class FaultingFunctionConsumer(AttemptLedger ledger) : IConsumer<FunctionMessage>
    {
        public Task Consume(ConsumeContext<FunctionMessage> context)
        {
            ledger.Record();
            throw ledger.Failure;
        }
    }

    public sealed class FaultingFunctionConsumerDefinition : ConsumerDefinition<FaultingFunctionConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<FaultingFunctionConsumer> consumerConfigurator,
            IRegistrationContext context) =>
            endpointConfigurator.UseMessageRetry(retry => retry.Immediate(3));
    }

    public sealed class FunctionFailureException(string message) : Exception(message);
}
