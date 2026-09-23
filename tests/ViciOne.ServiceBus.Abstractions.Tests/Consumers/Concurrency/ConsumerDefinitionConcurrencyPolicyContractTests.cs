using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Consumers.Concurrency;

public sealed class ConsumerDefinitionConcurrencyPolicyContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "definition-policy-projects-legacy-limit-and-actual-consumer-configuration")]
    public void PolicyTransitions_ProjectTheLegacyLimitAndConfiguredConsumerPolicy()
    {
        var definition = new Definition();
        ConsumerConcurrencyPolicy parallel = ConsumerConcurrencyPolicy.Parallel(17);

        definition.SetPolicy(parallel);
        Assert.Same(parallel, definition.Policy);
        Assert.Equal(17, definition.ConcurrentMessageLimit);
        Assert.Same(parallel, ConfiguredPolicy(definition));

        definition.SetPolicy(ConsumerConcurrencyPolicy.Serial);
        Assert.Same(ConsumerConcurrencyPolicy.Serial, definition.Policy);
        Assert.Null(definition.ConcurrentMessageLimit);
        Assert.Same(ConsumerConcurrencyPolicy.Serial, ConfiguredPolicy(definition));

        definition.SetPolicy(null);
        Assert.Null(definition.Policy);
        Assert.Null(definition.ConcurrentMessageLimit);
        Assert.Null(ConfiguredPolicy(definition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "definition-rejects-untyped-partitioned-policy-without-changing-effective-policy")]
    public void PartitionedPolicy_IsRejectedWithoutChangingTheEffectivePolicy()
    {
        var definition = new Definition();
        ConsumerConcurrencyPolicy previous = ConsumerConcurrencyPolicy.Parallel(3);
        definition.SetPolicy(previous);

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            definition.SetPolicy(ConsumerConcurrencyPolicy.Partitioned(4)));

        Assert.Equal("value", error.ParamName);
        Assert.Same(previous, definition.Policy);
        Assert.Equal(3, definition.ConcurrentMessageLimit);
        Assert.Same(previous, ConfiguredPolicy(definition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "definition-legacy-limit-creates-and-clears-effective-parallel-policy")]
    public void LegacyLimit_CreatesAndClearsTheConfiguredParallelPolicy()
    {
        var definition = new Definition();

        definition.SetLimit(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency);
        Assert.Equal(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency, definition.ConcurrentMessageLimit);
        Assert.Equal(ConsumerConcurrencyMode.Parallel, definition.Policy?.Mode);
        Assert.Equal(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency, ConfiguredPolicy(definition)?.Concurrency);

        definition.SetLimit(null);
        Assert.Null(definition.Policy);
        Assert.Null(definition.ConcurrentMessageLimit);
        Assert.Null(ConfiguredPolicy(definition));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ConsumerConcurrencyPolicy.AbsoluteMaximumConcurrency + 1)]
    [RequirementCoverage("REQ-VSB-V5-CONSUMER-CONCURRENCY", "definition-invalid-legacy-limit-preserves-effective-policy")]
    public void InvalidLegacyLimit_PreservesThePreviousEffectivePolicy(int invalidLimit)
    {
        var definition = new Definition();
        ConsumerConcurrencyPolicy previous = ConsumerConcurrencyPolicy.Parallel(5);
        definition.SetPolicy(previous);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            definition.SetLimit(invalidLimit));

        Assert.Equal("maximumConcurrency", error.ParamName);
        Assert.Same(previous, definition.Policy);
        Assert.Equal(5, definition.ConcurrentMessageLimit);
        Assert.Same(previous, ConfiguredPolicy(definition));
    }

    private static ConsumerConcurrencyPolicy? ConfiguredPolicy(Definition definition)
    {
        ConsumerConcurrencyPolicy? configured = null;
        IConsumerConfigurator<Consumer> consumer = Proxy<IConsumerConfigurator<Consumer>>((method, args) =>
        {
            Assert.Equal("set_ConcurrencyPolicy", method.Name);
            configured = Assert.IsType<ConsumerConcurrencyPolicy>(Assert.Single(args));
            return null;
        });
        IReceiveEndpointConfigurator endpoint = Proxy<IReceiveEndpointConfigurator>((method, _) =>
            throw new InvalidOperationException($"Unexpected endpoint invocation: {method.Name}"));
        IRegistrationContext registration = Proxy<IRegistrationContext>((method, _) =>
            throw new InvalidOperationException($"Unexpected registration invocation: {method.Name}"));

        ((IConsumerDefinition<Consumer>)definition).Configure(endpoint, consumer, registration);

        return configured;
    }

    private static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T result = DispatchProxy.Create<T, CallProxy>();
        ((CallProxy)(object)result).Handler = handler;
        return result;
    }

    public class CallProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(Assert.IsAssignableFrom<MethodInfo>(targetMethod), args ?? []);
    }

    private sealed class Definition : ConsumerDefinition<Consumer>
    {
        public ConsumerConcurrencyPolicy? Policy => ConcurrencyPolicy;
        public void SetPolicy(ConsumerConcurrencyPolicy? value) => ConcurrencyPolicy = value;
        public void SetLimit(int? value) => ConcurrentMessageLimit = value;
    }

    public sealed class Consumer : IConsumer;
}
