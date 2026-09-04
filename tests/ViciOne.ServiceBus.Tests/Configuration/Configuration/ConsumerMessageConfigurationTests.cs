using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ConsumerMessageConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-FACTORY-INTERCEPTION", "around-consumer-exact-order")]
    public async Task ConsumerFactoryFilter_RunsBeforeAndAfterTheExactConsumerInvocationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new LayerObservation();
        var consumer = new LayeredConsumer(observation);
        using var harness = new InMemoryTestHarness($"consumer-factory-filter-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
            endpoint.Consumer(
                () => consumer,
                configuration => configuration.UseFilter(new AroundConsumerFilter(observation)));

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new LayeredMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["before", "consume", "after"], observation.Entries.Select(entry => entry.Layer));
            Assert.All(observation.Entries, entry => Assert.Equal(message, entry.Message));
            Assert.All(observation.Entries, entry => Assert.Same(consumer, entry.Consumer));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(RegistrationShape.ConsumerFactory)]
    [InlineData(RegistrationShape.Instance)]
    [RequirementCoverage("REQ-VSB-CONSUMER-MESSAGE-LAYERING", "consumer-and-instance-registration")]
    public async Task Registration_ComposesConsumerMessageAndConsumerSpecificMessageLayersExactlyOnceAsync(
        RegistrationShape registrationShape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new LayerObservation();
        var consumer = new LayeredConsumer(observation);
        using var harness = new InMemoryTestHarness($"consumer-message-layers-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            void Configure(IConsumerConfigurator<LayeredConsumer> consumerConfiguration)
            {
                consumerConfiguration.UseExecute(context =>
                    observation.Record("consumer", context.Consumer, context.TryGetMessage(out ConsumeContext<LayeredMessage>? messageContext)
                        ? messageContext.Message
                        : null));
                consumerConfiguration.Message<LayeredMessage>(message => message.UseExecute(context =>
                    observation.Record("message", null, context.Message)));
                consumerConfiguration.ConsumerMessage<LayeredMessage>(message => message.UseExecute(context =>
                    observation.Record("consumer-message", context.Consumer, context.Message)));
            }

            if (registrationShape == RegistrationShape.ConsumerFactory)
                endpoint.Consumer(() => consumer, Configure);
            else
                endpoint.Instance(consumer, Configure);
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new LayeredMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(4, observation.Entries.Length);
            Assert.Equal(
                ["message", "consumer", "consumer-message", "consume"],
                observation.Entries.Select(entry => entry.Layer));
            Assert.Equal(1, observation.Entries.Count(entry => entry.Layer == "consumer"));
            Assert.Equal(1, observation.Entries.Count(entry => entry.Layer == "message"));
            Assert.Equal(1, observation.Entries.Count(entry => entry.Layer == "consumer-message"));
            Assert.Equal(1, observation.Entries.Count(entry => entry.Layer == "consume"));
            Assert.All(observation.Entries, entry => Assert.Equal(message, entry.Message));
            Assert.All(
                observation.Entries.Where(entry => entry.Layer is "consumer" or "consumer-message" or "consume"),
                entry => Assert.Same(consumer, entry.Consumer));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public enum RegistrationShape
    {
        ConsumerFactory,
        Instance,
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record LayeredMessage(Guid Id);

    private sealed class LayeredConsumer(LayerObservation observation) : IConsumer<LayeredMessage>
    {
        public Task ConsumeAsync(ConsumeContext<LayeredMessage> context)
        {
            observation.Record("consume", this, context.Message);
            observation.Complete();
            return Task.CompletedTask;
        }
    }

    private sealed class AroundConsumerFilter(LayerObservation observation) :
        IFilter<ConsumerConsumeContext<LayeredConsumer>>
    {
        public async Task SendAsync(
            ConsumerConsumeContext<LayeredConsumer> context,
            IPipe<ConsumerConsumeContext<LayeredConsumer>> next)
        {
            Assert.True(context.TryGetMessage(out ConsumeContext<LayeredMessage>? messageContext));
            observation.Record("before", context.Consumer, messageContext.Message);

            await next.SendAsync(context);

            observation.Record("after", context.Consumer, messageContext.Message);
            observation.Complete();
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("aroundConsumer");
    }

    private sealed class LayerObservation
    {
        private readonly List<LayerEntry> _entries = [];
        private readonly object _lock = new();

        public TaskCompletionSource<bool> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LayerEntry[] Entries
        {
            get
            {
                lock (_lock)
                    return _entries.ToArray();
            }
        }

        public void Record(string layer, LayeredConsumer? consumer, LayeredMessage? message)
        {
            lock (_lock)
                _entries.Add(new LayerEntry(layer, consumer, message));
        }

        public void Complete() => Completed.TrySetResult(true);
    }

    private sealed record LayerEntry(string Layer, LayeredConsumer? Consumer, LayeredMessage? Message);
}
