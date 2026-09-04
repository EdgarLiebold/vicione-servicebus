using System.Diagnostics;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzChainedSchedulingIntegrationTests
{
    private static readonly Uri Destination = new("loopback://localhost/quartz-chained-destination");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-CHAINED-DELIVERY", "trace-and-interface-dispatch")]
    public async Task ScheduledHandler_CanScheduleAnotherMessageWithoutLosingItsTraceOrInterfaceContractAsync(bool useRawJson)
    {
        TimeSpan timeout = OperationTimeout();
        var firstTrace = new TaskCompletionSource<ActivityContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var chainedTrace = new TaskCompletionSource<ActivityContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var concreteDelivery = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var interfaceDelivery = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using ActivityListener listener = CreateActivityListener();

        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator =>
            {
                if (useRawJson)
                    configurator.UseRawJsonSerializer();

                configurator.ReceiveEndpoint("quartz-chained-destination", endpoint =>
                {
                    endpoint.Handler<InitialPayload>(async context =>
                    {
                        firstTrace.TrySetResult(RequiredCurrentActivity());
                        await context.Advanced().ScheduleSendAsync(
                            Destination,
                            TimeSpan.Zero,
                            new ChainedPayload("second"),
                            context.CancellationToken);
                    });
                    endpoint.Handler<ChainedPayload>(context =>
                    {
                        chainedTrace.TrySetResult(RequiredCurrentActivity());
                        concreteDelivery.TrySetResult(context.Message.Value);
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<IChainedPayload>(context =>
                    {
                        interfaceDelivery.TrySetResult(context.Message.Value);
                        return Task.CompletedTask;
                    });
                });
            });
        var scheduler = CreateMessageScheduler(fixture);

        await scheduler.ScheduleSendAsync(
            Destination,
            TimeProvider.System.GetUtcNow(),
            new InitialPayload("first"),
            TestContext.Current.CancellationToken);

        ActivityContext initialActivity = await firstTrace.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        ActivityContext chainedActivity = await chainedTrace.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        string concrete = await concreteDelivery.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        string abstraction = await interfaceDelivery.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal("second", concrete);
        Assert.Equal("second", abstraction);
        Assert.NotEqual(default, initialActivity.TraceId);
        Assert.Equal(initialActivity.TraceId, chainedActivity.TraceId);
        Assert.NotEqual(initialActivity.SpanId, chainedActivity.SpanId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPIRATION", "chained-delivery-preserves-time-to-live-within-scheduling-precision")]
    public async Task ChainedSchedule_PreservesTheConfiguredTimeToLiveWithinSchedulingPrecisionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        TimeSpan timeToLive = TimeSpan.FromHours(1);
        var delivered = new TaskCompletionSource<ExpirationObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint("quartz-chained-destination", endpoint =>
            {
                endpoint.Handler<InitialPayload>(context => context.Advanced().ScheduleSendAsync(
                    Destination,
                    TimeSpan.Zero,
                    new ChainedPayload("expiring"),
                    Pipe.Execute<SendContext<ChainedPayload>>(sendContext => sendContext.TimeToLive = timeToLive),
                    context.CancellationToken));
                endpoint.Handler<ChainedPayload>(context =>
                {
                    delivered.TrySetResult(new ExpirationObservation(context.SentTime, context.ExpirationTime));
                    return Task.CompletedTask;
                });
            }));
        var scheduler = CreateMessageScheduler(fixture);

        await scheduler.ScheduleSendAsync(
            Destination,
            TimeProvider.System.GetUtcNow(),
            new InitialPayload("first"),
            TestContext.Current.CancellationToken);

        ExpirationObservation received = await delivered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        DateTimeOffset sentTime = Assert.IsType<DateTimeOffset>(received.SentTime);
        DateTimeOffset expirationTime = Assert.IsType<DateTimeOffset>(received.ExpirationTime);
        TimeSpan preservedTimeToLive = expirationTime - sentTime;

        Assert.InRange(preservedTimeToLive, timeToLive - TimeSpan.FromSeconds(5), timeToLive + TimeSpan.FromSeconds(5));
    }

    private static ActivityListener CreateActivityListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static ActivityContext RequiredCurrentActivity()
    {
        Activity? activity = Activity.Current;
        Assert.NotNull(activity);
        return activity.Context;
    }

    private static MessageScheduler CreateMessageScheduler(QuartzTestBus fixture) =>
        new(new EndpointScheduleMessageProvider(() => Task.FromResult(fixture.SchedulerEndpoint)), fixture.Bus.Topology);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record InitialPayload(string Value);

    public interface IChainedPayload
    {
        string Value { get; }
    }

    public sealed record ChainedPayload(string Value) : IChainedPayload;

    private sealed record ExpirationObservation(DateTimeOffset? SentTime, DateTimeOffset? ExpirationTime);
}
