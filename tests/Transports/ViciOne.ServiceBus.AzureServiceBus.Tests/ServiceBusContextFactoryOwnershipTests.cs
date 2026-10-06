using System.Reflection;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusContextFactoryOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "owned-context-readiness-joins-namespace-topology-and-processor-close")]
    public async Task CreateContext_PublishesTheActualCreationOutcomeAndOwnsTheClientUntilStoppedAsync(bool sender, int outcome)
    {
        var fixture = new Fixture();
        if (sender)
            await VerifyCreationAsync(fixture.SendFactory, fixture, outcome, true);
        else
            await VerifyCreationAsync(fixture.ClientFactory, fixture, outcome, false);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "shared-acquisition-outcomes-and-borrowed-use-stop-preserve-source-ownership")]
    public async Task CreateActiveContext_ObservesAcquisitionAndStopsTheBorrowWithoutDisposingTheOwnerAsync(bool sender, int outcome)
    {
        var fixture = new Fixture();
        try
        {
            if (sender)
                await VerifyLeaseAsync(fixture.SendFactory, fixture, new MessageSendEndpointContext(fixture.Connection, fixture.Client.Sender), outcome, true);
            else
            {
                var underlying = new QueueClientContext(fixture.Connection, fixture.InputAddress, fixture.Settings, new Supervisor());
                underlying.ConfigureMessageProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
                try
                {
                    await VerifyLeaseAsync(fixture.ClientFactory, fixture, underlying, outcome, false);
                }
                finally
                {
                    // A borrowed lease never owns this context. The fixture is its owner.
                    fixture.Client.Processor.CloseRelease.TrySetResult();
                    await underlying.DisposeAsync();
                }
            }
        }
        finally
        {
            await fixture.DrainAndDisposeAsync();
        }
        Assert.Equal(1, fixture.Client.DisposeCalls);
    }

    private static async Task VerifyCreationAsync<T>(IPipeContextFactory<T> factory, Fixture fixture, int outcome, bool sender)
        where T : class, PipeContext
    {
        var owner = new Supervisor();
        IPipeContextAgent<T>? child = null;
        Task? stop = null;
        try
        {
            child = factory.CreateContext(owner);
            await fixture.NamespaceEntered.Task.WaitAsync(Timeout, TestToken);
            Assert.Equal(owner.Stopped, fixture.NamespaceToken);
            Assert.False(child.Context.IsCompleted);
            Assert.False(child.Ready.IsCompleted);
            Assert.Equal(0, fixture.Client.CreateSenderCalls);
            Assert.Equal(0, fixture.Client.CreateProcessorCalls);
            Assert.False(fixture.NamespaceTask!.IsCompleted);

            if (outcome == 1)
                fixture.Acquisition.TrySetException(fixture.Failure);
            else if (outcome == 2)
            {
                fixture.ProviderCancellation.Cancel();
                fixture.Acquisition.TrySetCanceled(fixture.ProviderCancellation.Token);
            }
            else
            {
                fixture.Acquisition.TrySetResult(fixture.Connection);
                if (sender)
                {
                    await Task.WhenAny(fixture.Administration.Entered.Task, child.Context).WaitAsync(Timeout, TestToken);
                    Assert.True(fixture.Administration.Entered.Task.IsCompletedSuccessfully);
                    Assert.Equal(1, fixture.Client.CreateSenderCalls);
                    Assert.Equal("factory-queue", fixture.Client.RequestedSenderPath);
                    Assert.Equal("factory-queue", fixture.Administration.RequestedQueue);
                    Assert.Equal(owner.Stopped, fixture.Administration.Token);
                    Assert.False(child.Context.IsCompleted);
                    Assert.False(child.Ready.IsCompleted);
                    Assert.False(fixture.Administration.Result.Task.IsCompleted);
                    if (outcome == 3)
                        fixture.Administration.Result.TrySetException(fixture.Failure);
                    else
                        fixture.Administration.ReleaseSuccess();
                }
            }

            if (outcome != 0)
            {
                await AssertOutcomeAsync(child.Context, outcome == 2, fixture.ProviderCancellation.Token, fixture.Failure);
                await AssertOutcomeAsync(child.Ready, outcome == 2, fixture.ProviderCancellation.Token, fixture.Failure);
                await child.Completed.WaitAsync(Timeout, TestToken);
                Assert.Equal(sender && outcome == 3 ? 1 : 0, fixture.Client.CreateSenderCalls);
                Assert.Equal(0, fixture.Client.CreateProcessorCalls);
            }
            else
            {
                T created = await child.Context.WaitAsync(Timeout, TestToken);
                await child.Ready.WaitAsync(Timeout, TestToken);
                if (sender)
                {
                    var context = Assert.IsType<MessageSendEndpointContext>(created);
                    Assert.Same(fixture.Connection, context.ConnectionContext);
                    Assert.Equal("factory-queue", context.EntityPath);
                    Assert.Equal(0, fixture.Client.CreateProcessorCalls);
                }
                else
                {
                    var context = Assert.IsType<QueueClientContext>(created);
                    Assert.Same(fixture.Connection, context.ConnectionContext);
                    Assert.Equal(fixture.InputAddress, context.InputAddress);
                    // Factory readiness supplies an unconfigured client; processor startup is a separate receiver operation.
                    Assert.Equal(0, fixture.Client.CreateProcessorCalls);
                    context.ConfigureMessageProcessor((_, _, _) => Task.CompletedTask, _ => Task.CompletedTask);
                    Assert.Equal(1, fixture.Client.CreateProcessorCalls);
                    Assert.Equal("factory-queue", context.EntityPath);
                }
                Assert.False(child.Completed.IsCompleted);
                Assert.False(fixture.NamespaceTask!.IsCompleted);
                stop = owner.StopAsync(CancellationToken.None);
                if (!sender)
                {
                    await fixture.Client.Processor.CloseEntered.Task.WaitAsync(Timeout, TestToken);
                    Assert.False(stop.IsCompleted);
                    Assert.False(child.Completed.IsCompleted);
                    Assert.False(fixture.NamespaceTask!.IsCompleted);
                    Assert.Equal(1, fixture.Client.Processor.CloseCalls);
                    fixture.Client.Processor.CloseRelease.TrySetResult();
                }
                await stop.WaitAsync(Timeout, TestToken);
                await child.Completed.WaitAsync(Timeout, TestToken);
                await fixture.NamespaceTask!.WaitAsync(Timeout, TestToken);
            }
            Assert.Equal(0, fixture.Client.DisposeCalls);
            Assert.Equal(0, fixture.Client.Sender.DisposeCalls);
        }
        finally
        {
            fixture.ReleaseAll();
            try
            {
                await (stop ?? owner.StopAsync(CancellationToken.None)).WaitAsync(Timeout, CancellationToken.None);
                if (child is not null)
                {
                    await fixture.ObserveAsync(child.Context);
                    await fixture.ObserveAsync(child.Ready);
                    await fixture.ObserveAsync(child.Completed);
                }
                await fixture.ObserveAsync(owner.Ready);
            }
            finally
            {
                await fixture.DrainAndDisposeAsync();
            }
        }
        Assert.Equal(1, fixture.Client.DisposeCalls);
    }

    private static async Task VerifyLeaseAsync<T>(IPipeContextFactory<T> factory, Fixture fixture, T underlying, int outcome, bool sender)
        where T : class, PipeContext
    {
        using var caller = new CancellationTokenSource();
        var owner = new Supervisor();
        var handle = new RecordingHandle<T>();
        IActivePipeContextAgent<T>? child = null;
        IDisposable? use = null;
        Task? stop = null;
        try
        {
            if (outcome == 3)
            {
                handle.Completion.TrySetResult(underlying);
                caller.Cancel();
            }
            child = factory.CreateActiveContext(owner, handle, caller.Token);
            if (outcome != 3)
            {
                Assert.False(child.Context.IsCompleted);
                Assert.False(child.Ready.IsCompleted);
                if (outcome == 1) handle.Completion.TrySetException(fixture.Failure);
                else if (outcome == 2) caller.Cancel();
                else handle.Completion.TrySetResult(underlying);
            }

            if (outcome is 1 or 2)
            {
                await AssertOutcomeAsync(child.Context, outcome == 2, caller.Token, fixture.Failure);
                await AssertOutcomeAsync(child.Ready, outcome == 2, caller.Token, fixture.Failure);
                await child.Completed.WaitAsync(Timeout, TestToken);
                if (outcome == 2) Assert.False(handle.Context.IsCompleted);
            }
            else
            {
                T shared = await child.Context.WaitAsync(Timeout, TestToken);
                await child.Ready.WaitAsync(Timeout, TestToken);
                Assert.NotSame(underlying, shared);
                Assert.Equal(caller.Token, shared.CancellationToken);
                if (sender)
                {
                    var context = Assert.IsType<SharedSendEndpointContext>(shared);
                    Assert.Same(fixture.Connection, context.ConnectionContext);
                    Assert.Equal("factory-queue", context.EntityPath);
                }
                else
                {
                    var context = Assert.IsType<SharedClientContext>(shared);
                    Assert.Same(fixture.Connection, context.ConnectionContext);
                    Assert.Equal(fixture.InputAddress, context.InputAddress);
                    Assert.Equal("factory-queue", context.EntityPath);
                }
                Assert.Equal(outcome == 3, shared.CancellationToken.IsCancellationRequested);
                use = Assert.IsType<ActivePipeContextAgent<T>>(child).BeginUse();
                stop = owner.StopAsync(CancellationToken.None);
                Assert.True(child.Stopping.IsCancellationRequested);
                Assert.False(stop.IsCompleted);
                Assert.False(child.Completed.IsCompleted);
                Assert.Equal(0, fixture.Client.Processor.CloseCalls);
                use.Dispose();
                use = null;
                await stop.WaitAsync(Timeout, TestToken);
                await child.Completed.WaitAsync(Timeout, TestToken);
            }
            await (stop ?? owner.StopAsync(CancellationToken.None)).WaitAsync(Timeout, TestToken);
            Assert.Equal(0, handle.DisposeCalls);
            Assert.Equal(0, fixture.Client.Processor.CloseCalls);
            Assert.Equal(0, fixture.Client.Sender.DisposeCalls);
            Assert.Equal(0, fixture.Client.DisposeCalls);
        }
        finally
        {
            use?.Dispose();
            handle.Completion.TrySetResult(underlying);
            fixture.ReleaseAll();
            await owner.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            await fixture.ObserveAsync(handle.Context);
            if (child is not null)
            {
                await fixture.ObserveAsync(child.Context);
                await fixture.ObserveAsync(child.Ready);
                await fixture.ObserveAsync(child.Completed);
            }
            await fixture.ObserveAsync(owner.Ready);
        }
    }

    private static async Task AssertOutcomeAsync(Task task, bool canceled, CancellationToken token, Exception failure)
    {
        if (canceled)
        {
            OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(Timeout, TestToken));
            Assert.Equal(token, observed.CancellationToken);
            Assert.True(task.IsCanceled);
        }
        else
        {
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task.WaitAsync(Timeout, TestToken)));
            Assert.True(task.IsFaulted);
        }
    }

    private sealed class RecordingHandle<T> : IPipeContextHandle<T> where T : class, PipeContext
    {
        public TaskCompletionSource<T> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<T> Context => Completion.Task;
        public int DisposeCalls { get; private set; }
        public bool IsDisposed => DisposeCalls != 0;
        public ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Client = new RecordingClient();
            Administration = new RecordingAdministration();
            Connection = new ServiceBusConnectionContext(Client, Administration, CancellationToken.None);
            Settings = CreateSettings();
            InputAddress = new Uri(Connection.Endpoint, "factory-queue");
            var proxy = DispatchProxy.Create<IConnectionContextSupervisor, ConnectionSupervisorProxy>();
            ((ConnectionSupervisorProxy)(object)proxy).Handler = (method, args) =>
            {
                if (method.Name != "SendAsync") throw new NotSupportedException(method.Name);
                NamespaceToken = Assert.IsType<CancellationToken>(args[1]);
                NamespaceTask = PumpAsync(Assert.IsAssignableFrom<IPipe<ConnectionContext>>(args[0]));
                NamespaceEntered.TrySetResult();
                return NamespaceTask;
            };
            ClientFactory = new QueueClientContextFactory(proxy, Settings);
            var sendSettings = new QueueSendSettings(new CreateQueueOptions("factory-queue"));
            SendFactory = new SendEndpointContextFactory(proxy,
                new ConfigureServiceBusTopologyFilter<SendSettings>(sendSettings, sendSettings.GetBrokerTopology()), sendSettings);
        }
        public RecordingClient Client { get; }
        public RecordingAdministration Administration { get; }
        public ServiceBusConnectionContext Connection { get; }
        public ReceiveEndpointSettings Settings { get; }
        public Uri InputAddress { get; }
        public QueueClientContextFactory ClientFactory { get; }
        public SendEndpointContextFactory SendFactory { get; }
        public InvalidOperationException Failure { get; } = new("unique factory provider failure");
        public CancellationTokenSource ProviderCancellation { get; } = new();
        public TaskCompletionSource<ConnectionContext> Acquisition { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource NamespaceEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken NamespaceToken { get; private set; }
        public Task? NamespaceTask { get; private set; }
        private async Task PumpAsync(IPipe<ConnectionContext> pipe) =>
            await pipe.SendAsync(await Acquisition.Task.ConfigureAwait(false)).ConfigureAwait(false);
        public void ReleaseAll()
        {
            Acquisition.TrySetResult(Connection);
            Administration.ReleaseSuccess();
            Client.Processor.CloseRelease.TrySetResult();
        }
        public async Task ObserveAsync(Task task)
        {
            try { await task.WaitAsync(Timeout, CancellationToken.None); }
            catch (Exception) when (task.IsFaulted && task.Exception!.Flatten().InnerExceptions.All(cause => ReferenceEquals(cause, Failure))) { }
            catch (OperationCanceledException) when (task.IsCanceled) { }
        }
        public async Task DrainAndDisposeAsync()
        {
            ReleaseAll();
            try
            {
                if (NamespaceTask is not null) await ObserveAsync(NamespaceTask);
                await ObserveAsync(Acquisition.Task);
                if (Administration.Calls != 0) await ObserveAsync(Administration.Result.Task);
            }
            finally
            {
                try { await Client.Sender.DisposeAsync(); }
                finally
                {
                    try { await Connection.DisposeAsync(); }
                    finally { ProviderCancellation.Dispose(); }
                }
            }
        }
    }

    private static ReceiveEndpointSettings CreateSettings()
    {
        var configuration = new ServiceBusBusConfiguration(new ServiceBusTopologyConfiguration(AzureBusFactory.CreateMessageTopology()));
        var endpoint = (ServiceBusEndpointConfiguration)configuration.CreateEndpointConfiguration(false);
        return new ReceiveEndpointSettings(endpoint, "factory-queue", new ServiceBusQueueConfigurator("factory-queue"));
    }
    private sealed class RecordingClient : ServiceBusClient
    {
        public RecordingSender Sender { get; } = new();
        public RecordingProcessor Processor { get; } = new();
        public override string FullyQualifiedNamespace => "factory-ownership.servicebus.invalid";
        public int CreateSenderCalls { get; private set; }
        public int CreateProcessorCalls { get; private set; }
        public string? RequestedSenderPath { get; private set; }
        public int DisposeCalls { get; private set; }
        public override ServiceBusSender CreateSender(string queueOrTopicName)
        {
            CreateSenderCalls++; RequestedSenderPath = queueOrTopicName; return Sender;
        }
        public override ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
        {
            Assert.Equal("factory-queue", queueName); CreateProcessorCalls++; return Processor;
        }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
    private sealed class RecordingSender : ServiceBusSender
    {
        public override string EntityPath => "factory-queue";
        public int DisposeCalls { get; private set; }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
    private sealed class RecordingProcessor : ServiceBusProcessor
    {
        public override string EntityPath => "factory-queue";
        public override bool IsClosed => Closed;
        public bool Closed { get; private set; }
        public int CloseCalls { get; private set; }
        public TaskCompletionSource CloseEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CloseRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task CloseAsync(CancellationToken cancellationToken = default)
        {
            CloseCalls++; CloseEntered.TrySetResult();
            await CloseRelease.Task.ConfigureAwait(false); Closed = true;
        }
    }
    private sealed class RecordingAdministration : ServiceBusAdministrationClient
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<global::Azure.Response<QueueProperties>> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls { get; private set; }
        public string? RequestedQueue { get; private set; }
        public CancellationToken Token { get; private set; }
        public override Task<global::Azure.Response<QueueProperties>> GetQueueAsync(string name, CancellationToken cancellationToken = default)
        {
            Calls++; RequestedQueue = name; Token = cancellationToken; Entered.TrySetResult(); return Result.Task;
        }
        public void ReleaseSuccess() => Result.TrySetResult(global::Azure.Response.FromValue(
            ServiceBusModelFactory.QueueProperties("factory-queue", lockDuration: TimeSpan.FromMinutes(1), maxSizeInMegabytes: 1024,
                requiresDuplicateDetection: false, requiresSession: false, defaultMessageTimeToLive: TimeSpan.MaxValue,
                autoDeleteOnIdle: TimeSpan.MaxValue, deadLetteringOnMessageExpiration: false,
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10), maxDeliveryCount: 10,
                enableBatchedOperations: true, status: EntityStatus.Active, forwardTo: string.Empty,
                forwardDeadLetteredMessagesTo: string.Empty, userMetadata: string.Empty, enablePartitioning: false), new StubResponse()));
    }
    public class ConnectionSupervisorProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Missing proxy method"), args ?? []);
    }
    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "factory";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
        protected override bool TryGetHeader(string name, out string value) { value = null!; return false; }
        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) { values = null!; return false; }
    }
}
