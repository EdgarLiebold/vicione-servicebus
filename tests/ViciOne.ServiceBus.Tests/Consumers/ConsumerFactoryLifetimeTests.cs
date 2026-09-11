using System.Reflection;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerFactoryLifetimeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-LIFETIME", "owned-success-disposal-matrix")]
    public async Task OwnedFactories_ReleaseEachSuccessfulConsumerExactlyOnceAsync()
    {
        ConsumeContext<FactoryMessage> context = CreateContext();

        DefaultOwnedConsumer.LastInstance = null;
        var defaultFactory = new DefaultConstructorConsumerFactory<DefaultOwnedConsumer>();
        await defaultFactory.SendAsync(context, new ConsumerPipe<DefaultOwnedConsumer>(_ => Task.CompletedTask));
        DefaultOwnedConsumer created = Assert.IsType<DefaultOwnedConsumer>(DefaultOwnedConsumer.LastInstance);
        Assert.Equal(1, created.AsyncDisposeCount);
        Assert.Equal(0, created.DisposeCount);

        var delegateConsumer = new SynchronouslyDisposableConsumer();
        var delegateFactory = new DelegateConsumerFactory<SynchronouslyDisposableConsumer>(() => delegateConsumer);
        await delegateFactory.SendAsync(context, new ConsumerPipe<SynchronouslyDisposableConsumer>(_ => Task.CompletedTask));
        Assert.Equal(1, delegateConsumer.DisposeCount);

        var objectConsumer = new AsynchronouslyDisposableConsumer();
        var objectFactory = new ObjectConsumerFactory<AsynchronouslyDisposableConsumer>(type =>
        {
            Assert.Equal(typeof(AsynchronouslyDisposableConsumer), type);
            return objectConsumer;
        });
        await objectFactory.SendAsync(context, new ConsumerPipe<AsynchronouslyDisposableConsumer>(_ => Task.CompletedTask));
        Assert.Equal(1, objectConsumer.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-LIFETIME", "owned-failure-disposal-and-exception-identity")]
    public async Task OwnedFactory_ReleasesTheConsumerWhenThePipelineFailsWithoutReplacingTheFailureAsync()
    {
        ConsumeContext<FactoryMessage> context = CreateContext();
        var consumer = new AsynchronouslyDisposableConsumer();
        var expected = new ExpectedPipelineException();
        var factory = new DelegateConsumerFactory<AsynchronouslyDisposableConsumer>(() => consumer);

        ExpectedPipelineException actual = await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
            factory.SendAsync(context, new ConsumerPipe<AsynchronouslyDisposableConsumer>(_ => Task.FromException(expected))));

        Assert.Same(expected, actual);
        Assert.Equal(1, consumer.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-LIFETIME", "external-instance-remains-caller-owned")]
    public async Task InstanceFactory_NeverReleasesTheCallerOwnedConsumerAsync()
    {
        ConsumeContext<FactoryMessage> context = CreateContext();
        var consumer = new DefaultOwnedConsumer();
        var expected = new ExpectedPipelineException();
        var factory = new InstanceConsumerFactory<DefaultOwnedConsumer>(consumer);

        await factory.SendAsync(context, new ConsumerPipe<DefaultOwnedConsumer>(scope =>
        {
            Assert.Same(consumer, scope.Consumer);
            return Task.CompletedTask;
        }));
        await Assert.ThrowsAsync<ExpectedPipelineException>(() =>
            factory.SendAsync(context, new ConsumerPipe<DefaultOwnedConsumer>(_ => Task.FromException(expected))));

        Assert.Equal(0, consumer.DisposeCount);
        Assert.Equal(0, consumer.AsyncDisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-LIFETIME", "factory-result-validation-before-pipeline")]
    public async Task FactoryResults_AreValidatedBeforeTheConsumerPipelineAsync()
    {
        ConsumeContext<FactoryMessage> context = CreateContext();
        var calls = 0;
        var pipe = new ConsumerPipe<AsynchronouslyDisposableConsumer>(_ =>
        {
            calls++;
            return Task.CompletedTask;
        });
        var nullFactory = new DelegateConsumerFactory<AsynchronouslyDisposableConsumer>(() => null!);
        var wrongTypeFactory = new ObjectConsumerFactory<AsynchronouslyDisposableConsumer>(_ => new object());

        await Assert.ThrowsAsync<ConsumerException>(() => nullFactory.SendAsync(context, pipe));
        ConsumerException wrongType = await Assert.ThrowsAsync<ConsumerException>(() => wrongTypeFactory.SendAsync(context, pipe));

        Assert.Equal(0, calls);
        Assert.Contains(nameof(AsynchronouslyDisposableConsumer), wrongType.Message, StringComparison.Ordinal);
        Assert.Contains("System.Object", wrongType.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-LIFETIME", "public-argument-boundaries")]
    public async Task ConsumerFactories_RejectMissingRequiredArgumentsAtTheirOwningBoundaryAsync()
    {
        ConsumeContext<FactoryMessage> context = CreateContext();
        var pipe = new ConsumerPipe<AsynchronouslyDisposableConsumer>(_ => Task.CompletedTask);
        var defaultFactory = new DefaultConstructorConsumerFactory<AsynchronouslyDisposableConsumer>();
        var delegateFactory = new DelegateConsumerFactory<AsynchronouslyDisposableConsumer>(() => new AsynchronouslyDisposableConsumer());
        var instance = new AsynchronouslyDisposableConsumer();
        var instanceFactory = new InstanceConsumerFactory<AsynchronouslyDisposableConsumer>(instance);
        var objectFactory = new ObjectConsumerFactory<AsynchronouslyDisposableConsumer>(_ => new AsynchronouslyDisposableConsumer());

        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            new DelegateConsumerFactory<AsynchronouslyDisposableConsumer>(null!)).ParamName);
        Assert.Equal("consumer", Assert.Throws<ArgumentNullException>(() =>
            new InstanceConsumerFactory<AsynchronouslyDisposableConsumer>(null!)).ParamName);
        Assert.Equal("objectFactory", Assert.Throws<ArgumentNullException>(() =>
            new ObjectConsumerFactory<AsynchronouslyDisposableConsumer>(null!)).ParamName);

        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            defaultFactory.SendAsync<FactoryMessage>(null!, pipe))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            defaultFactory.SendAsync(context, null!))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            delegateFactory.SendAsync<FactoryMessage>(null!, pipe))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            instanceFactory.SendAsync(context, null!))).ParamName);
        Assert.Equal("next", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            objectFactory.SendAsync(context, null!))).ParamName);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ((IProbeSite)defaultFactory).Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ((IProbeSite)delegateFactory).Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ((IProbeSite)instanceFactory).Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            ((IProbeSite)objectFactory).Probe(null!)).ParamName);
    }

    private static ConsumeContext<FactoryMessage> CreateContext() =>
        DispatchProxy.Create<TestConsumeContext, UnusedConsumeContextProxy>();

    private interface TestConsumeContext : ConsumeContext<FactoryMessage>, ConsumeContext;

    private sealed record FactoryMessage;

    private sealed class ConsumerPipe<TConsumer>(Func<ConsumerConsumeContext<TConsumer, FactoryMessage>, Task> callback) :
        IPipe<ConsumerConsumeContext<TConsumer, FactoryMessage>>
        where TConsumer : class
    {
        public Task SendAsync(ConsumerConsumeContext<TConsumer, FactoryMessage> context) => callback(context);

        public void Probe(ProbeContext context) => context.CreateScope("consumerFactoryLifetime");
    }

    private sealed class DefaultOwnedConsumer : IDisposable, IAsyncDisposable
    {
        public DefaultOwnedConsumer()
        {
            LastInstance = this;
        }

        public static DefaultOwnedConsumer? LastInstance { get; set; }

        public int AsyncDisposeCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SynchronouslyDisposableConsumer : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class AsynchronouslyDisposableConsumer : IAsyncDisposable
    {
        public int AsyncDisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ExpectedPipelineException : Exception;

    private class UnusedConsumeContextProxy : DispatchProxy
    {
        private readonly ReceiveContext _receiveContext = DispatchProxy.Create<ReceiveContext, UnusedReceiveContextProxy>();
        private readonly SerializerContext _serializerContext = DispatchProxy.Create<SerializerContext, UnusedDependencyProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_ReceiveContext" => _receiveContext,
            "get_SerializerContext" => _serializerContext,
            _ => throw new InvalidOperationException("The lifetime test must not invoke the source consume context."),
        };
    }

    private class UnusedReceiveContextProxy : DispatchProxy
    {
        private readonly IPublishEndpointProvider _publishEndpointProvider =
            DispatchProxy.Create<IPublishEndpointProvider, UnusedDependencyProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_PublishEndpointProvider" => _publishEndpointProvider,
            _ => throw new InvalidOperationException("The lifetime test must not invoke the receive context."),
        };
    }

    private class UnusedDependencyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The lifetime test must not invoke consume-context dependencies.");
    }
}
