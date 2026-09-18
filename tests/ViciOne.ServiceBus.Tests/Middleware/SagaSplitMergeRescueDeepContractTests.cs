using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class SagaSplitMergeRescueDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "split-merge-required-null-boundaries")]
    public void SplitMergeAndRescue_RejectEveryMissingRequiredInputBeforeCollaboratorEffects()
    {
        TrackingSagaContext original = CreateSagaContext();
        var output = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>(
            "output", Task.CompletedTask);
        var sagaStage = new RecordingFilter<SagaConsumeContext<TestSaga>>("saga-stage");
        var messageStage = new RecordingFilter<ConsumeContext<TestMessage>>("message-stage");
        var sagaSplit = new SagaSplitFilter<TestSaga, TestMessage>(sagaStage);
        var messageSplit = new SagaMessageSplitFilter<TestSaga, TestMessage>(messageStage);
        var sagaMerge = new SagaMergePipe<TestSaga, TestMessage>(output);
        var messageMerge = new SagaMessageMergePipe<TestSaga, TestMessage>(output, original);
        var failure = new ExpectedFailure("rescue failure");

        AssertParameter("next", () => new SagaSplitFilter<TestSaga, TestMessage>(null!));
        AssertParameter("next", () => new SagaMessageSplitFilter<TestSaga, TestMessage>(null!));
        AssertParameter("output", () => new SagaMergePipe<TestSaga, TestMessage>(null!));
        AssertParameter("output", () => new SagaMessageMergePipe<TestSaga, TestMessage>(null!, original));
        AssertParameter("context", () => new SagaMessageMergePipe<TestSaga, TestMessage>(output, null!));
        AssertParameter("context", () => new RescueExceptionSagaConsumeContext<TestSaga>(null!, failure));
        AssertParameter("exception", () => new RescueExceptionSagaConsumeContext<TestSaga>(original, null!));

        AssertParameter("context", () => ((IProbeSite)sagaSplit).Probe(null!));
        AssertParameter("context", () => ((IProbeSite)messageSplit).Probe(null!));
        AssertParameter("context", () => ((IProbeSite)sagaMerge).Probe(null!));
        AssertParameter("context", () => ((IProbeSite)messageMerge).Probe(null!));

        AssertParameter("context", () => sagaMerge.SendAsync(null!));
        AssertParameter("context", () => messageMerge.SendAsync(null!));
        AssertParameter("context", () => sagaSplit.SendAsync(null!, output));
        AssertParameter("next", () => sagaSplit.SendAsync(original, null!));
        AssertParameter("context", () => messageSplit.SendAsync(null!, output));
        AssertParameter("next", () => messageSplit.SendAsync(original, null!));

        Assert.Equal(0, output.Count);
        Assert.Equal(0, sagaStage.Count);
        Assert.Equal(0, messageStage.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "split-merge-probe-structure-and-downstream-identity")]
    public void Probe_ReportsTheExactSplitOrMergeShapeAndNestedCollaboratorIdentity()
    {
        TrackingSagaContext original = CreateSagaContext();
        var sagaStage = new RecordingFilter<SagaConsumeContext<TestSaga>>("saga-stage");
        var messageStage = new RecordingFilter<ConsumeContext<TestMessage>>("message-stage");
        var output = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>(
            "typed-output", Task.CompletedTask);

        AssertProbe(
            new SagaSplitFilter<TestSaga, TestMessage>(sagaStage),
            "split",
            "saga-stage",
            expectedSagaType: TypeCache<TestSaga>.ShortName,
            expectedMessageType: null);
        AssertProbe(
            new SagaMessageSplitFilter<TestSaga, TestMessage>(messageStage),
            "split",
            "message-stage",
            expectedSagaType: null,
            expectedMessageType: TypeCache<TestMessage>.ShortName);
        AssertProbe(
            new SagaMergePipe<TestSaga, TestMessage>(output),
            "merge",
            "typed-output",
            TypeCache<TestSaga>.ShortName,
            TypeCache<TestMessage>.ShortName);
        AssertProbe(
            new SagaMessageMergePipe<TestSaga, TestMessage>(output, original),
            "merge",
            "typed-output",
            TypeCache<TestSaga>.ShortName,
            TypeCache<TestMessage>.ShortName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "public-merge-compatibility-and-reference-fast-paths")]
    public void MergePipes_PreserveThePublicSagaMergeContractAndBothExactReferenceFastPaths()
    {
        TrackingSagaContext original = CreateSagaContext();
        var compatibilityCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var compatibilityOutput = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>(
            "compatibility-output", compatibilityCompletion.Task);
        var publicSagaMerge = new SagaMergePipe<TestSaga, TestMessage>(compatibilityOutput);

        ConstructorInfo constructor = Assert.Single(
            typeof(SagaMergePipe<TestSaga, TestMessage>).GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal("output", parameter.Name);
        Assert.Equal(typeof(IPipe<SagaConsumeContext<TestSaga, TestMessage>>), parameter.ParameterType);

        Task sagaResult = publicSagaMerge.SendAsync(original);

        Assert.Same(compatibilityCompletion.Task, sagaResult);
        Assert.Same(original, compatibilityOutput.Context);
        Assert.Equal(1, compatibilityOutput.Count);

        var sagaOnly = new SagaOnlyAdapter(original, new TestSaga(Guid.NewGuid()), Task.CompletedTask);
        ArgumentException missingMessage = Assert.Throws<ArgumentException>(() =>
        {
            _ = publicSagaMerge.SendAsync(sagaOnly);
        });
        Assert.Equal("context", missingMessage.ParamName);
        Assert.Contains(TypeCache<TestMessage>.ShortName, missingMessage.Message, StringComparison.Ordinal);
        Assert.Equal(1, compatibilityOutput.Count);

        var sagaCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sagaOutput = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>(
            "saga-output", sagaCompletion.Task);
        var sagaSplit = new SagaSplitFilter<TestSaga, TestMessage>(
            new RecordingFilter<SagaConsumeContext<TestSaga>>("saga-stage"));

        Task sagaSplitResult = sagaSplit.SendAsync(original, sagaOutput);

        Assert.Same(sagaCompletion.Task, sagaSplitResult);
        Assert.Same(original, sagaOutput.Context);
        Assert.Equal(1, sagaOutput.Count);

        var messageCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var messageOutput = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>(
            "message-output", messageCompletion.Task);
        var messageSplit = new SagaMessageSplitFilter<TestSaga, TestMessage>(
            new RecordingFilter<ConsumeContext<TestMessage>>("message-stage"));

        Task messageResult = messageSplit.SendAsync(original, messageOutput);

        Assert.Same(messageCompletion.Task, messageResult);
        Assert.Same(original, messageOutput.Context);
        Assert.Equal(1, messageOutput.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "saga-split-typed-adapter-preserves-message-owner")]
    public async Task SagaSplit_TypedAdapterPreservesOriginalMessageAndAdaptedSagaOwnersAsync()
    {
        var originalMessage = new TestMessage("original-message");
        var originalPayload = new TestPayload("original-payload");
        using var originalCancellation = new CancellationTokenSource();
        var originalSaga = new TestSaga(Guid.NewGuid());
        var original = new TrackingSagaContext(
            CreateMessageContext(originalMessage, originalPayload, originalCancellation.Token),
            originalSaga,
            Task.CompletedTask);

        var adaptedMessage = new TestMessage("adapted-message");
        var adaptedPayload = new TestPayload("adapted-payload");
        using var adaptedCancellation = new CancellationTokenSource();
        var adaptedSaga = new TestSaga(Guid.NewGuid());
        var completionFailure = new ExpectedFailure("adapted completion failed");
        Task adaptedCompletion = Task.FromException(completionFailure);
        var adapter = new TrackingSagaContext(
            CreateMessageContext(adaptedMessage, adaptedPayload, adaptedCancellation.Token),
            adaptedSaga,
            adaptedCompletion)
        {
            IsCompleted = true
        };
        var sagaStage = new ReplacingFilter<SagaConsumeContext<TestSaga>>("typed-saga-adapter", adapter);
        var outputFailure = new ExpectedFailure("typed output failed");
        Task outputTask = Task.FromException(outputFailure);
        var output = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>("typed-output", outputTask);
        var split = new SagaSplitFilter<TestSaga, TestMessage>(sagaStage);

        Task returned = split.SendAsync(original, output);

        Assert.Same(outputTask, returned);
        ExpectedFailure observedOutput = await Assert.ThrowsAsync<ExpectedFailure>(() => returned);
        Assert.Same(outputFailure, observedOutput);
        Assert.Equal(1, sagaStage.Count);
        Assert.Equal(1, output.Count);
        SagaConsumeContext<TestSaga, TestMessage> merged = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(
            output.Context);
        Assert.Same(originalMessage, merged.Message);
        Assert.Same(originalPayload, AssertPayload<TestPayload>(merged));
        Assert.Equal(original.MessageId, merged.MessageId);
        Assert.Equal(originalCancellation.Token, merged.CancellationToken);
        Assert.Same(adaptedSaga, merged.Saga);
        Assert.Equal(adaptedSaga.CorrelationId, merged.CorrelationId);
        Assert.True(merged.IsCompleted);
        Assert.True(merged.TryGetPayload(out SagaConsumeContext<TestSaga>? sagaView));
        Assert.Same(adaptedSaga, sagaView.Saga);

        using var requestedCancellation = new CancellationTokenSource();
        Task completion = merged.SetCompletedAsync(requestedCancellation.Token);

        Assert.Same(adaptedCompletion, completion);
        Assert.Equal(requestedCancellation.Token, adapter.CompletionToken);
        ExpectedFailure observedCompletion = await Assert.ThrowsAsync<ExpectedFailure>(() => completion);
        Assert.Same(completionFailure, observedCompletion);
        Assert.Equal(1, adapter.CompletionCount);
        Assert.Equal(0, original.CompletionCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "split-immediate-stage-task-identity")]
    public async Task SplitFilters_ReturnTheImmediateStageTaskInsteadOfTheNestedMergeTaskAsync()
    {
        TrackingSagaContext original = CreateSagaContext();
        var sagaAdapter = new SagaOnlyAdapter(original, new TestSaga(Guid.NewGuid()), Task.CompletedTask);
        var sagaOutput = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>("saga-output", Task.CompletedTask);
        var sagaStageFailure = new ExpectedFailure("saga stage failed after merge");
        Task sagaStageTask = Task.FromException(sagaStageFailure);
        var sagaStage = new DistinctResultReplacingFilter<SagaConsumeContext<TestSaga>>(sagaAdapter, sagaStageTask);

        Task sagaReturned = new SagaSplitFilter<TestSaga, TestMessage>(sagaStage).SendAsync(original, sagaOutput);

        Assert.Same(sagaStageTask, sagaReturned);
        Assert.Same(sagaOutput.Result, sagaStage.NestedTask);
        Assert.Equal(1, sagaStage.Count);
        Assert.Equal(1, sagaOutput.Count);
        ExpectedFailure observedSagaFailure = await Assert.ThrowsAsync<ExpectedFailure>(() => sagaReturned);
        Assert.Same(sagaStageFailure, observedSagaFailure);

        using var messageStageCancellation = new CancellationTokenSource();
        messageStageCancellation.Cancel();
        Task messageStageTask = Task.FromCanceled(messageStageCancellation.Token);
        var adaptedMessage = CreateMessageContext(
            new TestMessage("adapted-message"),
            new TestPayload("adapted-payload"),
            TestContext.Current.CancellationToken);
        var messageOutput = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>("message-output", Task.CompletedTask);
        var messageStage = new DistinctResultReplacingFilter<ConsumeContext<TestMessage>>(adaptedMessage, messageStageTask);

        Task messageReturned = new SagaMessageSplitFilter<TestSaga, TestMessage>(messageStage).SendAsync(original, messageOutput);

        Assert.Same(messageStageTask, messageReturned);
        Assert.Same(messageOutput.Result, messageStage.NestedTask);
        Assert.Equal(1, messageStage.Count);
        Assert.Equal(1, messageOutput.Count);
        OperationCanceledException observedCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => messageReturned);
        Assert.Equal(messageStageCancellation.Token, observedCancellation.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "saga-split-saga-only-adapter-preserves-message-owner")]
    public async Task SagaSplit_SagaOnlyAdapterPreservesItsLiveStateAndTheOriginalMessageOwnerAsync()
    {
        using var deliveryCancellation = new CancellationTokenSource();
        var message = new TestMessage("original-message");
        var originalPayload = new TestPayload("original-payload");
        ConsumeContext<TestMessage> messageContext = CreateMessageContext(
            message, originalPayload, deliveryCancellation.Token);
        var originalSaga = new TestSaga(Guid.NewGuid());
        var original = new TrackingSagaContext(messageContext, originalSaga, Task.CompletedTask);

        using var completionCancellation = new CancellationTokenSource();
        completionCancellation.Cancel();
        Task adapterCompletion = Task.FromCanceled(completionCancellation.Token);
        var adapterSaga = new TestSaga(Guid.NewGuid());
        var sagaSidePayload = new TestPayload("saga-side-payload");
        var sagaSideContext = new ConsumeContextScope(original, sagaSidePayload);
        var adapter = new SagaOnlyAdapter(sagaSideContext, adapterSaga, adapterCompletion);
        Assert.True(adapter.TryGetPayload(out TestPayload? adapterPayload));
        Assert.Same(sagaSidePayload, adapterPayload);
        var sagaStage = new ReplacingFilter<SagaConsumeContext<TestSaga>>("saga-adapter", adapter);
        var sendFailure = new ExpectedFailure("typed output failed");
        Task outputTask = Task.FromException(sendFailure);
        var output = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>("typed-output", outputTask);
        var split = new SagaSplitFilter<TestSaga, TestMessage>(sagaStage);

        Task returned = split.SendAsync(original, output);

        Assert.Same(outputTask, returned);
        ExpectedFailure observed = await Assert.ThrowsAsync<ExpectedFailure>(() => returned);
        Assert.Same(sendFailure, observed);
        Assert.Same(original, sagaStage.Input);
        Assert.Equal(1, sagaStage.Count);
        Assert.Equal(1, output.Count);
        SagaConsumeContext<TestSaga, TestMessage> merged = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(
            output.Context);
        Assert.NotSame(original, merged);
        Assert.NotSame(adapter, merged);
        Assert.Same(message, merged.Message);
        Assert.Same(adapterSaga, merged.Saga);
        Assert.Equal(adapterSaga.CorrelationId, merged.CorrelationId);
        Assert.Equal(original.MessageId, merged.MessageId);
        Assert.Equal(deliveryCancellation.Token, merged.CancellationToken);
        Assert.True(merged.TryGetPayload(out TestPayload? selectedPayload));
        Assert.Same(originalPayload, selectedPayload);
        Assert.True(merged.TryGetPayload(out SagaConsumeContext<TestSaga, TestMessage>? selectedSagaContext));
        Assert.Same(merged, selectedSagaContext);
        Assert.True(merged.TryGetPayload(out SagaConsumeContext<TestSaga>? sagaView));
        Assert.Same(adapterSaga, sagaView.Saga);

        using var requestedCancellation = new CancellationTokenSource();
        Task completion = merged.SetCompletedAsync(requestedCancellation.Token);

        Assert.Same(adapterCompletion, completion);
        Assert.Equal(requestedCancellation.Token, adapter.CompletionToken);
        OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completion);
        Assert.Equal(completionCancellation.Token, cancellation.CancellationToken);
        Assert.Equal(1, adapter.CompletionCount);
        Assert.Equal(0, original.CompletionCount);

        var replacementSaga = new TestSaga(Guid.NewGuid());
        adapter.Saga = replacementSaga;
        adapter.IsCompleted = true;
        Assert.Same(replacementSaga, merged.Saga);
        Assert.True(merged.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-PIPE-LAYERS", "message-split-conflicting-typed-adapter-preserves-saga-owner")]
    public async Task MessageSplit_ConflictingTypedSagaAdapterCannotReplaceTheOriginalSagaOwnerAsync()
    {
        ConsumeContext<TestMessage> originalMessageContext = CreateMessageContext(
            new TestMessage("original-message"),
            new TestPayload("original-payload"),
            TestContext.Current.CancellationToken);
        var originalSaga = new TestSaga(Guid.NewGuid());
        var completionFailure = new ExpectedFailure("completion failed");
        Task originalCompletion = Task.FromException(completionFailure);
        var original = new TrackingSagaContext(originalMessageContext, originalSaga, originalCompletion);

        using var adaptedDeliveryCancellation = new CancellationTokenSource();
        var adaptedMessage = new TestMessage("adapted-message");
        var adaptedPayload = new TestPayload("adapted-payload");
        ConsumeContext<TestMessage> adaptedMessageContext = CreateMessageContext(
            adaptedMessage, adaptedPayload, adaptedDeliveryCancellation.Token);
        var conflictingSaga = new TestSaga(Guid.NewGuid());
        var conflictingContext = new TrackingSagaContext(
            adaptedMessageContext, conflictingSaga, Task.CompletedTask);
        var messageStage = new ReplacingFilter<ConsumeContext<TestMessage>>(
            "conflicting-typed-saga-adapter", conflictingContext);

        using var outputCancellation = new CancellationTokenSource();
        outputCancellation.Cancel();
        Task outputTask = Task.FromCanceled(outputCancellation.Token);
        var output = new RecordingPipe<SagaConsumeContext<TestSaga, TestMessage>>("typed-output", outputTask);
        var split = new SagaMessageSplitFilter<TestSaga, TestMessage>(messageStage);

        Task returned = split.SendAsync(original, output);

        Assert.Same(outputTask, returned);
        OperationCanceledException sendCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        Assert.Equal(outputCancellation.Token, sendCancellation.CancellationToken);
        Assert.Same(original, messageStage.Input);
        Assert.Equal(1, messageStage.Count);
        Assert.Equal(1, output.Count);
        SagaConsumeContext<TestSaga, TestMessage> merged = Assert.IsAssignableFrom<SagaConsumeContext<TestSaga, TestMessage>>(
            output.Context);
        Assert.NotSame(original, merged);
        Assert.NotSame(conflictingContext, merged);
        Assert.Same(adaptedMessage, merged.Message);
        Assert.Same(originalSaga, merged.Saga);
        Assert.NotSame(conflictingSaga, merged.Saga);
        Assert.Equal(originalSaga.CorrelationId, merged.CorrelationId);
        Assert.Equal(adaptedDeliveryCancellation.Token, merged.CancellationToken);
        Assert.True(merged.TryGetPayload(out TestPayload? selectedPayload));
        Assert.Same(adaptedPayload, selectedPayload);
        Assert.True(merged.TryGetPayload(out SagaConsumeContext<TestSaga, TestMessage>? selectedSagaContext));
        Assert.Same(merged, selectedSagaContext);
        Assert.True(merged.TryGetPayload(out SagaConsumeContext<TestSaga>? sagaView));
        Assert.Same(originalSaga, sagaView.Saga);

        using var requestedCancellation = new CancellationTokenSource();
        Task completion = merged.SetCompletedAsync(requestedCancellation.Token);

        Assert.Same(originalCompletion, completion);
        Assert.Equal(requestedCancellation.Token, original.CompletionToken);
        ExpectedFailure observed = await Assert.ThrowsAsync<ExpectedFailure>(() => completion);
        Assert.Same(completionFailure, observed);
        Assert.Equal(1, original.CompletionCount);
        Assert.Equal(0, conflictingContext.CompletionCount);

        var replacementSaga = new TestSaga(Guid.NewGuid());
        original.Saga = replacementSaga;
        original.IsCompleted = true;
        Assert.Same(replacementSaga, merged.Saga);
        Assert.True(merged.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESCUE", "saga-rescue-concurrent-snapshot-and-live-forwarding")]
    public async Task SagaRescue_PublishesOneFailureSnapshotAndForwardsLiveSagaCompletionExactlyAsync()
    {
        var payload = new TestPayload("rescue-payload");
        ConsumeContext<TestMessage> messageContext = CreateMessageContext(
            new TestMessage("failed-message"), payload, TestContext.Current.CancellationToken);
        var saga = new TestSaga(Guid.NewGuid());
        var completionFailure = new ExpectedFailure("completion failed");
        Task completionTask = Task.FromException(completionFailure);
        var source = new TrackingSagaContext(messageContext, saga, completionTask);
        var originalFailure = new ExpectedFailure("consume failed");
        var rescue = new RescueExceptionSagaConsumeContext<TestSaga>(source, originalFailure);

        const int ReaderCount = 8;
        using var readersReady = new CountdownEvent(ReaderCount);
        using var releaseReaders = new ManualResetEventSlim();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        Task<ExceptionInfo>[] readers = Enumerable.Range(0, ReaderCount)
            .Select(_ => Task.Factory.StartNew(
                () =>
                {
                    readersReady.Signal();
                    releaseReaders.Wait(testCancellation);
                    return rescue.ExceptionInfo;
                },
                testCancellation,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        try
        {
            readersReady.Wait(testCancellation);
            releaseReaders.Set();
            ExceptionInfo[] snapshots = await Task.WhenAll(readers);

            Assert.All(snapshots, snapshot => Assert.Same(snapshots[0], snapshot));
            Assert.Same(snapshots[0], rescue.ExceptionInfo);
            Assert.Equal(originalFailure.Message, snapshots[0].Message);
            Assert.Equal(originalFailure.GetType().FullName, snapshots[0].ExceptionType);
        }
        finally
        {
            releaseReaders.Set();
        }

        Assert.Same(originalFailure, rescue.Exception);
        Assert.Same(saga, rescue.Saga);
        Assert.False(rescue.IsCompleted);
        Assert.True(rescue.TryGetPayload(out TestPayload? selectedPayload));
        Assert.Same(payload, selectedPayload);

        using var requestedCancellation = new CancellationTokenSource();
        Task returned = rescue.SetCompletedAsync(requestedCancellation.Token);

        Assert.Same(completionTask, returned);
        Assert.Equal(requestedCancellation.Token, source.CompletionToken);
        ExpectedFailure observed = await Assert.ThrowsAsync<ExpectedFailure>(() => returned);
        Assert.Same(completionFailure, observed);
        Assert.Equal(1, source.CompletionCount);

        var replacementSaga = new TestSaga(Guid.NewGuid());
        source.Saga = replacementSaga;
        source.IsCompleted = true;
        Assert.Same(replacementSaga, rescue.Saga);
        Assert.True(rescue.IsCompleted);
    }

    private static TrackingSagaContext CreateSagaContext()
    {
        ConsumeContext<TestMessage> context = CreateMessageContext(
            new TestMessage("message"),
            new TestPayload("payload"),
            TestContext.Current.CancellationToken);
        return new TrackingSagaContext(context, new TestSaga(Guid.NewGuid()), Task.CompletedTask);
    }

    private static ConsumeContext<TestMessage> CreateMessageContext(
        TestMessage message,
        TestPayload payload,
        CancellationToken cancellationToken)
    {
        ConsumeContext<TestMessage> context = InMemoryOutboxTestContextFactory.Create(message, cancellationToken);
        context.AddOrUpdatePayload(() => payload, _ => payload);
        return context;
    }

    private static void AssertProbe(
        IProbeSite site,
        string expectedFilterType,
        string expectedCollaborator,
        string? expectedSagaType,
        string? expectedMessageType)
    {
        IProbeResult result = site.GetProbeResult(TestContext.Current.CancellationToken);
        IReadOnlyDictionary<string, object> scope = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("filters", result.Results));

        Assert.Equal(expectedFilterType, Assert.Contains("filterType", scope));
        Assert.Equal(expectedCollaborator, Assert.Contains("collaboratorIdentity", scope));

        if (expectedSagaType is null)
            Assert.DoesNotContain("sagaType", scope);
        else
            Assert.Equal(expectedSagaType, Assert.Contains("sagaType", scope));

        if (expectedMessageType is null)
            Assert.DoesNotContain("messageType", scope);
        else
            Assert.Equal(expectedMessageType, Assert.Contains("messageType", scope));
    }

    private static void AssertParameter(string parameterName, Action action)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }

    private static TPayload AssertPayload<TPayload>(PipeContext context)
        where TPayload : class
    {
        Assert.True(context.TryGetPayload(out TPayload? payload));
        return Assert.IsType<TPayload>(payload);
    }

    public sealed record TestMessage(string Value);

    public sealed record TestPayload(string Value);

    private sealed class TestSaga(Guid correlationId) : ISaga
    {
        public Guid CorrelationId { get; set; } = correlationId;
    }

    private sealed class ExpectedFailure(string message) : Exception(message);

    private sealed class TrackingSagaContext :
        ConsumeContextProxy<TestMessage>,
        SagaConsumeContext<TestSaga, TestMessage>
    {
        private int _completionCount;

        public TrackingSagaContext(ConsumeContext<TestMessage> context, TestSaga saga, Task completionTask)
            : base(context)
        {
            Saga = saga;
            CompletionTask = completionTask;
        }

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TestSaga Saga { get; set; }

        public bool IsCompleted { get; set; }

        public Task CompletionTask { get; set; }

        public CancellationToken CompletionToken { get; private set; }

        public int CompletionCount => Volatile.Read(ref _completionCount);

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            CompletionToken = cancellationToken;
            Interlocked.Increment(ref _completionCount);
            return CompletionTask;
        }
    }

    private sealed class SagaOnlyAdapter :
        ConsumeContextProxy,
        SagaConsumeContext<TestSaga>
    {
        private int _completionCount;

        public SagaOnlyAdapter(ConsumeContext context, TestSaga saga, Task completionTask)
            : base(context)
        {
            Saga = saga;
            CompletionTask = completionTask;
        }

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TestSaga Saga { get; set; }

        public bool IsCompleted { get; set; }

        public Task CompletionTask { get; set; }

        public CancellationToken CompletionToken { get; private set; }

        public int CompletionCount => Volatile.Read(ref _completionCount);

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            CompletionToken = cancellationToken;
            Interlocked.Increment(ref _completionCount);
            return CompletionTask;
        }
    }

    private sealed class ReplacingFilter<TContext>(string identity, TContext replacement) :
        IFilter<TContext>
        where TContext : class, PipeContext
    {
        private int _count;

        public TContext? Input { get; private set; }

        public int Count => Volatile.Read(ref _count);

        public Task SendAsync(TContext context, IPipe<TContext> next)
        {
            Input = context;
            Interlocked.Increment(ref _count);
            return next.SendAsync(replacement);
        }

        public void Probe(ProbeContext context)
        {
            context.Add("collaboratorIdentity", identity);
        }
    }

    private sealed class RecordingFilter<TContext>(string identity) :
        IFilter<TContext>
        where TContext : class, PipeContext
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task SendAsync(TContext context, IPipe<TContext> next)
        {
            Interlocked.Increment(ref _count);
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context)
        {
            context.Add("collaboratorIdentity", identity);
        }
    }

    private sealed class RecordingPipe<TContext>(string identity, Task result) :
        IPipe<TContext>
        where TContext : class, PipeContext
    {
        private int _count;

        public TContext? Context { get; private set; }

        public int Count => Volatile.Read(ref _count);

        public Task Result => result;

        public Task SendAsync(TContext context)
        {
            Context = context;
            Interlocked.Increment(ref _count);
            return result;
        }

        public void Probe(ProbeContext context)
        {
            context.Add("collaboratorIdentity", identity);
        }
    }

    private sealed class DistinctResultReplacingFilter<TContext>(TContext replacement, Task result) :
        IFilter<TContext>
        where TContext : class, PipeContext
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task? NestedTask { get; private set; }

        public Task SendAsync(TContext context, IPipe<TContext> next)
        {
            Interlocked.Increment(ref _count);
            NestedTask = next.SendAsync(replacement);
            return result;
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
