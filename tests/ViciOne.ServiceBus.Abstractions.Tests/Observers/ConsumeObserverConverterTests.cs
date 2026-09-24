using System.Reflection;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Observers;

public sealed class ConsumeObserverConverterTests
{
    [Theory]
    [InlineData(ObservationStage.Pre)]
    [InlineData(ObservationStage.Post)]
    [InlineData(ObservationStage.Fault)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVATION", "converter-forwards-exact-typed-context-stage-and-fault")]
    public async Task Converter_ForwardsTheExactTypedContextAndObserverTaskAsync(ObservationStage stage)
    {
        IConsumeObserverConverter converter = new ConsumeObserverConverter<ObservedMessage>();
        ConsumeContext<ObservedMessage> context = DispatchProxy.Create<ConsumeContext<ObservedMessage>, UnexpectedContextProxy>();
        var observer = new RecordingObserver();
        var failure = new InvalidOperationException("consumer failed");
        using var cancellation = new CancellationTokenSource();

        Task notification = InvokeAsync(stage, converter, observer, context, failure, cancellation.Token);

        Assert.Same(observer.Completion.Task, notification);
        Assert.False(notification.IsCompleted);
        Observation call = Assert.Single(observer.Calls);
        Assert.Equal(stage, call.Stage);
        Assert.Equal(typeof(ObservedMessage), call.MessageType);
        Assert.Same(context, call.Context);
        Assert.Same(stage == ObservationStage.Fault ? failure : null, call.Failure);

        cancellation.Cancel();
        Assert.False(notification.IsCompleted);
        observer.Completion.SetResult();
        await notification;
        Assert.Single(observer.Calls);
    }

    [Theory]
    [InlineData(ObservationStage.Pre)]
    [InlineData(ObservationStage.Post)]
    [InlineData(ObservationStage.Fault)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVATION", "converter-propagates-observer-failure-without-retry")]
    public async Task Converter_PropagatesTheExactObserverFailureWithoutRetryAsync(ObservationStage stage)
    {
        IConsumeObserverConverter converter = new ConsumeObserverConverter<ObservedMessage>();
        ConsumeContext<ObservedMessage> context = DispatchProxy.Create<ConsumeContext<ObservedMessage>, UnexpectedContextProxy>();
        var observer = new RecordingObserver();
        var observerFailure = new InvalidOperationException("observer rejected notification");

        Task notification = InvokeAsync(stage, converter, observer, context, new ApplicationException("consume fault"), CancellationToken.None);
        observer.Completion.SetException(observerFailure);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => notification);
        Assert.Same(observerFailure, actual);
        Assert.Single(observer.Calls);
    }

    [Theory]
    [InlineData(ObservationStage.Pre)]
    [InlineData(ObservationStage.Post)]
    [InlineData(ObservationStage.Fault)]
    [RequirementCoverage("REQ-VSB-CONSUME-OBSERVATION", "converter-rejects-missing-and-wrong-context-before-notification")]
    public async Task Converter_RejectsInvalidInputsBeforeCallingTheObserverAsync(ObservationStage stage)
    {
        IConsumeObserverConverter converter = new ConsumeObserverConverter<ObservedMessage>();
        ConsumeContext<ObservedMessage> context = DispatchProxy.Create<ConsumeContext<ObservedMessage>, UnexpectedContextProxy>();
        ConsumeContext<OtherMessage> wrongContext = DispatchProxy.Create<ConsumeContext<OtherMessage>, UnexpectedContextProxy>();
        var observer = new RecordingObserver();
        var failure = new InvalidOperationException("consumer failed");

        Assert.Equal("observer", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            InvokeAsync(stage, converter, null!, context, failure, CancellationToken.None))).ParamName);
        Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            InvokeAsync(stage, converter, observer, null!, failure, CancellationToken.None))).ParamName);
        ArgumentException mismatch = await Assert.ThrowsAsync<ArgumentException>(() =>
            InvokeAsync(stage, converter, observer, wrongContext, failure, CancellationToken.None));
        Assert.Contains(wrongContext.GetType().Name, mismatch.Message, StringComparison.Ordinal);
        Assert.Empty(observer.Calls);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            InvokeAsync(stage, converter, null!, null!, failure, canceled.Token));
        Assert.Equal(canceled.Token, cancellation.CancellationToken);
        Assert.Empty(observer.Calls);
    }

    private static Task InvokeAsync(ObservationStage stage, IConsumeObserverConverter converter, IConsumeObserver observer,
        object context, Exception exception, CancellationToken cancellationToken) => stage switch
        {
            ObservationStage.Pre => converter.PreConsumeAsync(observer, context, cancellationToken),
            ObservationStage.Post => converter.PostConsumeAsync(observer, context, cancellationToken),
            ObservationStage.Fault => converter.ConsumeFaultAsync(observer, context, exception, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };

    public enum ObservationStage { Pre, Post, Fault }

    public sealed record ObservedMessage;

    public sealed record OtherMessage;

    private sealed record Observation(ObservationStage Stage, Type MessageType, object Context, Exception? Failure);

    private sealed class RecordingObserver : IConsumeObserver
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<Observation> Calls { get; } = [];

        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => RecordAsync(ObservationStage.Pre, context);

        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class => RecordAsync(ObservationStage.Post, context);

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class =>
            RecordAsync(ObservationStage.Fault, context, exception);

        private Task RecordAsync<T>(ObservationStage stage, ConsumeContext<T> context, Exception? exception = null) where T : class
        {
            Calls.Add(new Observation(stage, typeof(T), context, exception));
            return Completion.Task;
        }
    }

    private class UnexpectedContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The converter unexpectedly read {targetMethod?.Name}.");
    }
}
