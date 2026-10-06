using System.Reflection;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSendBranchOwnershipTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "actual-send-schedule-cancel-branches-join-exact-operation")]
    public async Task SendAsync_SelectsAndJoinsTheExactOperationAndPreservesItsOutcomeAsync(int branch, int outcome)
    {
        using var fixture = new SendFixture(branch);
        Task? pending = null;
        try
        {
            pending = fixture.Transport.SendAsync(fixture.Sender, fixture.Context, fixture.Outer.Token);
            await fixture.FirstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            fixture.AssertFirstInput(branch);
            if (branch == 1)
                Assert.Null(fixture.Context.ScheduledMessageId);

            if (outcome == 0)
                fixture.First.TrySetResult();
            else if (outcome == 1)
                fixture.First.TrySetException(fixture.Failure);
            else
            {
                fixture.Caller.Cancel();
                fixture.First.TrySetCanceled(fixture.Caller.Token);
            }

            Exception? observed = await Record.ExceptionAsync(() => pending.WaitAsync(TestContext.Current.CancellationToken));
            if (outcome == 0)
            {
                Assert.Null(observed);
                Assert.True(pending.IsCompletedSuccessfully);
                if (branch == 1)
                {
                    Assert.True(fixture.Context.TryGetScheduledMessageId(out long sequence));
                    Assert.Equal(SendFixture.Sequence, sequence);
                }
            }
            else if (outcome == 1)
            {
                Assert.Same(fixture.Failure, observed);
                Assert.True(pending.IsFaulted);
            }
            else
            {
                OperationCanceledException error = Assert.IsAssignableFrom<OperationCanceledException>(observed);
                Assert.Equal(fixture.Caller.Token, error.CancellationToken);
                Assert.True(pending.IsCanceled);
            }
            if (branch == 1 && outcome != 0)
                Assert.Null(fixture.Context.ScheduledMessageId);
            Assert.Single(fixture.Calls);
        }
        finally
        {
            await fixture.DrainAsync(pending);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "past-or-rejected-schedule-falls-back-to-joined-real-send")]
    public async Task ScheduledTime_FallsBackToAJoinedRealSendOnlyForPastOrRejectedTimesAsync(bool serverRejects)
    {
        using var fixture = new SendFixture(1);
        fixture.Context.ScheduledEnqueueTimeUtc = serverRejects ? SendFixture.Due : SendFixture.Now.AddSeconds(-1);
        Task? pending = null;
        try
        {
            pending = fixture.Transport.SendAsync(fixture.Sender, fixture.Context, fixture.Outer.Token);
            await fixture.FirstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            if (serverRejects)
            {
                fixture.AssertFirstInput(1);
                fixture.First.TrySetException(fixture.RejectedTime);
                await fixture.SecondEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
                Assert.False(pending.IsCompleted);
                Assert.Equal(new[] { "ScheduleSendAsync", "SendAsync" }, fixture.Calls.Select(call => call.Method).ToArray());
                fixture.AssertMessage(fixture.Calls[1].Message!);
                Assert.Equal(fixture.Caller.Token, fixture.Calls[1].Token);
                Assert.Null(fixture.Context.ScheduledMessageId);
                fixture.Second.TrySetResult();
            }
            else
            {
                fixture.AssertFirstInput(0);
                fixture.First.TrySetResult();
            }

            await pending.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(pending.IsCompletedSuccessfully);
            Assert.Equal(serverRejects ? 2 : 1, fixture.Calls.Count);
            Assert.Null(fixture.Context.ScheduledMessageId);
        }
        finally
        {
            await fixture.DrainAsync(pending);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-CANCELLATION", "idempotent-cancel-errors-are-contained-after-real-task-completion")]
    public async Task TaggedCancel_ContainsOnlyItsTwoDocumentedIdempotentErrorsAfterTheActualOperationAsync(bool alreadyCancelling)
    {
        using var fixture = new SendFixture(2);
        Exception expected = alreadyCancelling
            ? new InvalidOperationException("This message is already being cancelled")
            : new ServiceBusException("Scheduled message is absent", ServiceBusFailureReason.MessageNotFound, "work-queue", null);
        fixture.OtherExpected = expected;
        Task? pending = null;
        try
        {
            pending = fixture.Transport.SendAsync(fixture.Sender, fixture.Context, fixture.Outer.Token);
            await fixture.FirstEntered.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            fixture.AssertFirstInput(2);
            fixture.First.TrySetException(expected);

            await pending.WaitAsync(TestContext.Current.CancellationToken);

            Assert.True(pending.IsCompletedSuccessfully);
            Assert.Single(fixture.Calls);
        }
        finally
        {
            await fixture.DrainAsync(pending);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-SEND-FAILURE", "outer-and-context-pre-cancel-preserve-source-token-before-provider-call")]
    public async Task PreCancelledSource_RejectsBeforeAnyProviderCallWithItsExactTokenAsync(bool contextToken)
    {
        using var fixture = new SendFixture(0);
        CancellationTokenSource source = contextToken ? fixture.Caller : fixture.Outer;
        source.Cancel();
        Task? pending = null;
        try
        {
            pending = fixture.Transport.SendAsync(fixture.Sender, fixture.Context, fixture.Outer.Token);
            OperationCanceledException error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => pending.WaitAsync(TestContext.Current.CancellationToken));

            Assert.Equal(source.Token, error.CancellationToken);
            Assert.True(pending.IsCanceled);
            Assert.Empty(fixture.Calls);
        }
        finally
        {
            await fixture.DrainAsync(pending);
        }
    }

    sealed class SendFixture : IDisposable
    {
        internal const long Sequence = 4242;
        internal static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        internal static readonly DateTimeOffset Due = Now.AddMinutes(17);
        internal readonly CancellationTokenSource Caller = new();
        internal readonly CancellationTokenSource Outer = new();
        internal readonly TaskCompletionSource First = NewSignal();
        internal readonly TaskCompletionSource Second = NewSignal();
        internal readonly TaskCompletionSource FirstEntered = NewSignal();
        internal readonly TaskCompletionSource SecondEntered = NewSignal();
        internal readonly Exception Failure = new IntentionalSendFailure();
        internal readonly ArgumentOutOfRangeException RejectedTime = new("scheduledTime", "Server rejected schedule time");
        internal Exception? OtherExpected;
        internal readonly List<RecordedCall> Calls = [];
        internal readonly List<Task> ActualTasks = [];
        internal readonly AzureServiceBusSendContext<TransportBody> Context;
        internal readonly ServiceBusSendTransportContext Transport;
        internal readonly SendEndpointContext Sender;
        readonly Guid _messageId = new("604b6701-9f0b-44f9-a581-5b7e153941aa");
        readonly Guid _correlationId = new("d5cb5b50-3279-4d57-a720-611f06bf8a80");

        internal SendFixture(int branch)
        {
            var tokenSource = new AzureServiceBusSendContext<TransportBody>(new TransportBody("token-source", Guid.Empty, Now), CancellationToken.None);
            tokenSource.SetScheduledMessageId(Sequence);
            Guid tokenId = branch == 2 ? tokenSource.ScheduledMessageId!.Value : Guid.Empty;
            Context = new AzureServiceBusSendContext<TransportBody>(new TransportBody("actual-payload", tokenId, Now), Caller.Token)
            {
                Serializer = new SystemTextJsonRawMessageSerializer(JsonSerializerOptions.Default),
                MessageId = _messageId,
                CorrelationId = _correlationId,
                SessionId = "session-17",
                ReplyTo = "replies-queue",
                Label = "actual-subject",
                TimeToLive = TimeSpan.FromMinutes(9),
                DestinationAddress = new Uri("sb://recorded.servicebus.windows.net/work-queue"),
                ScheduledEnqueueTimeUtc = branch == 1 ? Due : null
            };
            Context.Headers.Set("workflow-marker", "materialized-before-operation");
            Context.GetOrAddPayload<TimeProvider>(() => new FixedClock());
            Sender = Proxy<SendEndpointContext>(Record);
            ISerialization serialization = new SerializationConfiguration().CreateSerializerCollection();
            var endpoint = Proxy<ReceiveEndpointContext>((method, _) => method.Name == "get_Serialization"
                ? serialization : throw new InvalidOperationException("Unexpected receive endpoint call: " + method.Name));
            var host = Proxy<IServiceBusHostConfiguration>((method, _) => throw new InvalidOperationException("Unexpected host call: " + method.Name));
            var supervisor = Proxy<ISendEndpointContextSupervisor>((method, _) => throw new InvalidOperationException("Unexpected supervisor call: " + method.Name));
            Transport = new ServiceBusSendTransportContext(host, endpoint, supervisor,
                new QueueSendSettings(new CreateQueueOptions("work-queue")));
        }

        object Record(MethodInfo method, object?[] args)
        {
            bool first = Calls.Count == 0;
            Task gate = first ? First.Task : Second.Task;
            RecordedCall call;
            Task returned;
            if (method.Name == "SendAsync")
            {
                call = new RecordedCall(method.Name, (ServiceBusMessage)args[0]!, (CancellationToken)args[1]!, null, null);
                returned = gate;
            }
            else if (method.Name == "ScheduleSendAsync")
            {
                call = new RecordedCall(method.Name, (ServiceBusMessage)args[0]!, (CancellationToken)args[2]!, (DateTimeOffset)args[1]!, null);
                returned = CompleteScheduleAsync(gate);
            }
            else if (method.Name == "CancelScheduledSendAsync")
            {
                call = new RecordedCall(method.Name, null, (CancellationToken)args[1]!, null, (long)args[0]!);
                returned = gate;
            }
            else
                throw new InvalidOperationException("Unexpected sender call: " + method.Name);
            if (!method.ReturnType.IsInstanceOfType(returned))
                throw new InvalidOperationException("Invalid SPI return type for " + method.Name);
            Calls.Add(call);
            ActualTasks.Add(returned);
            (first ? FirstEntered : SecondEntered).TrySetResult();
            return returned;
        }

        internal void AssertFirstInput(int branch)
        {
            RecordedCall call = Assert.Single(Calls);
            Assert.Equal(branch switch { 0 => "SendAsync", 1 => "ScheduleSendAsync", _ => "CancelScheduledSendAsync" }, call.Method);
            Assert.Equal(Caller.Token, call.Token);
            Assert.NotEqual(Outer.Token, call.Token);
            if (branch == 2)
            {
                Assert.Null(call.Message);
                Assert.Equal(Sequence, call.Sequence);
            }
            else
            {
                Assert.NotNull(call.Message);
                AssertMessage(call.Message);
                if (branch == 1)
                    Assert.Equal(Due, call.Due);
                else
                    Assert.Null(call.Due);
            }
        }

        internal void AssertMessage(ServiceBusMessage message)
        {
            using var body = JsonDocument.Parse(message.Body.ToMemory());
            Assert.Equal("actual-payload", body.RootElement.GetProperty(nameof(TransportBody.Value)).GetString());
            Assert.Equal("application/json", message.ContentType);
            Assert.Equal(_messageId.ToString("N"), message.MessageId);
            Assert.Equal(_correlationId.ToString("N"), message.CorrelationId);
            Assert.Equal("session-17", message.SessionId);
            Assert.Equal("session-17", message.PartitionKey);
            Assert.Equal("session-17", message.ReplyToSessionId);
            Assert.Equal("replies-queue", message.ReplyTo);
            Assert.Equal("actual-subject", message.Subject);
            Assert.Equal(TimeSpan.FromMinutes(9), message.TimeToLive);
            Assert.Equal("materialized-before-operation", message.ApplicationProperties["workflow-marker"]);
        }

        internal async Task DrainAsync(Task? operation)
        {
            First.TrySetResult();
            Second.TrySetResult();
            try
            {
                await ObserveAsync(operation);
            }
            finally
            {
                try
                {
                    // Wrapper first: a late fallback operation cannot evade this snapshot.
                    var unexpected = new List<Exception>();
                    foreach (Task task in ActualTasks.ToArray())
                    {
                        try { await ObserveAsync(task); }
                        catch (Exception error) { unexpected.Add(error); }
                    }
                    if (unexpected.Count > 0)
                        throw new AggregateException("Unexpected provider task failure during complete cleanup", unexpected);
                }
                finally
                {
                    try { await ObserveAsync(First.Task); }
                    finally { await ObserveAsync(Second.Task); }
                }
            }
        }

        async Task ObserveAsync(Task? task)
        {
            if (task == null)
                return;
            try { await task.WaitAsync(CancellationToken.None); }
            catch (Exception error) when (ReferenceEquals(error, Failure) || ReferenceEquals(error, RejectedTime)
                || ReferenceEquals(error, OtherExpected)) { }
            catch (OperationCanceledException error) when ((Caller.IsCancellationRequested && error.CancellationToken == Caller.Token)
                || (Outer.IsCancellationRequested && error.CancellationToken == Outer.Token)) { }
        }

        public void Dispose() { Caller.Dispose(); Outer.Dispose(); }
        static async Task<long> CompleteScheduleAsync(Task gate) { await gate.ConfigureAwait(false); return Sequence; }
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    static T Proxy<T>(Func<MethodInfo, object?[], object?> handler) where T : class
    {
        T result = DispatchProxy.Create<T, RecordingProxy>();
        ((RecordingProxy)(object)result).Handler = handler;
        return result;
    }
    public class RecordingProxy : DispatchProxy
    {
        internal Func<MethodInfo, object?[], object?> Handler = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args ?? []);
    }
    sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => SendFixture.Now; }
    public sealed record TransportBody(string Value, Guid TokenId, DateTimeOffset Timestamp) : CancelScheduledMessage;
    sealed class IntentionalSendFailure : Exception;
    sealed record RecordedCall(string Method, ServiceBusMessage? Message, CancellationToken Token, DateTimeOffset? Due, long? Sequence);
}
