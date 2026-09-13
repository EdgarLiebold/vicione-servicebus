using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class MissingInstanceRedeliveryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "retry-observer-and-terminal-pipe")]
    public async Task ExhaustedPolicy_NotifiesTheConnectedObserverBeforeInvokingTheTerminalPipeAsync()
    {
        var terminalInvocationCount = 0;
        IPipe<ConsumeContext<MissingMessage>> terminalPipe = Pipe.ExecuteAwaited<ConsumeContext<MissingMessage>>(_ =>
        {
            terminalInvocationCount++;
            return Task.CompletedTask;
        });
        var missingInstance = new StubMissingInstanceConfigurator(terminalPipe);
        var observer = new RecordingRetryObserver();
        ConnectHandle? observerHandle = null;
        IPipe<ConsumeContext<MissingMessage>> pipe = missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
        {
            configurator.SetRetryPolicy(_ => Retry.None);
            observerHandle = configurator.ConnectRetryObserver(observer);
        });
        using ConnectHandle handle = Assert.IsAssignableFrom<ConnectHandle>(observerHandle);
        Guid correlationId = NewId.NextGuid();
        ConsumeContext<MissingMessage> context = InMemoryOutboxTestContextFactory.Create(
            new MissingMessage(correlationId),
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

        await pipe.SendAsync(context);

        Assert.Equal(1, terminalInvocationCount);
        Assert.Equal(["post-create", "retry-fault"], observer.Notifications);
        SagaException exception = Assert.IsType<SagaException>(observer.Exception);
        Assert.Equal(typeof(MissingState), exception.SagaType);
        Assert.Equal(typeof(MissingMessage), exception.MessageType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "retry-observer-lifetime-and-scheduled-redelivery")]
    public async Task RetryablePolicy_NotifiesUntilDisconnectedAndSchedulesEachRedeliveryAsync()
    {
        var terminalInvocationCount = 0;
        IPipe<ConsumeContext<MissingMessage>> terminalPipe = Pipe.ExecuteAwaited<ConsumeContext<MissingMessage>>(_ =>
        {
            terminalInvocationCount++;
            return Task.CompletedTask;
        });
        var missingInstance = new StubMissingInstanceConfigurator(terminalPipe);
        var observer = new RecordingRetryObserver();
        ConnectHandle? observerHandle = null;
        IPipe<ConsumeContext<MissingMessage>> pipe = missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
        {
            configurator.SetRetryPolicy(_ => Retry.Interval(1, TimeSpan.FromSeconds(3)));
            configurator.ConfigureMessageScheduler = false;
            observerHandle = configurator.ConnectRetryObserver(observer);
        });
        using ConnectHandle handle = Assert.IsAssignableFrom<ConnectHandle>(observerHandle);
        var outgoing = new OutgoingMessageRecorder();
        ConsumeContext<MissingMessage> firstContext = CreateContext(NewId.NextGuid(), outgoing);

        await pipe.SendAsync(firstContext);

        Assert.Equal(["post-create", "post-fault"], observer.Notifications);
        Assert.Single(outgoing.Messages);
        OutgoingMessageRecorder.SendObservation firstSend = Assert.Single(outgoing.SendObservations);
        Assert.Equal(TimeSpan.FromSeconds(3), firstSend.Delay);
        Assert.Equal(1, firstSend.RedeliveryCount);
        Assert.NotEqual(firstContext.MessageId, firstSend.MessageId);
        Assert.Equal(0, terminalInvocationCount);

        handle.Disconnect();
        ConsumeContext<MissingMessage> secondContext = CreateContext(NewId.NextGuid(), outgoing);
        await pipe.SendAsync(secondContext);

        Assert.Equal(["post-create", "post-fault"], observer.Notifications);
        Assert.Equal(2, outgoing.Messages.Count);
        OutgoingMessageRecorder.SendObservation secondSend = outgoing.SendObservations[1];
        Assert.Equal(TimeSpan.FromSeconds(3), secondSend.Delay);
        Assert.Equal(1, secondSend.RedeliveryCount);
        Assert.NotEqual(secondContext.MessageId, secondSend.MessageId);
        Assert.Equal(0, terminalInvocationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "required-configuration-arguments")]
    public void Configurator_RejectsEveryNullRequiredArgument()
    {
        IPipe<ConsumeContext<MissingMessage>> terminalPipe = Pipe.ExecuteAwaited<ConsumeContext<MissingMessage>>(
            _ => Task.CompletedTask);
        var missingInstance = new StubMissingInstanceConfigurator(terminalPipe);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MissingInstanceRedeliveryExtensions.Redeliver<MissingState, MissingMessage>(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(null!)).ParamName);

        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
                configurator.SetRetryPolicy(null!))).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
                configurator.OnRedeliveryLimitReached(null!))).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
                configurator.ConnectRetryObserver(null!))).ParamName);
        ConfigurationException retryPolicyException = Assert.Throws<ConfigurationException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
                configurator.SetRetryPolicy(_ => null!)));
        Assert.Equal("retryPolicy", Assert.IsType<ArgumentNullException>(retryPolicyException.InnerException).ParamName);
        Assert.Throws<ConfigurationException>(() =>
            missingInstance.Redeliver<MissingState, MissingMessage>(configurator =>
            {
                configurator.SetRetryPolicy(_ => Retry.None);
                configurator.OnRedeliveryLimitReached(_ => null!);
            }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-MISSING", "invalid-retry-policy-context")]
    public async Task RetryPolicy_ReturningNoContextFailsBeforeObserversOrTerminalBehaviorAsync()
    {
        var terminalInvocationCount = 0;
        IPipe<ConsumeContext<MissingMessage>> terminalPipe = Pipe.ExecuteAwaited<ConsumeContext<MissingMessage>>(_ =>
        {
            terminalInvocationCount++;
            return Task.CompletedTask;
        });
        var observer = new RecordingRetryObserver();
        IPipe<ConsumeContext<MissingMessage>> pipe = new StubMissingInstanceConfigurator(terminalPipe)
            .Redeliver<MissingState, MissingMessage>(configurator =>
            {
                configurator.SetRetryPolicy(_ => new NullContextRetryPolicy());
                configurator.ConnectRetryObserver(observer);
            });
        ConsumeContext<MissingMessage> context = CreateContext(NewId.NextGuid(), new OutgoingMessageRecorder());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => pipe.SendAsync(context));

        Assert.Contains("null policy context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(observer.Notifications);
        Assert.Equal(0, terminalInvocationCount);
    }

    private static ConsumeContext<MissingMessage> CreateContext(Guid correlationId, OutgoingMessageRecorder outgoingMessages) =>
        InMemoryOutboxTestContextFactory.Create(
            new MissingMessage(correlationId),
            TestContext.Current.CancellationToken,
            outgoingMessages: outgoingMessages,
            correlationId: correlationId);

    public sealed record MissingMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed class MissingState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class StubMissingInstanceConfigurator(
        IPipe<ConsumeContext<MissingMessage>> terminalPipe) :
        IMissingInstanceConfigurator<MissingState, MissingMessage>
    {
        public IPipe<ConsumeContext<MissingMessage>> Discard() => terminalPipe;

        public IPipe<ConsumeContext<MissingMessage>> Fault() => terminalPipe;

        public IPipe<ConsumeContext<MissingMessage>> ExecuteAwaited(
            Func<ConsumeContext<MissingMessage>, Task> callback) =>
            Pipe.ExecuteAwaited(callback);

        public IPipe<ConsumeContext<MissingMessage>> Execute(
            Action<ConsumeContext<MissingMessage>> callback) =>
            Pipe.Execute(callback);
    }

    private sealed class RecordingRetryObserver : IRetryObserver
    {
        public List<string> Notifications { get; } = [];

        public Exception? Exception { get; private set; }

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext
        {
            Notifications.Add("post-create");
            return Task.CompletedTask;
        }

        public Task PostFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Notifications.Add("post-fault");
            return Task.CompletedTask;
        }

        public Task PreRetryAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Notifications.Add("pre-retry");
            return Task.CompletedTask;
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Notifications.Add("retry-fault");
            Exception = context.Exception;
            return Task.CompletedTask;
        }

        public Task RetryCompleteAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Notifications.Add("retry-complete");
            return Task.CompletedTask;
        }
    }

    private sealed class NullContextRetryPolicy : IRetryPolicy
    {
        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext =>
            null!;

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }
    }
}
