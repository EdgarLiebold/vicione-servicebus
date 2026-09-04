using System.Collections.Concurrent;
using System.Diagnostics;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

[Collection(ActiveMqTelemetryCollection.Name)]
public sealed class ActiveMqTelemetryTests
{
    private const string BaggageKey = "vicione.test.trace";
    private const string BaggageValue = "activemq-local-integration";
    private const string CallerSourceName =
        "ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Caller";

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0440", "send-and-consume-emit-exact-correlated-activities-and-tags")]
    public async Task SendAndConsume_EmitExactCorrelatedActivitiesAndTags(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "telemetry");
        string queueName = fixture.Name("input");
        string topicName = fixture.Name("topic");
        Guid correlationId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        var handled = NewObservation<HandlerObservation>();
        var stoppedActivities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is ServiceBusTelemetry.ActivitySourceName or CallerSourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using var callerSource = new ActivitySource(CallerSourceName, "1.0.0");
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<TelemetryMessage>().SetEntityName(topicName);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<TelemetryMessage>(context =>
            {
                Activity current = Assert.IsType<Activity>(Activity.Current);
                handled.TrySetResult(new HandlerObservation(
                    current.Context,
                    current.GetBaggageItem(BaggageKey),
                    context.Message.CorrelationId,
                    context.MessageId,
                    context.CorrelationId,
                    context.ConversationId));
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        ActivityContext callerContext;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            using (Activity caller = Assert.IsType<Activity>(callerSource.StartActivity("activemq caller", ActivityKind.Internal)))
            {
                caller.AddBaggage(BaggageKey, BaggageValue);
                callerContext = caller.Context;
                await bus.Publish(
                        new TelemetryMessage(correlationId),
                        context =>
                        {
                            context.MessageId = messageId;
                            context.CorrelationId = correlationId;
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);

                HandlerObservation observation = await handled.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
                Assert.Equal(callerContext.TraceId, observation.Activity.TraceId);
                Assert.Equal(BaggageValue, observation.Baggage);
                Assert.Equal(correlationId, observation.PayloadCorrelationId);
                Assert.Equal(messageId, observation.MessageId);
                Assert.Equal(correlationId, observation.CorrelationId);
                Assert.NotNull(observation.ConversationId);
            }

            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Activity[] activities =
            [
                .. stoppedActivities.Where(activity =>
                    activity.Source.Name == ServiceBusTelemetry.ActivitySourceName
                    && activity.TraceId == callerContext.TraceId),
            ];
            Activity send = SingleOperation(activities, "send");
            Activity receive = SingleOperation(activities, "receive");
            Activity process = SingleOperation(activities, "process");
            string version = Assert.IsType<string>(HostMetadataCache.Host.ViciOneServiceBusVersion);
            string messageUrn = MessageUrn.ForTypeString<TelemetryMessage>();

            Assert.All(activities, activity =>
            {
                Assert.Equal(ServiceBusTelemetry.ActivitySourceName, activity.Source.Name);
                Assert.Equal(version, activity.Source.Version);
                Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            });
            Assert.Equal(ActivityKind.Producer, send.Kind);
            Assert.Equal(ActivityKind.Consumer, receive.Kind);
            Assert.Equal(ActivityKind.Consumer, process.Kind);
            Assert.Equal(callerContext.SpanId, send.ParentSpanId);
            Assert.Equal(send.SpanId, receive.ParentSpanId);
            Assert.Equal(receive.SpanId, process.ParentSpanId);
            Assert.Equal("activemq", Tag(send, DiagnosticHeaders.Messaging.System));
            Assert.Equal("activemq", Tag(receive, DiagnosticHeaders.Messaging.System));
            Assert.Equal("activemq", Tag(process, DiagnosticHeaders.Messaging.System));
            Assert.Equal(messageId.ToString("D"), Tag(send, DiagnosticHeaders.MessageId));
            Assert.Equal(messageId.ToString("D"), Tag(process, DiagnosticHeaders.MessageId));
            Assert.Equal(correlationId.ToString("D"), Tag(send, DiagnosticHeaders.CorrelationId));
            Assert.Equal(correlationId.ToString("D"), Tag(process, DiagnosticHeaders.CorrelationId));
            Assert.Equal(messageUrn, Tag(send, DiagnosticHeaders.MessageTypes));
            Assert.Equal(messageUrn, Tag(process, DiagnosticHeaders.MessageTypes));
            Assert.Equal("Handler", Tag(process, DiagnosticHeaders.ConsumerType));
            Assert.Equal(MessageTypeCache<TelemetryMessage>.DiagnosticAddress, Tag(process, DiagnosticHeaders.PeerAddress));
            Assert.Equal(BaggageValue, process.GetBaggageItem(BaggageKey));
            Assert.Equal(queueName, Tag(receive, DiagnosticHeaders.Messaging.DestinationName));
            string expectedDestination = $"VirtualTopic.{topicName}";
            Assert.Equal(expectedDestination, Tag(send, DiagnosticHeaders.Messaging.DestinationName));
            Assert.Equal(bus.Address.ToString(), Tag(send, DiagnosticHeaders.SourceAddress));
            Assert.Equal(send.TraceId, process.TraceId);
            Assert.NotEqual(send.SpanId, process.SpanId);
            Assert.NotNull(Tag(receive, DiagnosticHeaders.Messaging.TransportMessageId));
            Assert.Contains(queueName, Assert.IsType<string>(Tag(receive, DiagnosticHeaders.InputAddress)), StringComparison.Ordinal);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Activity SingleOperation(IEnumerable<Activity> activities, string operation) =>
        Assert.Single(activities, activity => Tag(activity, DiagnosticHeaders.Messaging.Operation) == operation);

    private static string? Tag(Activity activity, string key) =>
        activity.TagObjects.SingleOrDefault(tag => tag.Key == key).Value?.ToString();

    public sealed record TelemetryMessage(Guid CorrelationId);

    private sealed record HandlerObservation(
        ActivityContext Activity,
        string? Baggage,
        Guid PayloadCorrelationId,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? ConversationId);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ActiveMqTelemetryCollection
{
    public const string Name = "ActiveMQ process-wide telemetry";
}
