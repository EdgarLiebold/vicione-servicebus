using System.Collections.Concurrent;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzExplicitRedeliveryIntegrationTests
{
    private const string CallbackOrdinalHeader = "ViciOne-Quartz-Callback-Ordinal";

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPLICIT-REDELIVERY", "two-explicit-redeliveries")]
    public async Task Redeliver_SchedulesEachRequestedDeliveryThroughQuartzAsync()
    {
        ExplicitRedeliveryResult result = await ExecuteRedeliveryAsync(includeCallback: false);

        Assert.Equal([0, 1, 2], result.RedeliveryCounts);
        Assert.Equal(2, result.ScheduledCommandCount);
        Assert.Equal(0, result.CallbackCount);
        Assert.Null(result.FinalCallbackOrdinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-EXPLICIT-REDELIVERY", "callback-mutates-redelivered-message")]
    public async Task Redeliver_CallbackRunsBeforeSchedulingAndMutatesTheRedeliveredMessageAsync()
    {
        ExplicitRedeliveryResult result = await ExecuteRedeliveryAsync(includeCallback: true);

        Assert.Equal([0, 1, 2], result.RedeliveryCounts);
        Assert.Equal(2, result.ScheduledCommandCount);
        Assert.Equal(2, result.CallbackCount);
        Assert.Equal(2, result.FinalCallbackOrdinal);
    }

    private static async Task<ExplicitRedeliveryResult> ExecuteRedeliveryAsync(bool includeCallback)
    {
        TimeSpan timeout = OperationTimeout();
        var probe = new ExplicitRedeliveryProbe(includeCallback);
        string queueName = $"quartz-explicit-redelivery-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configure: configurator => configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<ExplicitRedeliveryPayload>(probe.ConsumeAsync)));
        var scheduledCommands = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, expectedCount: 2);
        using ConnectHandle observer = fixture.Bus.ConnectConsumeObserver(scheduledCommands);
        ISendEndpoint input = await fixture.Bus.GetSendEndpointAsync(inputAddress)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.SendAsync(new ExplicitRedeliveryPayload(NewId.NextGuid()), TestContext.Current.CancellationToken);
        int? finalCallbackOrdinal = await probe.Delivered
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduledCommands.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        return new ExplicitRedeliveryResult(
            probe.RedeliveryCounts.ToArray(),
            scheduledCommands.ObservedCount,
            probe.CallbackCount,
            finalCallbackOrdinal);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ExplicitRedeliveryPayload(Guid CorrelationId);

    private sealed record ExplicitRedeliveryResult(
        int[] RedeliveryCounts,
        int ScheduledCommandCount,
        int CallbackCount,
        int? FinalCallbackOrdinal);

    private sealed class ExplicitRedeliveryProbe(bool includeCallback)
    {
        private readonly TaskCompletionSource<int?> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callbackCount;

        public Task<int?> Delivered => _delivered.Task;
        public ConcurrentQueue<int> RedeliveryCounts { get; } = new();
        public int CallbackCount => Volatile.Read(ref _callbackCount);

        public async Task ConsumeAsync(ConsumeContext<ExplicitRedeliveryPayload> context)
        {
            int redeliveryCount = context.Advanced().GetRedeliveryCount();
            RedeliveryCounts.Enqueue(redeliveryCount);

            if (redeliveryCount < 2)
            {
                if (includeCallback)
                {
                    await context.RedeliverAsync(TimeSpan.Zero, (_, sendContext) =>
                    {
                        int callbackOrdinal = Interlocked.Increment(ref _callbackCount);
                        sendContext.Headers.Set(CallbackOrdinalHeader, callbackOrdinal);
                    });
                }
                else
                {
                    await context.RedeliverAsync(TimeSpan.Zero);
                }

                return;
            }

            int? callbackOrdinal = context.Headers.TryGetHeader(CallbackOrdinalHeader, out object? value)
                ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
                : null;
            _delivered.TrySetResult(callbackOrdinal);
        }
    }
}
