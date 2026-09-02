using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class MessagePipelineActivityTests
{
    private const string CallerSource = "ViciOne.ServiceBus.Tests.MessagePipelineCaller";
    private const string BaggageKey = "vicione-test-baggage";
    private const string BaggageValue = "carried-across-the-message";

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "complete-send-receive-process-trace")]
    public async Task MessageFlow_EmitsOneCompleteTraceWithExactKindsParentsBaggageAndTags()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var recorded = new ConcurrentQueue<Activity>();
        var observation = new ActivityObservation();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source =>
                source.Name == DiagnosticHeaders.DefaultListenerName || source.Name == CallerSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = recorded.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<MessagePipelineActivityConsumer>();
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            using var source = new ActivitySource(CallerSource);
            Activity? caller = source.StartActivity("caller", ActivityKind.Internal);
            Assert.NotNull(caller);
            using (caller)
            {
                caller.AddBaggage(BaggageKey, BaggageValue);
                await harness.Bus.Publish(new ActivityMessage("trace"), cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
                Assert.True(await harness.Consumed.Any<ActivityMessage>(cancellationToken));
            }
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Activity callerActivity = Assert.Single(recorded, activity => activity.Source.Name == CallerSource);
        Activity[] productActivities = recorded
            .Where(activity => activity.Source.Name == DiagnosticHeaders.DefaultListenerName)
            .Where(activity => Tag(activity, DiagnosticHeaders.Messaging.Operation) is "send" or "receive" or "process")
            .ToArray();
        Assert.Equal(3, productActivities.Length);
        Activity send = Assert.Single(productActivities, activity => Operation(activity) == "send");
        Activity receive = Assert.Single(productActivities, activity => Operation(activity) == "receive");
        Activity process = Assert.Single(productActivities, activity => Operation(activity) == "process");

        Assert.Equal(ActivityKind.Producer, send.Kind);
        Assert.Equal(ActivityKind.Consumer, receive.Kind);
        Assert.Equal(ActivityKind.Consumer, process.Kind);
        Assert.Equal(callerActivity.TraceId, send.TraceId);
        Assert.Equal(callerActivity.TraceId, receive.TraceId);
        Assert.Equal(callerActivity.TraceId, process.TraceId);
        Assert.Equal(callerActivity.SpanId, send.ParentSpanId);
        Assert.Equal(send.SpanId, receive.ParentSpanId);
        Assert.Equal(receive.SpanId, process.ParentSpanId);
        Assert.Equal(BaggageValue, observation.Baggage);
        Assert.Equal(BaggageValue, process.GetBaggageItem(BaggageKey));
        Assert.Equal("in-memory", Tag(send, DiagnosticHeaders.Messaging.System));
        Assert.False(string.IsNullOrWhiteSpace(Tag(send, DiagnosticHeaders.Messaging.DestinationName)));
        Assert.False(string.IsNullOrWhiteSpace(Tag(receive, DiagnosticHeaders.Messaging.DestinationName)));
        Assert.Contains(nameof(MessagePipelineActivityConsumer), Tag(process, DiagnosticHeaders.ConsumerType), StringComparison.Ordinal);
    }

    private static string? Operation(Activity activity) =>
        Tag(activity, DiagnosticHeaders.Messaging.Operation);

    private static string? Tag(Activity activity, string name) =>
        activity.TagObjects.FirstOrDefault(tag => tag.Key == name).Value?.ToString();

    public sealed record ActivityMessage(string Value);

    private sealed class ActivityObservation
    {
        public string? Baggage { get; set; }
    }

    private sealed class MessagePipelineActivityConsumer(ActivityObservation observation) : IConsumer<ActivityMessage>
    {
        public Task Consume(ConsumeContext<ActivityMessage> context)
        {
            observation.Baggage = Activity.Current?.GetBaggageItem(BaggageKey);
            return Task.CompletedTask;
        }
    }
}
