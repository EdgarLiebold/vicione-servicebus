using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureSubscriberFanoutContractTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-FUTURE-SUBSCRIPTIONS", "all-started-subscriber-sends-join-after-independent-failure")]
    public async Task SubscriberFanout_JoinsBothSendsAndPreservesResultOrFailureIdentityAsync(bool typedContext, bool secondFails)
    {
        var machine = new ContextMachine();
        var state = new FutureState();
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        var expected = new Reply("terminal-value");
        var failure = new IOException("second-subscriber-send-failure");
        var pipe = new CoordinatedSendPipe(firstId, secondId, secondFails ? failure : null);
        var outgoing = new OutgoingMessageRecorder();
        int factoryCalls = 0;
        FutureSubscription[] subscriptions =
        [
            new(new Uri("loopback://localhost/fanout-first"), firstId),
            new(new Uri("loopback://localhost/fanout-second"), secondId),
        ];
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await FutureBehaviorContextFactory.UseAsync(machine, machine.SignalReceived, state, new Signal(), async context =>
        {
            Task<Reply>? dispatch = null;
            try
            {
                if (typedContext)
                {
                    var factory = new ContextMessageFactory<IBehaviorContext<FutureState, Signal>, Reply>(_ =>
                    {
                        Interlocked.Increment(ref factoryCalls);
                        return Task.FromResult(new InitializedMessage<Reply>(expected, pipe));
                    });
                    dispatch = context.SendMessageToSubscriptionsAsync(factory, subscriptions, caller.Token);
                }
                else
                {
                    IBehaviorContext<FutureState> stateContext = context;
                    var factory = new ContextMessageFactory<IBehaviorContext<FutureState>, Reply>(_ =>
                    {
                        Interlocked.Increment(ref factoryCalls);
                        return Task.FromResult(new InitializedMessage<Reply>(expected, pipe));
                    });
                    dispatch = stateContext.SendMessageToSubscriptionsAsync(factory, subscriptions, caller.Token);
                }

                await Task.WhenAll(pipe.FirstEntered.Task, pipe.SecondEntered.Task)
                    .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.False(dispatch.IsCompleted);
                Assert.False(pipe.FirstCompleted);
                Assert.Equal(caller.Token, pipe.Tokens[firstId]);
                Assert.Equal(caller.Token, pipe.Tokens[secondId]);
                pipe.ReleaseFirst.TrySetResult();
                Reply? actual = null;
                Exception? observed = await Record.ExceptionAsync(async () =>
                {
                    actual = await dispatch;
                });
                Assert.True(pipe.FirstCompleted);
                Assert.Equal(1, factoryCalls);
                if (secondFails)
                {
                    Assert.Same(failure, observed);
                    Assert.Null(actual);
                    SendObservationIs(outgoing, firstId, expected);
                    Assert.Single(outgoing.SendObservations);
                }
                else
                {
                    Assert.Null(observed);
                    Assert.Same(expected, actual);
                    Assert.Equal(2, outgoing.SendObservations.Count);
                    SendObservationIs(outgoing, firstId, expected);
                    SendObservationIs(outgoing, secondId, expected);
                }
            }
            finally
            {
                pipe.ReleaseFirst.TrySetResult();
                if (dispatch is not null)
                    await Record.ExceptionAsync(() => dispatch);
            }
        }, outgoing, cancellationToken: caller.Token);
    }

    private static void SendObservationIs(OutgoingMessageRecorder outgoing, Guid requestId, Reply expected)
    {
        OutgoingMessageRecorder.SendObservation observation = Assert.Single(outgoing.SendObservations,
            current => current.RequestId == requestId);
        Assert.Same(expected, observation.Message);
    }

    private sealed class CoordinatedSendPipe(Guid firstId, Guid secondId, Exception? secondFailure) : IPipe<SendContext<Reply>>
    {
        public TaskCompletionSource FirstEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentDictionary<Guid, CancellationToken> Tokens { get; } = new();
        public bool FirstCompleted { get; private set; }

        public async Task SendAsync(SendContext<Reply> context)
        {
            Guid requestId = context.RequestId ?? throw new InvalidOperationException("Subscriber request id was not forwarded.");
            Tokens[requestId] = context.CancellationToken;
            if (requestId == firstId)
            {
                FirstEntered.TrySetResult();
                await ReleaseFirst.Task;
                FirstCompleted = true;
            }
            else if (requestId == secondId)
            {
                SecondEntered.TrySetResult();
                if (secondFailure is not null)
                    throw secondFailure;
            }
            else
                throw new InvalidOperationException("Unexpected subscriber request id.");
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("subscriber-send-barrier");
    }

    private sealed class ContextMachine : ViciOneServiceBusStateMachine<FutureState>
    {
        public ContextMachine() => InstanceState(instance => instance.CurrentState);
        public IEvent<Signal> SignalReceived { get; private set; } = null!;
    }
    public sealed record Signal;
    public sealed record Reply(string Value);
}
