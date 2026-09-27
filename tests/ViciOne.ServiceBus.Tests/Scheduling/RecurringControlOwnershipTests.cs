using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class RecurringControlOwnershipTests
{
    private static readonly Uri SchedulerAddress = new("loopback://localhost/control-owner");
    private static readonly DateTimeOffset CreatedAt = new(2045, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    public static IEnumerable<object[]> FailureCases()
    {
        foreach (Control operation in Enum.GetValues<Control>())
            foreach (bool failResolution in new[] { false, true })
                foreach (bool cancel in new[] { false, true })
                    yield return [operation, failResolution, cancel];
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    [RequirementCoverage("REQ-VSB-RECURRING-SCHEDULER", "control-resolution-and-send-own-failure-before-recovery")]
    public async Task EndpointResolution_OwnsPendingFailureAndCancellationBeforeCommandAsync(
        Control operation, bool failResolution, bool cancel)
    {
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        var clock = new FakeTimeProvider(CreatedAt);
        var resolution = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var delivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("scheduler provider rejected the operation");
        var lookups = new List<(Uri Address, CancellationToken Token)>();
        var commands = new List<(Type Contract, object Message, CancellationToken Token)>();
        ISendEndpoint endpoint = DispatchProxy.Create<ISendEndpoint, Boundary>();
        var endpointBoundary = (Boundary)(object)endpoint;
        endpointBoundary.Callback = (method, args) =>
        {
            Assert.Equal(nameof(ISendEndpoint.SendAsync), method.Name);
            commands.Add((Assert.Single(method.GetGenericArguments()), args[0]!, Assert.IsType<CancellationToken>(args[^1])));
            sent.TrySetResult();
            return delivery.Task;
        };
        ISendEndpointProvider provider = DispatchProxy.Create<ISendEndpointProvider, Boundary>();
        var providerBoundary = (Boundary)(object)provider;
        providerBoundary.Callback = (method, args) =>
        {
            Assert.Equal(nameof(ISendEndpointProvider.GetSendEndpointAsync), method.Name);
            lookups.Add((Assert.IsType<Uri>(args[0]), Assert.IsType<CancellationToken>(args[1])));
            return resolution.Task;
        };
        var scheduler = new EndpointRecurringMessageScheduler(provider, SchedulerAddress, timeProvider: clock);
        Task pending = InvokeAsync(scheduler, operation, "original", "group-a", caller.Token);
        try
        {
            Assert.Equal(new[] { (SchedulerAddress, caller.Token) }, lookups);
            Assert.False(pending.IsCompleted);
            Assert.Empty(commands);
            clock.Advance(TimeSpan.FromHours(2));

            if (!failResolution)
            {
                resolution.SetResult(endpoint);
                await sent.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
                Assert.False(pending.IsCompleted);
                AssertCommand(Assert.Single(commands), operation, "original", "group-a", CreatedAt, caller.Token);
            }

            if (cancel)
            {
                providerCancellation.Cancel();
                if (failResolution)
                    resolution.SetCanceled(providerCancellation.Token);
                else
                    delivery.SetCanceled(providerCancellation.Token);
                OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    pending.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(providerCancellation.Token, actual.CancellationToken);
                Assert.Equal(TaskStatus.Canceled, pending.Status);
                Assert.False(caller.IsCancellationRequested);
            }
            else
            {
                if (failResolution)
                    resolution.SetException(failure);
                else
                    delivery.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    pending.WaitAsync(Timeout, TestContext.Current.CancellationToken)));
            }
            Assert.Equal(failResolution ? 0 : 1, commands.Count);
            Assert.Single(lookups);
        }
        finally
        {
            resolution.TrySetResult(endpoint);
            delivery.TrySetResult();
            try
            {
                await pending.WaitAsync(Timeout, CancellationToken.None);
            }
            catch (InvalidOperationException actual) when (ReferenceEquals(actual, failure))
            {
            }
            catch (OperationCanceledException actual) when (actual.CancellationToken == providerCancellation.Token)
            {
            }
        }

        using var successor = new CancellationTokenSource();
        providerBoundary.Callback = (method, args) =>
        {
            Assert.Equal(nameof(ISendEndpointProvider.GetSendEndpointAsync), method.Name);
            lookups.Add((Assert.IsType<Uri>(args[0]), Assert.IsType<CancellationToken>(args[1])));
            return Task.FromResult(endpoint);
        };
        endpointBoundary.Callback = (method, args) =>
        {
            Assert.Equal(nameof(ISendEndpoint.SendAsync), method.Name);
            commands.Add((Assert.Single(method.GetGenericArguments()), args[0]!, Assert.IsType<CancellationToken>(args[^1])));
            return Task.CompletedTask;
        };
        await InvokeAsync(scheduler, operation, "successor", "group-b", successor.Token)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { (SchedulerAddress, caller.Token), (SchedulerAddress, successor.Token) }, lookups);
        Assert.Equal(failResolution ? 1 : 2, commands.Count);
        AssertCommand(commands[^1], operation, "successor", "group-b", CreatedAt.AddHours(2), successor.Token);
    }

    private static Task InvokeAsync(IRecurringMessageScheduler scheduler, Control operation, string id, string group, CancellationToken token) =>
        operation switch
        {
            Control.Cancel => scheduler.CancelScheduledRecurringSendAsync(id, group, token),
            Control.Pause => scheduler.PauseScheduledRecurringSendAsync(id, group, token),
            Control.Resume => scheduler.ResumeScheduledRecurringSendAsync(id, group, token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    private static void AssertCommand((Type Contract, object Message, CancellationToken Token) command,
        Control operation, string id, string group, DateTimeOffset timestamp, CancellationToken token)
    {
        Type expected = operation switch
        {
            Control.Cancel => typeof(CancelScheduledRecurringMessage),
            Control.Pause => typeof(PauseScheduledRecurringMessage),
            Control.Resume => typeof(ResumeScheduledRecurringMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Assert.Equal(expected, command.Contract);
        Assert.Equal(token, command.Token);
        (string Id, string Group, DateTimeOffset Timestamp) values = command.Message switch
        {
            CancelScheduledRecurringMessage message => (message.ScheduleId, message.ScheduleGroup, message.Timestamp),
            PauseScheduledRecurringMessage message => (message.ScheduleId, message.ScheduleGroup, message.Timestamp),
            ResumeScheduledRecurringMessage message => (message.ScheduleId, message.ScheduleGroup, message.Timestamp),
            _ => throw new InvalidOperationException("Unexpected recurring command payload.")
        };
        Assert.Equal((id, group, timestamp), values);
    }

    public enum Control
    {
        Cancel,
        Pause,
        Resume
    }

    private class Boundary : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Callback { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Callback(targetMethod!, args!);
    }
}
