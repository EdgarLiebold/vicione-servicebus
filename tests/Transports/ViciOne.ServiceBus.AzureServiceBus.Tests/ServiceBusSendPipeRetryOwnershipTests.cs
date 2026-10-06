using System.Reflection;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSendPipeRetryOwnershipTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-RETRY", "pipe-overload-joins-real-sdk-attempts-and-discriminates-caller-versus-stopping")]
    public async Task SendPipe_RetriesTheActualHeldSdkCallOrPreservesTheCorrectTerminalCauseAsync(int outcome)
    {
        var fixture = new Fixture(outcome == 1);
        Task? pending = null;
        try
        {
            pending = fixture.Transport.SendAsync(fixture.Pipe, fixture.Caller.Token);
            await fixture.Sender.FirstEntered.Task.WaitAsync(Timeout, TestToken);
            fixture.AssertCalls(1);
            Assert.False(pending.IsCompleted);
            Assert.False(fixture.Sender.First.Task.IsCompleted);
            if (outcome == 2)
            {
                fixture.Caller.Cancel();
                // Cancellation bounds retries, but this already admitted SDK task still belongs to the operation.
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.Sender.First.Task.IsCompleted);
            }
            else if (outcome == 3)
            {
                fixture.Stopping.Cancel();
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.Sender.First.Task.IsCompleted);
            }
            if (outcome == 4) fixture.Sender.First.TrySetResult();
            else fixture.Sender.First.TrySetException(fixture.Failure);

            if (outcome == 0)
            {
                // Observe the actual second SDK call after the real host policy delay; no elapsed-time oracle.
                await Task.WhenAny(fixture.Sender.SecondEntered.Task, pending).WaitAsync(Timeout, TestToken);
                Assert.True(fixture.Sender.SecondEntered.Task.IsCompletedSuccessfully);
                fixture.AssertCalls(2);
                Assert.False(pending.IsCompleted);
                Assert.False(fixture.Sender.Second.Task.IsCompleted);
                fixture.Sender.Second.TrySetResult();
            }

            Exception? observed = await Record.ExceptionAsync(() => pending.WaitAsync(Timeout, TestToken));
            switch (outcome)
            {
                case 0:
                case 4:
                    Assert.Null(observed);
                    Assert.True(pending.IsCompletedSuccessfully);
                    break;
                case 1:
                    Assert.Same(fixture.Failure, observed);
                    Assert.True(pending.IsFaulted);
                    break;
                case 2:
                    Assert.Equal(fixture.Caller.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
                    Assert.True(pending.IsCanceled);
                    break;
                case 3:
                    Assert.Same(fixture.Failure, Assert.IsType<ConnectionException>(observed).InnerException);
                    Assert.True(pending.IsFaulted);
                    break;
            }
            fixture.AssertCalls(outcome == 0 ? 2 : 1);
            Assert.True(fixture.Sender.ActualTasks.All(task => task.IsCompleted));
            Assert.Equal(0, fixture.Client.DisposeCalls);
            Assert.Equal(0, fixture.Sender.DisposeCalls);
        }
        finally
        {
            await fixture.DrainAndDisposeAsync(pending);
        }
        Assert.Equal(1, fixture.Client.DisposeCalls);
        Assert.Equal(1, fixture.Sender.DisposeCalls);
    }

    private sealed class Fixture
    {
        private readonly ServiceBusConnectionContext _connection;
        private readonly List<(object Pipe, CancellationToken Token)> _calls = [];
        public Fixture(bool permanent)
        {
            Failure = new ServiceBusException(!permanent, "unique retry SDK failure", "pipe-retry-queue",
                permanent ? ServiceBusFailureReason.MessageSizeExceeded : ServiceBusFailureReason.ServiceTimeout, null);
            Client = new RecordingClient();
            Sender = new RecordingSender();
            _connection = new ServiceBusConnectionContext(Client, new RecordingAdministration(), CancellationToken.None);
            var context = new MessageSendEndpointContext(_connection, Sender);
            Pipe = new SendPipe(Message, Caller.Token);
            var supervisor = Proxy<ISendEndpointContextSupervisor>((method, args) =>
            {
                switch (method.Name)
                {
                    case "get_SendStopping": return Stopping.Token;
                    case "SendAsync":
                        _calls.Add((args[0]!, Assert.IsType<CancellationToken>(args[1])));
                        return Assert.IsAssignableFrom<IPipe<SendEndpointContext>>(args[0]).SendAsync(context);
                    default: throw new NotSupportedException(method.Name);
                }
            });
            var configuration = new ServiceBusBusConfiguration(new ServiceBusTopologyConfiguration(AzureBusFactory.CreateMessageTopology()));
            ISerialization serialization = new SerializationConfiguration().CreateSerializerCollection();
            var endpoint = Proxy<ReceiveEndpointContext>((method, _) => method.Name == "get_Serialization"
                ? serialization : throw new NotSupportedException(method.Name));
            Transport = new ServiceBusSendTransportContext(configuration.HostConfiguration, endpoint, supervisor,
                new QueueSendSettings(new CreateQueueOptions("pipe-retry-queue")));
        }
        public CancellationTokenSource Caller { get; } = new();
        public CancellationTokenSource Stopping { get; } = new();
        public ServiceBusException Failure { get; }
        public ServiceBusMessage Message { get; } = new(BinaryData.FromString("owned SDK retry payload")) { MessageId = "pipe-retry-message" };
        public RecordingClient Client { get; }
        public RecordingSender Sender { get; }
        public IPipe<SendEndpointContext> Pipe { get; }
        public ServiceBusSendTransportContext Transport { get; }
        public void AssertCalls(int count)
        {
            Assert.Equal(count, _calls.Count);
            Assert.Equal(count, Sender.Calls.Count);
            foreach ((object pipe, CancellationToken token) in _calls)
            {
                Assert.Same(Pipe, pipe);
                Assert.Equal(Caller.Token, token);
            }
            foreach ((ServiceBusMessage message, CancellationToken token) in Sender.Calls)
            {
                Assert.Same(Message, message);
                Assert.Equal("pipe-retry-message", message.MessageId);
                Assert.Equal(Caller.Token, token);
            }
        }
        public async Task DrainAndDisposeAsync(Task? pending)
        {
            Sender.First.TrySetResult();
            Sender.Second.TrySetResult();
            try
            {
                try
                {
                    await ObserveAsync(pending ?? Task.CompletedTask);
                }
                finally
                {
                    // Root operation is terminal before snapshot: it may have admitted a retry after releasing the first gate.
                    await ObserveAsync(Task.WhenAll(Sender.ActualTasks));
                }
            }
            finally
            {
                try { await Sender.DisposeAsync(); }
                finally
                {
                    try { await _connection.DisposeAsync(); }
                    finally { Caller.Dispose(); Stopping.Dispose(); }
                }
            }
        }
        private async Task ObserveAsync(Task task)
        {
            try { await task.WaitAsync(Timeout, CancellationToken.None); }
            catch (Exception) when (task.IsFaulted && task.Exception!.Flatten().InnerExceptions.All(cause =>
                ReferenceEquals(cause, Failure) || cause is ConnectionException { InnerException: not null } connection
                    && ReferenceEquals(connection.InnerException, Failure))) { }
            catch (OperationCanceledException) when (task.IsCanceled) { }
        }
    }

    private sealed class SendPipe(ServiceBusMessage message, CancellationToken caller) : IPipe<SendEndpointContext>
    {
        public Task SendAsync(SendEndpointContext context) => context.SendAsync(message, caller);
        public void Probe(ProbeContext context) { }
    }
    private sealed class RecordingSender : ServiceBusSender
    {
        public override string EntityPath => "pipe-retry-queue";
        public TaskCompletionSource First { get; } = NewSignal();
        public TaskCompletionSource Second { get; } = NewSignal();
        public TaskCompletionSource FirstEntered { get; } = NewSignal();
        public TaskCompletionSource SecondEntered { get; } = NewSignal();
        public List<(ServiceBusMessage Message, CancellationToken Token)> Calls { get; } = [];
        public List<Task> ActualTasks { get; } = [];
        public int DisposeCalls { get; private set; }
        public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            Calls.Add((message, cancellationToken));
            Task result;
            if (Calls.Count == 1) { result = First.Task; FirstEntered.TrySetResult(); }
            else if (Calls.Count == 2) { result = Second.Task; SecondEntered.TrySetResult(); }
            else throw new InvalidOperationException("Unexpected third SDK retry attempt");
            ActualTasks.Add(result);
            return result;
        }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
    private sealed class RecordingClient : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => "pipe-retry.servicebus.invalid";
        public int DisposeCalls { get; private set; }
        public override ValueTask DisposeAsync() { DisposeCalls++; return ValueTask.CompletedTask; }
    }
    private sealed class RecordingAdministration : ServiceBusAdministrationClient { }
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T proxy = DispatchProxy.Create<T, ContextProxy>();
        ((ContextProxy)(object)proxy).Handler = handler;
        return proxy;
    }
    public class ContextProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("Missing proxy method"), args ?? []);
    }
}
