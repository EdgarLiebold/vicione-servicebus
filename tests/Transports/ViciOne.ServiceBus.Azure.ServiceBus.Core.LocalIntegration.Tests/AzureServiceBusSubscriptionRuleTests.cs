namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.LocalIntegration.Tests;

using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AzureServiceBusSubscriptionRuleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "sql-filter-delivers-only-the-matching-integer-header")]
    public async Task SqlFilter_DeliversOnlyTheMatchingIntegerHeaderAndDrainsTheSubscription()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("sql-filter");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string topic = fixture.Name("topic");
        string subscription = $"vsb-{Guid.NewGuid():N}";
        Guid selectedId = NewId.NextGuid();
        var selected = Observation<ConsumeContext<FilteredMessage>>();
        var received = new List<FilteredMessage>();
        object receivedLock = new();
        IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
        {
            configuration.Host(new Uri("sb://localhost/"), client, admin);
            configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
            configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
            configuration.Message<FilteredMessage>(topology => topology.SetEntityName(topic));
            configuration.Publish<FilteredMessage>(topology =>
                topology.DefaultMessageTimeToLive = EmulatorEntityTimeToLive);
            configuration.SubscriptionEndpoint<FilteredMessage>(subscription, endpoint =>
            {
                endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                endpoint.Rule = new CreateRuleOptions("Only27", new SqlRuleFilter("ClientId = 27"));
                endpoint.Handler<FilteredMessage>(context =>
                {
                    lock (receivedLock)
                        received.Add(context.Message);
                    if (context.Message.Id == selectedId)
                        selected.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.Publish(new FilteredMessage(NewId.NextGuid(), "rejected"),
                    context => context.Headers.Set("ClientId", 69), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish(new FilteredMessage(selectedId, "selected"),
                    context => context.Headers.Set("ClientId", 27), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<FilteredMessage> context = await selected.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal("selected", context.Message.Value);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            FilteredMessage delivered = Assert.Single(received);
            Assert.Equal(selectedId, delivered.Id);
            SubscriptionRuntimeProperties terminal =
                await admin.GetSubscriptionRuntimePropertiesAsync(topic, subscription, cancellationToken);
            Assert.Equal(0, terminal.ActiveMessageCount);
            Assert.Equal(0, terminal.DeadLetterMessageCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync(admin);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "existing-rule-updates-filter-and-action-in-place")]
    public async Task ExistingRule_UpdatesFilterAndActionInPlaceWithoutAddingAnotherRule()
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create("rule-update");
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        string topic = fixture.Name("topic");
        string queue = fixture.Name("input");
        string subscription = $"vsb-{Guid.NewGuid():N}";
        const string rule = "configured-rule";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await admin.CreateTopicAsync(new CreateTopicOptions(topic)
            {
                DefaultMessageTimeToLive = EmulatorEntityTimeToLive,
            }, cancellationToken);
            await StartAndStop("0 = 1", "SET A = 1");
            await AssertRule("0 = 1", "SET A = 1");

            await StartAndStop("1 = 1", "SET A = 2");
            await AssertRule("1 = 1", "SET A = 2");
        }
        finally
        {
            await fixture.CleanupAsync(admin);
        }

        async Task StartAndStop(string filter, string action)
        {
            await using ServiceBusClient client = fixture.CreateClient();
            IBusControl bus = Bus.Factory.CreateUsingAzureServiceBus(configuration =>
            {
                configuration.Host(new Uri("sb://localhost/"), client, admin);
                configuration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                configuration.OverrideDefaultBusEndpointQueueName(fixture.Name("bus"));
                configuration.ReceiveEndpoint(queue, endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                    endpoint.Subscribe(topic, subscription, subscriptionConfiguration =>
                    {
                        subscriptionConfiguration.DefaultMessageTimeToLive = EmulatorEntityTimeToLive;
                        subscriptionConfiguration.Rule = new CreateRuleOptions(rule, new SqlRuleFilter(filter))
                        {
                            Action = new SqlRuleAction(action),
                        };
                    });
                });
            });
            bool started = false;

            try
            {
                await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
                started = true;
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
                started = false;
            }
            finally
            {
                if (started)
                    await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            }
        }

        async Task AssertRule(string filter, string action)
        {
            var rules = new List<RuleProperties>();
            await foreach (RuleProperties current in admin.GetRulesAsync(topic, subscription, cancellationToken))
                rules.Add(current);

            RuleProperties currentRule = Assert.Single(rules);
            Assert.Equal(rule, currentRule.Name);
            Assert.Equal(filter, Assert.IsType<SqlRuleFilter>(currentRule.Filter).SqlExpression);
            Assert.Equal(action, Assert.IsType<SqlRuleAction>(currentRule.Action).SqlExpression);
        }
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record FilteredMessage(Guid Id, string Value);
}
