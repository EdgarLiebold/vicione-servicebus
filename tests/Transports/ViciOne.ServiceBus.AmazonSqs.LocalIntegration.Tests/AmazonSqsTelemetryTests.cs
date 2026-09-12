using System.Collections.Concurrent;
using System.Diagnostics;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using ActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

[Collection(AmazonSqsTelemetryCollection.Name)]
public sealed class AmazonSqsTelemetryTests
{
    private const string BaggageKey = "vicione.test.trace";
    private const string BaggageValue = "sqs-local-integration";
    private const string CallerSourceName =
        "ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Caller";

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0227", "send-and-consume-emit-exact-correlated-activities-and-tags")]
    public async Task SendAndConsume_EmitExactCorrelatedActivitiesAndTagsAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("telemetry");
        string queueName = fixture.Name("input");
        string topicName = fixture.Name("topic");
        Guid correlationId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        var handled = new TaskCompletionSource<HandlerObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
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
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Message<TelemetryMessage>(message => message.SetEntityName(topicName));
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
            using (Activity caller = Assert.IsType<Activity>(callerSource.StartActivity("sqs caller", ActivityKind.Internal)))
            {
                caller.AddBaggage(BaggageKey, BaggageValue);
                callerContext = caller.Context;
                await bus.PublishAsync(
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
                [.. stoppedActivities.Where(activity => activity.Source.Name == ServiceBusTelemetry.ActivitySourceName
                    && activity.TraceId == callerContext.TraceId)];
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

            Assert.Equal("aws.sns", Tag(send, ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal("aws_sqs", Tag(receive, ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal("aws_sqs", Tag(process, ServiceBusTelemetry.Attributes.MessagingSystem));
            Assert.Equal(messageId.ToString("D"), Tag(send, ServiceBusTelemetry.Attributes.MessageId));
            Assert.Equal(messageId.ToString("D"), Tag(process, ServiceBusTelemetry.Attributes.MessageId));
            Assert.Equal(correlationId.ToString("D"), Tag(send, ServiceBusTelemetry.Attributes.CorrelationId));
            Assert.Equal(correlationId.ToString("D"), Tag(process, ServiceBusTelemetry.Attributes.CorrelationId));
            Assert.Equal(messageUrn, Tag(send, ServiceBusTelemetry.Attributes.MessageContracts));
            Assert.Equal(messageUrn, Tag(process, ServiceBusTelemetry.Attributes.MessageContracts));
            Assert.Equal("Handler", Tag(process, ServiceBusTelemetry.Attributes.ProcessorName));
            Assert.Equal(MessageTypeCache<TelemetryMessage>.DiagnosticAddress, Tag(process, ServiceBusTelemetry.Attributes.MessageContract));
            Assert.Equal(BaggageValue, process.GetBaggageItem(BaggageKey));
            Assert.Equal(queueName, Tag(receive, ServiceBusTelemetry.Attributes.DestinationName));
            Assert.Equal(topicName, Tag(send, ServiceBusTelemetry.Attributes.DestinationName));
            Assert.Equal(bus.Address.ToString(), Tag(send, ServiceBusTelemetry.Attributes.SourceAddress));
            Assert.Equal(send.TraceId, process.TraceId);
            Assert.NotEqual(send.SpanId, process.SpanId);
            Assert.NotNull(Tag(receive, ServiceBusTelemetry.Attributes.MessageId));
            Assert.Contains(queueName, Assert.IsType<string>(Tag(receive, ServiceBusTelemetry.Attributes.InputAddress)), StringComparison.Ordinal);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    public sealed record TelemetryMessage(Guid CorrelationId);

    private static Activity SingleOperation(IEnumerable<Activity> activities, string operation) =>
        Assert.Single(activities, activity => Tag(activity, ServiceBusTelemetry.Attributes.OperationType) == operation);

    private static string? Tag(Activity activity, string key) =>
        activity.TagObjects.SingleOrDefault(tag => tag.Key == key).Value?.ToString();

    private sealed record HandlerObservation(
        ActivityContext Activity,
        string? Baggage,
        Guid PayloadCorrelationId,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? ConversationId);
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AmazonSqsTelemetryCollection
{
    public const string Name = "Amazon SQS process-wide telemetry";
}
