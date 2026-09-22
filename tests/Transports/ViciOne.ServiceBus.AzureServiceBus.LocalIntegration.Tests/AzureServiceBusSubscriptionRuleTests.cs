using global::Azure.Messaging.ServiceBus;
using global::Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests;

public sealed class AzureServiceBusSubscriptionRuleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "sql-filter-delivers-only-the-matching-integer-header")]
    public async Task SqlFilter_DeliversOnlyTheMatchingIntegerHeaderAndDrainsTheSubscriptionAsync()
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
            await bus.PublishAsync(new FilteredMessage(NewId.NextGuid(), "rejected"),
                    context => context.Headers.Set("ClientId", 69), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(new FilteredMessage(selectedId, "selected"),
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
    public async Task ExistingRule_UpdatesFilterAndActionInPlaceWithoutAddingAnotherRuleAsync()
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
            await StartAndStopAsync("0 = 1", "SET A = 1");
            await AssertRuleAsync("0 = 1", "SET A = 1");

            await StartAndStopAsync("1 = 1", "SET A = 2");
            await AssertRuleAsync("1 = 1", "SET A = 2");
        }
        finally
        {
            await fixture.CleanupAsync(admin);
        }

        async Task StartAndStopAsync(string filter, string action)
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

        async Task AssertRuleAsync(string filter, string action)
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

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "missing-configured-rule-rejects-broad-existing-subscription")]
    public async Task MissingConfiguredRule_RejectsExistingSubscriptionWithBroadDefaultRuleAsync()
    {
        await WithExistingSubscriptionAsync("missing-rule", null, async (context, admin, topic, subscription, cancellationToken) =>
        {
            var configuredRule = new CreateRuleOptions("only-27", new SqlRuleFilter("ClientId = 27"));
            ServiceBusException exception = await Assert.ThrowsAsync<ServiceBusException>(() =>
                context.CreateTopicSubscriptionAsync(
                    new CreateSubscriptionOptions(topic, subscription), configuredRule, null, cancellationToken));

            Assert.Equal(ServiceBusFailureReason.MessagingEntityNotFound, exception.Reason);
            RuleProperties defaultRule = await GetOnlyRuleAsync(admin, topic, subscription, cancellationToken);
            Assert.Equal("$Default", defaultRule.Name);
            Assert.IsType<TrueRuleFilter>(defaultRule.Filter);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "existing-generated-rule-updates-filter-without-duplication")]
    public async Task ExistingGeneratedRule_UpdatesItsFilterWithoutAddingAnotherRuleAsync()
    {
        string ruleName = Guid.NewGuid().ToString("D");
        var initialRule = new CreateRuleOptions(ruleName, new SqlRuleFilter("ClientId = 69"));
        await WithExistingSubscriptionAsync("generated-rule-update", initialRule,
            async (context, admin, topic, subscription, cancellationToken) =>
        {
            var options = new CreateSubscriptionOptions(topic, subscription);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                SubscriptionProperties result = await context.CreateTopicSubscriptionAsync(
                    options, null, new SqlRuleFilter("ClientId = 27"), cancellationToken);

                Assert.Equal(topic, result.TopicName);
                Assert.Equal(subscription, result.SubscriptionName);
                RuleProperties currentRule = await GetOnlyRuleAsync(admin, topic, subscription, cancellationToken);
                Assert.Equal(ruleName, currentRule.Name);
                Assert.Equal("ClientId = 27", Assert.IsType<SqlRuleFilter>(currentRule.Filter).SqlExpression);
            }
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "generated-filter-rejects-unmanaged-default-rule")]
    public async Task GeneratedFilter_RejectsBroadDefaultRuleInsteadOfReportingSuccessAsync()
    {
        await WithExistingSubscriptionAsync("unmanaged-default-rule", null,
            async (context, admin, topic, subscription, cancellationToken) =>
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                context.CreateTopicSubscriptionAsync(new CreateSubscriptionOptions(topic, subscription),
                    null, new SqlRuleFilter("ClientId = 27"), cancellationToken));

            Assert.Contains(topic, exception.Message, StringComparison.Ordinal);
            Assert.Contains(subscription, exception.Message, StringComparison.Ordinal);
            RuleProperties defaultRule = await GetOnlyRuleAsync(admin, topic, subscription, cancellationToken);
            Assert.Equal("$Default", defaultRule.Name);
            Assert.IsType<TrueRuleFilter>(defaultRule.Filter);
        });
    }

    static async Task WithExistingSubscriptionAsync(string fixtureName, CreateRuleOptions? initialRule,
        Func<ServiceBusConnectionContext, ServiceBusAdministrationClient, string, string, CancellationToken, Task> verify)
    {
        AzureServiceBusLocalFixture fixture = AzureServiceBusLocalFixture.Create(fixtureName);
        ServiceBusAdministrationClient admin = fixture.CreateAdministrationClient();
        await using ServiceBusClient client = fixture.CreateClient();
        string topic = fixture.Name("topic");
        string subscription = $"vsb-{Guid.NewGuid():N}";
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            await admin.CreateTopicAsync(new CreateTopicOptions(topic)
            {
                DefaultMessageTimeToLive = EmulatorEntityTimeToLive,
            }, cancellationToken);
            if (initialRule == null)
                await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(topic, subscription), cancellationToken);
            else
                await admin.CreateSubscriptionAsync(new CreateSubscriptionOptions(topic, subscription), initialRule, cancellationToken);
            var context = new ServiceBusConnectionContext(client, admin, cancellationToken);
            await verify(context, admin, topic, subscription, cancellationToken);
        }
        finally
        {
            await fixture.CleanupAsync(admin);
        }
    }

    static async Task<RuleProperties> GetOnlyRuleAsync(ServiceBusAdministrationClient admin, string topic,
        string subscription, CancellationToken cancellationToken)
    {
        var rules = new List<RuleProperties>();
        await foreach (RuleProperties rule in admin.GetRulesAsync(topic, subscription, cancellationToken))
            rules.Add(rule);
        return Assert.Single(rules);
    }

    static TaskCompletionSource<T> Observation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    static readonly TimeSpan EmulatorEntityTimeToLive = TimeSpan.FromHours(1);

    public sealed record FilteredMessage(Guid Id, string Value);
}
