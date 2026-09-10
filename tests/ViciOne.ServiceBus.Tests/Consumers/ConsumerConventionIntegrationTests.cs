using System.Diagnostics;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class ConsumerConventionIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONVENTION", "registration-publishes-an-immutable-snapshot")]
    public void Registration_PublishesANewImmutableConventionSnapshot()
    {
        IEnumerable<IConsumerMessageConvention> beforeRegistration = ConsumerConventionCache.GetConventions<SnapshotConsumer>();

        try
        {
            Assert.True(ConsumerConvention.Register(new SnapshotMarkerConsumerConvention()));
            Assert.DoesNotContain(
                beforeRegistration,
                convention => convention is SnapshotMarkerConsumerMessageConvention<SnapshotConsumer>);
            Assert.Contains(
                ConsumerConventionCache.GetConventions<SnapshotConsumer>(),
                convention => convention is SnapshotMarkerConsumerMessageConvention<SnapshotConsumer>);
        }
        finally
        {
            ConsumerConvention.Remove<SnapshotMarkerConsumerConvention>();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONVENTION", "custom-message-only-handler-contract")]
    public async Task CustomConvention_DispatchesEveryDeclaredMessageOnlyHandlerExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var first = new TaskCompletionSource<FirstHandled>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<SecondHandled>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("custom", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            Assert.True(ConsumerConvention.Register<MessageOnlyConsumerConvention>());
            endpoint.Consumer(typeof(MessageOnlyHandler), _ => new MessageOnlyHandler(first, second));
        };

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.Bus.PublishAsync<FirstHandled>(new { Value = "first" }, cancellationToken);
            await harness.Bus.PublishAsync<SecondHandled>(new { Value = "second" }, cancellationToken);

            Assert.Equal("first", (await first.Task.WaitAsync(timeout, cancellationToken)).Value);
            Assert.Equal("second", (await second.Task.WaitAsync(timeout, cancellationToken)).Value);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            ConsumerConvention.Remove<MessageOnlyConsumerConvention>();
        }

        Assert.Single(harness.Consumed.Select<FirstHandled>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<SecondHandled>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONVENTION", "default-consumer-contract")]
    public async Task DefaultConvention_DispatchesTheDeclaredConsumerMessageExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<DefaultHandled>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("default", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Consumer(typeof(DefaultHandler), _ => new DefaultHandler(received));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.Bus.PublishAsync<DefaultHandled>(new { Value = "default" }, cancellationToken);
            Assert.Equal("default", (await received.Task.WaitAsync(timeout, cancellationToken)).Value);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Select<DefaultHandled>(SnapshotOnlyToken()));
    }

    private static InMemoryTestHarness CreateHarness(string suffix, TimeSpan timeout) =>
        new($"consumer-convention-{suffix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public interface FirstHandled
    {
        string Value { get; }
    }

    public interface SecondHandled
    {
        string Value { get; }
    }

    public interface DefaultHandled
    {
        string Value { get; }
    }

    public interface IMessageOnlyHandler<in T>
    {
        void Handle(T message);
    }

    public sealed class MessageOnlyHandler(
        TaskCompletionSource<FirstHandled> first,
        TaskCompletionSource<SecondHandled> second) :
        IMessageOnlyHandler<FirstHandled>,
        IMessageOnlyHandler<SecondHandled>
    {
        public void Handle(FirstHandled message) => first.TrySetResult(message);

        public void Handle(SecondHandled message) => second.TrySetResult(message);
    }

    public sealed class DefaultHandler(TaskCompletionSource<DefaultHandled> received) : IConsumer<DefaultHandled>
    {
        public Task ConsumeAsync(ConsumeContext<DefaultHandled> context)
        {
            received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class SnapshotConsumer;

    private sealed class SnapshotMarkerConsumerConvention : IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>() =>
            new SnapshotMarkerConsumerMessageConvention<T>();
    }

    private sealed class SnapshotMarkerConsumerMessageConvention<T> : IConsumerMessageConvention
        where T : class
    {
        public IEnumerable<IMessageInterfaceType> GetMessageTypes() => [];
    }

    public sealed class MessageOnlyConsumerConvention : IConsumerConvention
    {
        IConsumerMessageConvention IConsumerConvention.GetConsumerMessageConvention<T>() =>
            new MessageOnlyConsumerMessageConvention<T>();
    }

    public sealed class MessageOnlyConsumerMessageConvention<T> : IConsumerMessageConvention
        where T : class
    {
        public IEnumerable<IMessageInterfaceType> GetMessageTypes()
        {
            Type consumerType = typeof(T);
            IEnumerable<MessageOnlyConsumerInterfaceType> types = consumerType.GetInterfaces()
                .Where(candidate => candidate.IsGenericType)
                .Where(candidate => candidate.GetGenericTypeDefinition() == typeof(IMessageOnlyHandler<>))
                .Select(candidate => new MessageOnlyConsumerInterfaceType(candidate.GetGenericArguments()[0], consumerType))
                .Where(candidate => MessageTypeCache.IsValidMessageType(candidate.MessageType));

            foreach (MessageOnlyConsumerInterfaceType type in types)
                yield return type;
        }
    }

    public sealed class MessageOnlyConsumerInterfaceType : IMessageInterfaceType
    {
        private readonly Lazy<IMessageConnectorFactory> _factory;

        public MessageOnlyConsumerInterfaceType(Type messageType, Type consumerType)
        {
            MessageType = messageType;
            _factory = new Lazy<IMessageConnectorFactory>(() => (IMessageConnectorFactory)Activator.CreateInstance(
                typeof(MessageOnlyConnectorFactory<,>).MakeGenericType(consumerType, messageType))!);
        }

        public Type MessageType { get; }

        IConsumerMessageConnector<TMessage> IMessageInterfaceType.GetConsumerConnector<TMessage>() =>
            _factory.Value.CreateConsumerConnector<TMessage>();

        IInstanceMessageConnector<TMessage> IMessageInterfaceType.GetInstanceConnector<TMessage>() =>
            _factory.Value.CreateInstanceConnector<TMessage>();
    }

    public sealed class MessageOnlyConnectorFactory<TConsumer, TMessage> : IMessageConnectorFactory
        where TConsumer : class, IMessageOnlyHandler<TMessage>
        where TMessage : class
    {
        private readonly ConsumerMessageConnector<TConsumer, TMessage> _consumer =
            new(new MessageOnlyConsumerFilter<TConsumer, TMessage>());
        private readonly InstanceMessageConnector<TConsumer, TMessage> _instance =
            new(new MessageOnlyConsumerFilter<TConsumer, TMessage>());

        IConsumerMessageConnector<T> IMessageConnectorFactory.CreateConsumerConnector<T>() =>
            _consumer as IConsumerMessageConnector<T>
            ?? throw new ArgumentException("The consumer type did not match the connector type");

        IInstanceMessageConnector<T> IMessageConnectorFactory.CreateInstanceConnector<T>() =>
            _instance as IInstanceMessageConnector<T>
            ?? throw new ArgumentException("The consumer type did not match the connector type");
    }

    public sealed class MessageOnlyConsumerFilter<TConsumer, TMessage> : IConsumerMessageFilter<TConsumer, TMessage>
        where TConsumer : class, IMessageOnlyHandler<TMessage>
        where TMessage : class
    {
        void IProbeSite.Probe(ProbeContext context)
        {
            ProbeContext scope = context.CreateScope("consume");
            scope.Add("method", $"Handle({TypeCache<TMessage>.ShortName} message)");
        }

        [DebuggerNonUserCode]
        Task IFilter<ConsumerConsumeContext<TConsumer, TMessage>>.SendAsync(
            ConsumerConsumeContext<TConsumer, TMessage> context,
            IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        {
            context.Consumer.Handle(context.Message);
            return Task.CompletedTask;
        }
    }
}
