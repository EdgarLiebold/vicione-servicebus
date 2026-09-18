using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineBehaviorBuilderDeepContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-builder-null-add-atomicity")]
    public async Task Builder_AddNullRejectsAtomicallyAndLeavesTheBuilderUsableAsync(bool catchBuilder)
    {
        BuilderHarness harness = CreateBuilder(catchBuilder);

        AssertParam("activity", () => harness.Builder.Add(null!));

        var calls = new List<string>();
        harness.Builder.Add(new RecordingActivity("first", calls, followContinuation: true));
        harness.Builder.Add(new RecordingActivity("second", calls, followContinuation: true));

        await harness.GetBehavior().ExecuteAsync(CreateProxy<IBehaviorContext<TestSaga>>());

        Assert.Equal(["first:execute", "second:execute"], calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-builder-empty-snapshot-freeze")]
    public void Builder_EmptyBehaviorIsStableAndMaterializationPermanentlyFreezesConfiguration(bool catchBuilder)
    {
        BuilderHarness harness = CreateBuilder(catchBuilder);

        IBehavior<TestSaga> snapshot = harness.GetBehavior();

        Assert.Same(Behavior.Empty<TestSaga>(), snapshot);
        Assert.Same(snapshot, harness.GetBehavior());
        AssertParam("activity", () => harness.Builder.Add(null!));
        SagaStateMachineException exception = Assert.Throws<SagaStateMachineException>(() =>
            harness.Builder.Add(new RecordingActivity("late", [], followContinuation: true)));
        Assert.Equal("The behavior was already built, additional activities cannot be added.", exception.Message);
        Assert.Same(snapshot, harness.GetBehavior());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-builder-single-multiple-ordered-composition")]
    public async Task Builder_SingleAndMultipleSnapshotsPreserveOrderAcrossEveryTraversalShapeAsync(bool catchBuilder)
    {
        var singleCalls = new List<string>();
        BuilderHarness single = CreateBuilder(catchBuilder);
        single.Builder.Add(new RecordingActivity("single", singleCalls, followContinuation: true));
        IBehavior<TestSaga> singleSnapshot = single.GetBehavior();

        Assert.Same(singleSnapshot, single.GetBehavior());
        await singleSnapshot.ExecuteAsync(CreateProxy<IBehaviorContext<TestSaga>>());
        Assert.Equal(["single:execute"], singleCalls);

        var calls = new List<string>();
        BuilderHarness multiple = CreateBuilder(catchBuilder);
        multiple.Builder.Add(new RecordingActivity("first", calls, followContinuation: true));
        multiple.Builder.Add(new RecordingActivity("second", calls, followContinuation: true));
        multiple.Builder.Add(new RecordingActivity("third", calls, followContinuation: true));
        IBehavior<TestSaga> snapshot = multiple.GetBehavior();

        await snapshot.ExecuteAsync(CreateProxy<IBehaviorContext<TestSaga>>());
        Assert.Equal(["first:execute", "second:execute", "third:execute"], calls);

        calls.Clear();
        await snapshot.ExecuteAsync(CreateProxy<IBehaviorContext<TestSaga, Message>>());
        Assert.Equal(["first:execute-message", "second:execute-message", "third:execute-message"], calls);

        calls.Clear();
        snapshot.Accept(new ContinuingVisitor());
        Assert.Equal(["first:accept", "second:accept", "third:accept"], calls);

        calls.Clear();
        snapshot.Probe(new PassiveProbeContext());
        Assert.Equal(["first:probe", "second:probe", "third:probe"], calls);

        Assert.Same(snapshot, multiple.GetBehavior());
        Assert.Throws<SagaStateMachineException>(() =>
            multiple.Builder.Add(new RecordingActivity("late", calls, followContinuation: true)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-builder-publication-add-linearizability")]
    public async Task Builder_ConcurrentPublicationAndAddHaveOneConsistentLinearizationPointAsync(bool catchBuilder)
    {
        for (var iteration = 0; iteration < 32; iteration++)
        {
            BuilderHarness harness = CreateBuilder(catchBuilder);
            var calls = new List<string>();
            var activity = new RecordingActivity("concurrent", calls, followContinuation: true);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var publicationReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var additionReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task<IBehavior<TestSaga>> publication = Task.Run(async () =>
            {
                publicationReady.SetResult();
                await start.Task;
                return harness.GetBehavior();
            });
            Task<SagaStateMachineException?> addition = Task.Run(async () =>
            {
                additionReady.SetResult();
                await start.Task;
                try
                {
                    harness.Builder.Add(activity);
                    return null;
                }
                catch (SagaStateMachineException exception)
                {
                    return exception;
                }
            });

            await Task.WhenAll(publicationReady.Task, additionReady.Task);
            start.SetResult();
            IBehavior<TestSaga> snapshot = await publication;
            SagaStateMachineException? addFailure = await addition;

            Assert.Same(snapshot, harness.GetBehavior());
            if (addFailure is null)
            {
                await snapshot.ExecuteAsync(CreateProxy<IBehaviorContext<TestSaga>>());
                Assert.Equal(["concurrent:execute"], calls);
            }
            else
            {
                Assert.Equal("The behavior was already built, additional activities cannot be added.", addFailure.Message);
                Assert.Same(Behavior.Empty<TestSaga>(), snapshot);
                Assert.Empty(calls);
            }
        }
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 3)]
    [InlineData(true, 1)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-built-builder-terminal-fault-policy")]
    public void Builder_BuiltFaultTraversalEndsInTheExactPolicyForEveryContextShape(bool catchBuilder, int activityCount)
    {
        var calls = new List<string>();
        BuilderHarness harness = CreateBuilder(catchBuilder);
        RecordingActivity[] activities = Enumerable.Range(0, activityCount)
            .Select(index => new RecordingActivity($"activity-{index}", calls, followContinuation: index < activityCount - 1))
            .ToArray();
        foreach (RecordingActivity activity in activities)
            harness.Builder.Add(activity);

        IBehavior<TestSaga> behavior = harness.GetBehavior();
        RecordingActivity last = activities[^1];
        IBehaviorContext<TestSaga, Message> messageContext = CreateProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> faultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> messageFaultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();
        IBehavior<TestSaga> expectedUntyped = catchBuilder ? Behavior.Empty<TestSaga>() : Behavior.Faulted<TestSaga>();
        IBehavior<TestSaga, Message> expectedTyped = catchBuilder
            ? Behavior.Empty<TestSaga, Message>()
            : Behavior.Faulted<TestSaga, Message>();

        Assert.Same(last.ResultTask, behavior.FaultedAsync(faultContext));
        Assert.Equal(
            Enumerable.Range(0, activityCount).Select(index => $"activity-{index}:fault"),
            calls);
        AssertInvocation(last, "fault", faultContext, expectedUntyped);

        calls.Clear();
        Assert.Same(last.ResultTask, behavior.FaultedAsync(messageFaultContext));
        Assert.Equal(
            Enumerable.Range(0, activityCount).Select(index => $"activity-{index}:fault-message"),
            calls);
        AssertInvocation(last, "fault-message", messageFaultContext, expectedTyped);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-terminal-constructor-null-boundary")]
    public void TerminalBehavior_ConstructorRejectsNullActivity(bool catchBehavior)
    {
        if (catchBehavior)
            AssertParam("activity", () => _ = new LastCatchBehavior<TestSaga>(null!));
        else
            AssertParam("activity", () => _ = new LastBehavior<TestSaga>(null!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-terminal-method-null-boundaries")]
    public void TerminalBehavior_RejectsEveryNullCollaboratorBeforeInvokingTheActivity(bool catchBehavior)
    {
        var activity = new RecordingActivity("activity", [], followContinuation: false);
        IBehavior<TestSaga> behavior = CreateTerminal(catchBehavior, activity);

        AssertParam("visitor", () => behavior.Accept(null!));
        AssertParam("context", () => behavior.Probe(null!));
        AssertParam("context", () =>
        {
            _ = behavior.ExecuteAsync((IBehaviorContext<TestSaga>)null!);
        });
        AssertParam("context", () =>
        {
            _ = behavior.ExecuteAsync<Message>(null!);
        });
        AssertParam("context", () =>
        {
            _ = behavior.FaultedAsync<InvalidOperationException>(null!);
        });
        AssertParam("context", () =>
        {
            _ = behavior.FaultedAsync<Message, InvalidOperationException>(null!);
        });

        Assert.Empty(activity.Invocations);
        Assert.Null(activity.AcceptedVisitor);
        Assert.Null(activity.ProbedContext);
        Assert.Equal(0, activity.AcceptCalls);
        Assert.Equal(0, activity.ProbeCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-terminal-visitor-probe-delegation")]
    public void TerminalBehavior_DelegatesVisitorAndProbeExactlyAndPreservesFailureIdentity(bool catchBehavior)
    {
        var activity = new RecordingActivity("activity", [], followContinuation: false);
        IBehavior<TestSaga> behavior = CreateTerminal(catchBehavior, activity);
        var visitor = new ContinuingVisitor();
        var probe = new PassiveProbeContext();

        behavior.Accept(visitor);
        behavior.Probe(probe);

        Assert.Same(visitor, activity.AcceptedVisitor);
        Assert.Same(probe, activity.ProbedContext);
        Assert.Equal(1, activity.AcceptCalls);
        Assert.Equal(1, activity.ProbeCalls);
        Assert.Equal(0, visitor.VisitCount);

        var acceptFailure = new MarkerException("accept");
        activity.AcceptFailure = acceptFailure;
        Assert.Same(acceptFailure, Assert.Throws<MarkerException>(() => behavior.Accept(visitor)));

        var probeFailure = new MarkerException("probe");
        activity.ProbeFailure = probeFailure;
        Assert.Same(probeFailure, Assert.Throws<MarkerException>(() => behavior.Probe(probe)));
        Assert.Equal(2, activity.AcceptCalls);
        Assert.Equal(2, activity.ProbeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-last-behavior-exact-terminal-policy")]
    public void LastBehavior_UsesEmptyForExecutionAndFaultedForFaultPropagationAcrossBothContextShapes()
    {
        var activity = new RecordingActivity("activity", [], followContinuation: false);
        IBehavior<TestSaga> behavior = new LastBehavior<TestSaga>(activity);
        IBehaviorContext<TestSaga> context = CreateProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> messageContext = CreateProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> faultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> messageFaultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();

        Assert.Same(activity.ResultTask, behavior.ExecuteAsync(context));
        AssertInvocation(activity, "execute", context, Behavior.Empty<TestSaga>());

        Assert.Same(activity.ResultTask, behavior.ExecuteAsync(messageContext));
        AssertInvocation(activity, "execute-message", messageContext, Behavior.Empty<TestSaga, Message>());

        Assert.Same(activity.ResultTask, behavior.FaultedAsync(faultContext));
        AssertInvocation(activity, "fault", faultContext, Behavior.Faulted<TestSaga>());

        Assert.Same(activity.ResultTask, behavior.FaultedAsync(messageFaultContext));
        AssertInvocation(activity, "fault-message", messageFaultContext, Behavior.Faulted<TestSaga, Message>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-ACTIVITY", "iteration-216-last-catch-behavior-exact-terminal-policy")]
    public void LastCatchBehavior_UsesEmptyForExecutionAndHandledFaultsAcrossBothContextShapes()
    {
        var activity = new RecordingActivity("activity", [], followContinuation: false);
        IBehavior<TestSaga> behavior = new LastCatchBehavior<TestSaga>(activity);
        IBehaviorContext<TestSaga> context = CreateProxy<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, Message> messageContext = CreateProxy<IBehaviorContext<TestSaga, Message>>();
        IBehaviorExceptionContext<TestSaga, InvalidOperationException> faultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException> messageFaultContext =
            CreateProxy<IBehaviorExceptionContext<TestSaga, Message, InvalidOperationException>>();

        Assert.Same(activity.ResultTask, behavior.ExecuteAsync(context));
        AssertInvocation(activity, "execute", context, Behavior.Empty<TestSaga>());

        Assert.Same(activity.ResultTask, behavior.ExecuteAsync(messageContext));
        AssertInvocation(activity, "execute-message", messageContext, Behavior.Empty<TestSaga, Message>());

        Assert.Same(activity.ResultTask, behavior.FaultedAsync(faultContext));
        AssertInvocation(activity, "fault", faultContext, Behavior.Empty<TestSaga>());

        Assert.Same(activity.ResultTask, behavior.FaultedAsync(messageFaultContext));
        AssertInvocation(activity, "fault-message", messageFaultContext, Behavior.Empty<TestSaga, Message>());
    }

    static void AssertInvocation(RecordingActivity activity, string operation, object context, object next)
    {
        Invocation invocation = Assert.Single(activity.Invocations);
        Assert.Equal(operation, invocation.Operation);
        Assert.Same(context, invocation.Context);
        Assert.Same(next, invocation.Next);
        activity.Invocations.Clear();
    }

    static void AssertParam(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    static BuilderHarness CreateBuilder(bool catchBuilder)
    {
        if (catchBuilder)
        {
            var builder = new CatchBehaviorBuilder<TestSaga>();
            return new BuilderHarness(builder, () => builder.Behavior);
        }

        var activityBuilder = new ActivityBehaviorBuilder<TestSaga>();
        return new BuilderHarness(activityBuilder, () => activityBuilder.Behavior);
    }

    static IBehavior<TestSaga> CreateTerminal(bool catchBehavior, RecordingActivity activity) =>
        catchBehavior
            ? new LastCatchBehavior<TestSaga>(activity)
            : new LastBehavior<TestSaga>(activity);

    static T CreateProxy<T>()
        where T : class => DispatchProxy.Create<T, PassiveDispatchProxy>();

    sealed record BuilderHarness(IBehaviorBuilder<TestSaga> Builder, Func<IBehavior<TestSaga>> GetBehavior);

    sealed record Invocation(string Operation, object Context, object Next);

    public sealed record Message;

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    sealed class RecordingActivity : IStateMachineActivity<TestSaga>
    {
        readonly bool _followContinuation;
        readonly List<string> _calls;
        readonly string _name;
        readonly TaskCompletionSource<bool> _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingActivity(string name, List<string> calls, bool followContinuation)
        {
            _name = name;
            _calls = calls;
            _followContinuation = followContinuation;
        }

        public IStateMachineVisitor? AcceptedVisitor { get; private set; }

        public int AcceptCalls { get; private set; }

        public MarkerException? AcceptFailure { get; set; }

        public List<Invocation> Invocations { get; } = [];

        public ProbeContext? ProbedContext { get; private set; }

        public int ProbeCalls { get; private set; }

        public MarkerException? ProbeFailure { get; set; }

        public Task ResultTask => _taskSource.Task;

        public void Accept(IStateMachineVisitor visitor)
        {
            AcceptCalls++;
            AcceptedVisitor = visitor;
            _calls.Add($"{_name}:accept");
            if (AcceptFailure is { } failure)
                throw failure;
        }

        public void Probe(ProbeContext context)
        {
            ProbeCalls++;
            ProbedContext = context;
            _calls.Add($"{_name}:probe");
            if (ProbeFailure is { } failure)
                throw failure;
        }

        public Task ExecuteAsync(IBehaviorContext<TestSaga> context, IBehavior<TestSaga> next)
        {
            _calls.Add($"{_name}:execute");
            Invocations.Add(new Invocation("execute", context, next));
            return _followContinuation ? next.ExecuteAsync(context) : ResultTask;
        }

        public Task ExecuteAsync<T>(IBehaviorContext<TestSaga, T> context, IBehavior<TestSaga, T> next)
            where T : class
        {
            _calls.Add($"{_name}:execute-message");
            Invocations.Add(new Invocation("execute-message", context, next));
            return _followContinuation ? next.ExecuteAsync(context) : ResultTask;
        }

        public Task FaultedAsync<TException>(
            IBehaviorExceptionContext<TestSaga, TException> context,
            IBehavior<TestSaga> next)
            where TException : Exception
        {
            _calls.Add($"{_name}:fault");
            Invocations.Add(new Invocation("fault", context, next));
            return _followContinuation ? next.FaultedAsync(context) : ResultTask;
        }

        public Task FaultedAsync<T, TException>(
            IBehaviorExceptionContext<TestSaga, T, TException> context,
            IBehavior<TestSaga, T> next)
            where T : class
            where TException : Exception
        {
            _calls.Add($"{_name}:fault-message");
            Invocations.Add(new Invocation("fault-message", context, next));
            return _followContinuation ? next.FaultedAsync(context) : ResultTask;
        }
    }

    sealed class ContinuingVisitor : IStateMachineVisitor
    {
        public int VisitCount { get; private set; }

        public void Visit(IState state, Action<IState> next)
        {
            VisitCount++;
            next(state);
        }

        public void Visit(IEvent @event, Action<IEvent> next)
        {
            VisitCount++;
            next(@event);
        }

        public void Visit<T>(IEvent<T> @event, Action<IEvent<T>> next)
            where T : class
        {
            VisitCount++;
            next(@event);
        }

        public void Visit(IStateMachineActivity activity) => VisitCount++;

        public void Visit(IStateMachineExceptionActivity activity, Action<IStateMachineExceptionActivity> next)
        {
            VisitCount++;
            next(activity);
        }

        public void Visit<T>(IBehavior<T> behavior)
            where T : class, ISagaStateMachineInstance => VisitCount++;

        public void Visit<T>(IBehavior<T> behavior, Action<IBehavior<T>> next)
            where T : class, ISagaStateMachineInstance
        {
            VisitCount++;
            next(behavior);
        }

        public void Visit<T, TData>(IBehavior<T, TData> behavior)
            where T : class, ISagaStateMachineInstance
            where TData : class => VisitCount++;

        public void Visit<T, TData>(IBehavior<T, TData> behavior, Action<IBehavior<T, TData>> next)
            where T : class, ISagaStateMachineInstance
            where TData : class
        {
            VisitCount++;
            next(behavior);
        }

        public void Visit(IStateMachineActivity activity, Action<IStateMachineActivity> next)
        {
            VisitCount++;
            next(activity);
        }
    }

    sealed class PassiveProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;

        public void Add(string key, string? value)
        {
        }

        public void Add(string key, object? value)
        {
        }

        public void Set(object values)
        {
        }

        public void Set(IEnumerable<KeyValuePair<string, object?>> values)
        {
        }

        public ProbeContext CreateScope(string key) => this;
    }

    public class PassiveDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            Type returnType = targetMethod.ReturnType;
            if (returnType == typeof(void))
                return null;
            if (returnType == typeof(Task))
                return Task.CompletedTask;
            if (returnType.IsValueType)
                return Activator.CreateInstance(returnType);

            return null;
        }
    }

    public sealed class MarkerException(string message) : Exception(message);
}
