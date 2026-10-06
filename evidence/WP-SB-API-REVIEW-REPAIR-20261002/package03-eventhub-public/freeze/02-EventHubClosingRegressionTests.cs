using System.Reflection;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

// Regression for the existing signed native owner; SDK callbacks are exercised without processor startup.
public sealed class EventHubClosingRegressionTests
{
    private static TimeSpan Timeout => PublicContractRuntime.Timeout;

    public async Task Closing_AlwaysAwaitsInternalCallbackAndPreservesBothOutcomesAsync(string mode)
    {
        var calls = new List<string>();
        using var caller = new CancellationTokenSource();
        await caller.CancelAsync();
        var applicationFault = new InvalidOperationException("application-close-canary");
        var cleanupFault = new ApplicationException("internal-close-canary");
        Exception? callbackCancellation = null;
        Func<PartitionClosingEventArgs, Task>? application = mode == "no-app" ? null : args =>
        {
            calls.Add("application");
            Assert.Equal("0", args.PartitionId);
            Assert.Equal(ProcessingStoppedReason.Shutdown, args.Reason);
            if (mode == "app-sync") throw applicationFault;
            if (mode is "app-async" or "dual-fault") return Task.FromException(applicationFault);
            if (mode == "app-task-cancel") return Task.FromCanceled(caller.Token);
            if (mode is "app-cancel" or "cancel-and-cleanup-fault")
            {
                callbackCancellation = new OperationCanceledException(caller.Token);
                return Task.FromException(callbackCancellation);
            }
            return Task.CompletedTask;
        };
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new ControlledClient();
        var context = new EventHubProcessorContext(DispatchProxy.Create<IHostConfiguration, UnexpectedProxy>(), client,
            null, application, PublicContractRuntime.CancellationToken);
        var builder = new ClosingBuilder(async args =>
        {
            calls.Add("internal");
            Assert.Equal("0", args.PartitionId);
            cleanupEntered.TrySetResult();
            await cleanupRelease.Task;
            if (mode is "cleanup-async" or "dual-fault" or "cancel-and-cleanup-fault") throw cleanupFault;
        }, mode == "cleanup-sync" ? cleanupFault : null, calls, cleanupEntered);
        Assert.Same(client, context.GetClient(builder));
        Task closing = client.CloseAsync();
        try
        {
            await Task.WhenAny(cleanupEntered.Task, closing).WaitAsync(Timeout, PublicContractRuntime.CancellationToken);
            Assert.True(cleanupEntered.Task.IsCompletedSuccessfully, "SDK closing completed before the internal close callback was entered.");
            if (mode != "cleanup-sync") Assert.False(closing.IsCompleted);
            Assert.Equal(mode == "no-app" ? new[] { "internal" } : new[] { "application", "internal" }, calls);
            cleanupRelease.TrySetResult();
            Exception? observed = await Record.ExceptionAsync(() => closing.WaitAsync(Timeout, PublicContractRuntime.CancellationToken));
            switch (mode)
            {
                case "no-app":
                case "success":
                    Assert.Null(observed);
                    Assert.True(closing.IsCompletedSuccessfully);
                    break;
                case "app-sync":
                case "app-async":
                    Assert.Same(applicationFault, observed);
                    Assert.True(closing.IsFaulted);
                    break;
                case "app-cancel":
                    Assert.Equal(caller.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
                    Assert.Same(callbackCancellation, observed);
                    Assert.True(closing.IsCanceled);
                    break;
                case "app-task-cancel":
                    Assert.Equal(caller.Token, Assert.IsType<TaskCanceledException>(observed).CancellationToken);
                    Assert.True(closing.IsCanceled);
                    break;
                case "cleanup-sync":
                case "cleanup-async":
                    Assert.Same(cleanupFault, observed);
                    Assert.True(closing.IsFaulted);
                    break;
                default:
                    AggregateException aggregate = Assert.IsType<AggregateException>(observed);
                    Assert.Equal(2, aggregate.InnerExceptions.Count);
                    Assert.Same(mode == "dual-fault" ? applicationFault : callbackCancellation, aggregate.InnerExceptions[0]);
                    Assert.Same(cleanupFault, aggregate.InnerExceptions[1]);
                    Assert.True(closing.IsFaulted);
                    break;
            }
            Assert.Equal(1, builder.Calls);
        }
        finally
        {
            cleanupRelease.TrySetResult();
            try
            {
                // Join first; a TimeoutException is never swallowed because the task won a race later.
                await Task.WhenAny(closing).WaitAsync(Timeout, CancellationToken.None);
                // The body asserts every exact outcome. Observe this already joined task
                // without allowing a teardown rethrow to replace that assertion failure.
                _ = await Record.ExceptionAsync(() => closing);
            }
            finally
            {
                context.ReleaseClient();
                context.ReleaseClient();
            }
        }

    }

    private sealed class ClosingBuilder(Func<PartitionClosingEventArgs, Task> close, Exception? synchronousFault,
        List<string> calls, TaskCompletionSource entered) : ProcessorClientBuilderContext
    {
        public int Calls { get; private set; }
        public Task OnPartitionInitializingAsync(PartitionInitializingEventArgs args, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task OnPartitionClosingAsync(PartitionClosingEventArgs args, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (synchronousFault is not null)
            {
                calls.Add("internal");
                entered.TrySetResult();
                throw synchronousFault;
            }
            return close(args);
        }
    }
    private sealed class ControlledClient : EventProcessorClient
    {
        public Task CloseAsync() => OnPartitionProcessingStoppedAsync(new Partition(), ProcessingStoppedReason.Shutdown, CancellationToken.None);
    }
    private sealed class Partition : EventProcessorPartition { public Partition() => PartitionId = "0"; }
    public class UnexpectedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new InvalidOperationException($"Unexpected host invocation: {method?.Name}");
    }
}
