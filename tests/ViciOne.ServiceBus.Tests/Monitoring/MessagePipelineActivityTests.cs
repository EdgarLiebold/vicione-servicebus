using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private const string TraceState = "vicione=review-integration";

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
                caller.TraceStateString = TraceState;
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
        Assert.Equal(TraceState, send.TraceStateString);
        Assert.Equal(TraceState, receive.TraceStateString);
        Assert.Equal(TraceState, observation.TraceStateHeader);
        Assert.Equal(BaggageValue, observation.Baggage);
        Assert.Equal(BaggageValue, observation.BaggageHeader);
        Assert.Equal(BaggageValue, process.GetBaggageItem(BaggageKey));
        Assert.Equal("in-memory", Tag(send, DiagnosticHeaders.Messaging.System));
        Assert.False(string.IsNullOrWhiteSpace(Tag(send, DiagnosticHeaders.Messaging.DestinationName)));
        Assert.False(string.IsNullOrWhiteSpace(Tag(receive, DiagnosticHeaders.Messaging.DestinationName)));
        Assert.Contains(nameof(MessagePipelineActivityConsumer), Tag(process, DiagnosticHeaders.ConsumerType), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "unsampled-parent-context-propagation")]
    public async Task UnsampledServiceBusActivity_StillPropagatesParentIdTraceStateAndBaggage()
    {
        HostileFlowResult result = await RunActivityFlow(
            static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None);

        Assert.Equal(1, result.Observation.DeliveryCount);
        Assert.Equal(result.CallerId, result.Observation.ActivityIdHeader);
        Assert.Equal(TraceState, result.Observation.TraceStateHeader);
        Assert.Equal(BaggageValue, result.Observation.BaggageHeader);
        Assert.DoesNotContain(DiagnosticHeaders.CorrelationId, result.Observation.BaggageHeaderKeys);
        Assert.DoesNotContain(DiagnosticHeaders.Messaging.ConversationId, result.Observation.BaggageHeaderKeys);
        Assert.DoesNotContain("empty-baggage", result.Observation.BaggageHeaderKeys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "sample-and-logging-fault-isolation")]
    public async Task ThrowingSampleAndSecondaryLogger_DoNotChangeExactlyOnceDeliveryOrParentPropagation()
    {
        HostileFlowResult result = await RunActivityFlow(
            static (ref ActivityCreationOptions<ActivityContext> _) => throw new HostileTelemetryException("sample"),
            useThrowingLogger: true);

        Assert.Equal(1, result.Observation.DeliveryCount);
        Assert.Equal(result.CallerId, result.Observation.ActivityIdHeader);
        Assert.Equal(TraceState, result.Observation.TraceStateHeader);
        Assert.Equal(BaggageValue, result.Observation.BaggageHeader);
    }

    [Theory]
    [InlineData(CallbackFault.ActivityStarted)]
    [InlineData(CallbackFault.ActivityStopped)]
    [RequirementCoverage("REQ-VSB-MESSAGE-ACTIVITY", "start-and-stop-callback-fault-isolation")]
    public async Task ThrowingStartOrStopCallback_DoesNotChangeExactlyOnceDelivery(CallbackFault callbackFault)
    {
        HostileFlowResult result = await RunActivityFlow(
            static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            callbackFault);

        Assert.Equal(1, result.Observation.DeliveryCount);
        if (callbackFault == CallbackFault.ActivityStarted)
            Assert.Equal(result.CallerId, result.Observation.ActivityIdHeader);
        else
            Assert.NotEqual(result.CallerId, result.Observation.ActivityIdHeader);
    }

    private static async Task<HostileFlowResult> RunActivityFlow(
        SampleActivity<ActivityContext> sample,
        CallbackFault callbackFault = CallbackFault.None,
        bool useThrowingLogger = false)
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ActivityObservation();
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
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DiagnosticHeaders.DefaultListenerName,
            Sample = sample,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = _ => ThrowIfSelected(callbackFault, CallbackFault.ActivityStarted),
            ActivityStopped = _ => ThrowIfSelected(callbackFault, CallbackFault.ActivityStopped),
        };
        ILogContext previousLogContext = LogContext.Current;

        try
        {
            ActivitySource.AddActivityListener(listener);
            if (useThrowingLogger)
                LogContext.ConfigureCurrentLogContext(new ThrowingLogger());

            using var caller = new Activity("hostile-listener-caller");
            caller.SetIdFormat(ActivityIdFormat.W3C);
            caller.TraceStateString = TraceState;
            caller.AddBaggage(BaggageKey, BaggageValue);
            caller.AddBaggage(DiagnosticHeaders.CorrelationId, Guid.NewGuid().ToString("D"));
            caller.AddBaggage(DiagnosticHeaders.Messaging.ConversationId, Guid.NewGuid().ToString("D"));
            caller.AddBaggage("empty-baggage", "   ");
            caller.Start();
            string callerId = Assert.IsType<string>(caller.Id);

            await harness.Bus.Publish(new ActivityMessage("hostile-listener"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Assert.True(await harness.Consumed.Any<ActivityMessage>(cancellationToken));

            return new HostileFlowResult(observation, callerId);
        }
        finally
        {
            LogContext.Current = previousLogContext;
            listener.Dispose();
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void ThrowIfSelected(CallbackFault actual, CallbackFault selected)
    {
        if (actual == selected)
            throw new HostileTelemetryException(selected.ToString());
    }

    private static string? Operation(Activity activity) =>
        Tag(activity, DiagnosticHeaders.Messaging.Operation);

    private static string? Tag(Activity activity, string name) =>
        activity.TagObjects.FirstOrDefault(tag => tag.Key == name).Value?.ToString();

    public sealed record ActivityMessage(string Value);

    private sealed class ActivityObservation
    {
        public string? ActivityIdHeader { get; set; }

        public string? Baggage { get; set; }

        public string? BaggageHeader { get; set; }

        public string[] BaggageHeaderKeys { get; set; } = [];

        public int DeliveryCount { get; set; }

        public string? TraceStateHeader { get; set; }
    }

    private sealed class MessagePipelineActivityConsumer(ActivityObservation observation) : IConsumer<ActivityMessage>
    {
        public Task Consume(ConsumeContext<ActivityMessage> context)
        {
            observation.DeliveryCount++;
            observation.ActivityIdHeader = context.Headers.Get<string>(DiagnosticHeaders.ActivityId);
            observation.TraceStateHeader = context.Headers.Get<string>(DiagnosticHeaders.ActivityTraceState);
            observation.Baggage = Activity.Current?.GetBaggageItem(BaggageKey);
            if (context.TryGetHeader(
                    DiagnosticHeaders.ActivityCorrelationContext,
                    out IEnumerable<KeyValuePair<string, object>>? baggage))
            {
                KeyValuePair<string, object>[] baggageEntries = baggage.ToArray();
                observation.BaggageHeader = baggageEntries.FirstOrDefault(pair => pair.Key == BaggageKey).Value as string;
                observation.BaggageHeaderKeys = baggageEntries.Select(pair => pair.Key).ToArray();
            }

            return Task.CompletedTask;
        }
    }

    public enum CallbackFault
    {
        None,
        ActivityStarted,
        ActivityStopped,
    }

    private sealed record HostileFlowResult(ActivityObservation Observation, string CallerId);

    private sealed class HostileTelemetryException(string callback) : Exception($"Hostile telemetry callback: {callback}");

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            if (message.StartsWith("Activity listener faulted", StringComparison.Ordinal))
                throw new InvalidOperationException("Secondary logger fault");
        }
    }
}
