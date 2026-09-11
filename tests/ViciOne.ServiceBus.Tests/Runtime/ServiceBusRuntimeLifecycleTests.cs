using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Runtime;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Runtime;

public sealed class ServiceBusRuntimeLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-RUNTIME-LIFECYCLE", "cancellation-wins-over-late-readiness")]
    public async Task CanceledStartup_CannotBecomeStartedWhenReadinessCompletesDuringCleanupAsync()
    {
        var driver = new ServiceBusRuntimeLifecycleTestDriver();
        using var source = new CancellationTokenSource();

        Task start = driver.Bus.StartAsync(source.Token);
        await driver.HostStarted.WaitAsync(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start);

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, driver.HostStopCount);
        Assert.Equal(0, driver.PostStartCount);
        Assert.Equal(1, driver.StartFaultedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-RUNTIME-LIFECYCLE", "canceled-startup-does-not-wait-for-never-completing-readiness")]
    public async Task CanceledStartup_CompletesAfterBoundedCleanupWhenReadinessNeverCompletesAsync()
    {
        var driver = new ServiceBusRuntimeLifecycleTestDriver(completeReadyOnStop: false);
        using var source = new CancellationTokenSource();

        Task start = driver.Bus.StartAsync(source.Token);
        await driver.HostStarted.WaitAsync(TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            start.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, driver.HostStopCount);
        Assert.Equal(0, driver.PostStartCount);
        Assert.Equal(1, driver.StartFaultedCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-RUNTIME-PIPE-REGISTRATION", "post-start-direct-connection-delivers")]
    public async Task ConsumePipe_ConnectsDirectlyAndDeliversAfterReadinessAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"runtime-pipe-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var received = new TaskCompletionSource<RuntimePipeMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var receivedWithOptions = new TaskCompletionSource<RuntimePipeMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new RuntimePipeMessage(NewId.NextGuid());

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            using ConnectHandle connection = harness.Bus.ConnectConsumePipe(
                Pipe.Execute<ConsumeContext<RuntimePipeMessage>>(context => received.TrySetResult(context.Message)));
            using ConnectHandle optionConnection = ((IConsumePipeConnector)harness.Bus).ConnectConsumePipe(
                Pipe.Execute<ConsumeContext<RuntimePipeMessage>>(context => receivedWithOptions.TrySetResult(context.Message)),
                ConnectPipeOptions.All);
            ISendEndpoint endpoint = await harness.Bus
                .GetSendEndpointAsync(harness.Bus.Address, cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            await endpoint.SendAsync(message, cancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, await received.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal(message, await receivedWithOptions.Task.WaitAsync(timeout, cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-RUNTIME-PUBLISH-FORWARDING", "untyped-overloads-forward-message-type-pipe-and-cancellation")]
    public async Task AdvancedUntypedPublishOverloads_ForwardEveryArgumentToTheRuntimeEndpointAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"runtime-publish-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var pipedMessageIds = new List<Guid>();
        IPipe<PublishContext> publishPipe = Pipe.Execute<PublishContext>(context =>
            pipedMessageIds.Add(context.MessageId ?? Guid.Empty));
        RuntimePipeMessage[] messages =
        [
            new RuntimePipeMessage(NewId.NextGuid()),
            new RuntimePipeMessage(NewId.NextGuid()),
            new RuntimePipeMessage(NewId.NextGuid()),
        ];

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);

            await harness.Bus.Advanced().PublishAsync((object)messages[0], cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await harness.Bus.Advanced().PublishAsync((object)messages[1], typeof(RuntimePipeMessage), publishPipe, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await harness.Bus.PublishAsync<RuntimePipeMessage>(new { messages[2].Id }, publishPipe, cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(messages.Select(message => message.Id), harness.Published
                .Snapshot<RuntimePipeMessage>()
                .Select(observation => observation.Context.Message.Id));
            Assert.Equal(2, pipedMessageIds.Count);
            Assert.DoesNotContain(Guid.Empty, pipedMessageIds);

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            CancellationToken canceledToken = canceled.Token;
            Func<Task>[] canceledCalls =
            [
                () => harness.Bus.Advanced().PublishAsync((object)messages[0], canceledToken),
                () => harness.Bus.Advanced().PublishAsync((object)messages[1], typeof(RuntimePipeMessage), publishPipe, canceledToken),
                () => harness.Bus.PublishAsync<RuntimePipeMessage>(new { messages[2].Id }, publishPipe, canceledToken),
            ];

            foreach (Func<Task> canceledCall in canceledCalls)
            {
                OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(canceledCall);
                Assert.Equal(canceledToken, exception.CancellationToken);
            }

            Assert.Equal(3, harness.Published.Snapshot<RuntimePipeMessage>().Count);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record RuntimePipeMessage(Guid Id)
    {
        public RuntimePipeMessage()
            : this(Guid.Empty)
        {
        }
    }
}
