using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSubscriptionReconciliationRaceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-RULE", "concurrent-creation-reconciles-winner-settings-and-rule")]
    public async Task ConcurrentCreation_ReconcilesTheWinnerSubscriptionAndNamedRuleAsync()
    {
        const string topic = "topic";
        const string subscription = "subscription";
        const string ruleName = "only-27";
        using var caller = new CancellationTokenSource();
        var administration = new RacingAdministrationClient(topic, subscription, ruleName, caller.Token);
        var client = new StubServiceBusClient();
        var context = new ServiceBusConnectionContext(client, administration, caller.Token);
        var desired = new CreateSubscriptionOptions(topic, subscription) { MaxDeliveryCount = 17 };
        var desiredRule = new CreateRuleOptions(ruleName, new SqlRuleFilter("ClientId = 27"));

        SubscriptionProperties result = await context.CreateTopicSubscriptionAsync(desired, desiredRule, null, caller.Token);

        Assert.Equal(2, administration.GetSubscriptionCalls);
        Assert.Equal(1, administration.CreateSubscriptionCalls);
        Assert.Equal(1, administration.UpdateSubscriptionCalls);
        Assert.Equal(1, administration.GetRuleCalls);
        Assert.Equal(1, administration.UpdateRuleCalls);
        Assert.Equal(topic, result.TopicName);
        Assert.Equal(subscription, result.SubscriptionName);
        Assert.Equal(17, result.MaxDeliveryCount);
        Assert.Equal(17, administration.Winner.MaxDeliveryCount);
        Assert.Equal(ruleName, administration.WinnerRule.Name);
        Assert.Equal("ClientId = 27", Assert.IsType<SqlRuleFilter>(administration.WinnerRule.Filter).SqlExpression);
    }

    private sealed class StubServiceBusClient : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => "unit.servicebus.invalid";
    }

    private sealed class RacingAdministrationClient : ServiceBusAdministrationClient
    {
        private readonly string _topic;
        private readonly string _subscription;
        private readonly string _ruleName;
        private readonly CancellationToken _callerToken;
        private readonly StubResponse _response = new();

        public RacingAdministrationClient(string topic, string subscription, string ruleName, CancellationToken callerToken)
        {
            _topic = topic;
            _subscription = subscription;
            _ruleName = ruleName;
            _callerToken = callerToken;
            var defaults = new CreateSubscriptionOptions(topic, subscription);
            Winner = ServiceBusModelFactory.SubscriptionProperties(topic, subscription,
                defaults.LockDuration, defaults.RequiresSession, defaults.DefaultMessageTimeToLive,
                defaults.AutoDeleteOnIdle, defaults.DeadLetteringOnMessageExpiration, 5,
                defaults.EnableBatchedOperations, EntityStatus.Active, string.Empty, string.Empty, string.Empty);
            WinnerRule = ServiceBusModelFactory.RuleProperties(ruleName, new SqlRuleFilter("ClientId = 69"), null!);
        }

        public SubscriptionProperties Winner { get; }
        public RuleProperties WinnerRule { get; }
        public int GetSubscriptionCalls { get; private set; }
        public int CreateSubscriptionCalls { get; private set; }
        public int UpdateSubscriptionCalls { get; private set; }
        public int GetRuleCalls { get; private set; }
        public int UpdateRuleCalls { get; private set; }

        public override Task<global::Azure.Response<SubscriptionProperties>> GetSubscriptionAsync(
            string topicName, string subscriptionName, CancellationToken cancellationToken = default)
        {
            AssertRequest(topicName, subscriptionName, cancellationToken);
            GetSubscriptionCalls++;
            if (GetSubscriptionCalls == 1)
                throw new ServiceBusException(false, "absent on first read", subscriptionName,
                    ServiceBusFailureReason.MessagingEntityNotFound, null);
            Assert.Equal(2, GetSubscriptionCalls);
            return Task.FromResult(global::Azure.Response.FromValue(Winner, _response));
        }

        public override Task<global::Azure.Response<SubscriptionProperties>> CreateSubscriptionAsync(
            CreateSubscriptionOptions options, CreateRuleOptions rule, CancellationToken cancellationToken = default)
        {
            AssertRequest(options.TopicName, options.SubscriptionName, cancellationToken);
            Assert.Equal(_ruleName, rule.Name);
            Assert.Equal("ClientId = 27", Assert.IsType<SqlRuleFilter>(rule.Filter).SqlExpression);
            Assert.Equal(17, options.MaxDeliveryCount);
            CreateSubscriptionCalls++;
            throw new ServiceBusException(false, "created by competitor", options.SubscriptionName,
                ServiceBusFailureReason.MessagingEntityAlreadyExists, null);
        }

        public override Task<global::Azure.Response<SubscriptionProperties>> UpdateSubscriptionAsync(
            SubscriptionProperties properties, CancellationToken cancellationToken = default)
        {
            AssertRequest(properties.TopicName, properties.SubscriptionName, cancellationToken);
            Assert.Same(Winner, properties);
            Assert.Equal(17, properties.MaxDeliveryCount);
            UpdateSubscriptionCalls++;
            return Task.FromResult(global::Azure.Response.FromValue(properties, _response));
        }

        public override Task<global::Azure.Response<RuleProperties>> GetRuleAsync(
            string topicName, string subscriptionName, string ruleName, CancellationToken cancellationToken = default)
        {
            AssertRequest(topicName, subscriptionName, cancellationToken);
            Assert.Equal(_ruleName, ruleName);
            GetRuleCalls++;
            return Task.FromResult(global::Azure.Response.FromValue(WinnerRule, _response));
        }

        public override Task<global::Azure.Response<RuleProperties>> UpdateRuleAsync(
            string topicName, string subscriptionName, RuleProperties properties, CancellationToken cancellationToken = default)
        {
            AssertRequest(topicName, subscriptionName, cancellationToken);
            Assert.Same(WinnerRule, properties);
            Assert.Equal("ClientId = 27", Assert.IsType<SqlRuleFilter>(properties.Filter).SqlExpression);
            UpdateRuleCalls++;
            return Task.FromResult(global::Azure.Response.FromValue(properties, _response));
        }

        private void AssertRequest(string topicName, string subscriptionName, CancellationToken cancellationToken)
        {
            Assert.Equal(_topic, topicName);
            Assert.Equal(_subscription, subscriptionName);
            Assert.Equal(_callerToken, cancellationToken);
        }
    }

    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "subscription-race-test";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
        protected override bool TryGetHeader(string name, out string value)
        {
            value = null!;
            return false;
        }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = null!;
            return false;
        }
    }
}
