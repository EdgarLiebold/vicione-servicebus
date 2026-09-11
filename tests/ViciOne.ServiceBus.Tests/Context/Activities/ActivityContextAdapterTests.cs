using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Activities;

public sealed class ActivityContextAdapterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EXECUTE-CONTEXT-ADAPTER", "all-result-factories-metadata-and-activity-binding")]
    public async Task ExecuteAdapters_ForwardEveryOperationAndBindProjectedArgumentsAsync()
    {
        ExecuteContext<ActivityArguments> source = CreateExecuteContext(out RecordingActivityContextProxy recording);
        var projectedArguments = new ActivityArguments("projected");
        ExecuteContext<ActivityArguments>[] adapters =
        [
            new ExecuteContextProxy<ActivityArguments>(source, projectedArguments),
            new ExecuteContextScope<ActivityArguments>(source, new LocalPayload("scope")),
        ];

        Assert.Same(projectedArguments, adapters[0].Arguments);
        Assert.Same(recording.Arguments, adapters[1].Arguments);
        foreach (ExecuteContext<ActivityArguments> adapter in adapters)
        {
            await AssertActivityMetadataAsync(adapter, recording);
            Assert.Same(recording.ExecutionResult, adapter.Result = recording.ExecutionResult);
            Assert.Same(recording.ExecutionResult, adapter.Result);
            await AssertEveryExecutionResultFactoryAsync(adapter, recording);

            var activity = new ExecuteActivity();
            ExecuteActivityContext<ExecuteActivity, ActivityArguments> host = adapter.CreateActivityContext(activity);
            Assert.Same(activity, host.Activity);
            Assert.Same(adapter.Arguments, host.Arguments);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COMPENSATE-CONTEXT-ADAPTER", "all-result-factories-metadata-and-activity-binding")]
    public async Task CompensateAdapters_ForwardEveryOperationAndBindProjectedLogsAsync()
    {
        CompensateContext<ActivityLog> source = CreateCompensateContext(out RecordingActivityContextProxy recording);
        var projectedLog = new ActivityLog("projected");
        CompensateContext<ActivityLog>[] adapters =
        [
            new CompensateContextProxy<ActivityLog>(source, projectedLog),
            new CompensateContextScope<ActivityLog>(source, new LocalPayload("scope")),
        ];

        Assert.Same(projectedLog, adapters[0].Log);
        Assert.Same(recording.Log, adapters[1].Log);
        foreach (CompensateContext<ActivityLog> adapter in adapters)
        {
            await AssertActivityMetadataAsync(adapter, recording);
            adapter.Result = recording.CompensationResult;
            Assert.Same(recording.CompensationResult, adapter.Result);

            Assert.Same(recording.CompensationResult, adapter.Compensated());
            Assert.Same(recording.CompensationResult, adapter.Compensated(new { Value = 1 }));
            Assert.Same(
                recording.CompensationResult,
                adapter.Compensated(new Dictionary<string, object> { ["value"] = 2 }));
            Assert.Same(recording.CompensationResult, adapter.Failed());
            Assert.Same(recording.CompensationResult, adapter.Failed(new InvalidOperationException("compensation failed")));

            var activity = new CompensateActivity();
            CompensateActivityContext<CompensateActivity, ActivityLog> host = adapter.CreateActivityContext(activity);
            Assert.Same(activity, host.Activity);
            Assert.Same(adapter.Log, host.Log);
        }

        Assert.Equal(14, recording.OperationNames.Count(name => name is
            "get_Result" or "set_Result" or "Compensated" or "Failed"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-CONTEXT-ADAPTER", "constructor-and-activity-input-validation")]
    public void Constructors_RejectMissingContextsMessagesAndActivities()
    {
        ExecuteContext<ActivityArguments> execute = CreateExecuteContext(out _);
        CompensateContext<ActivityLog> compensate = CreateCompensateContext(out _);

        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ExecuteContextProxy<ActivityArguments>(null!, new ActivityArguments("value"))).ParamName);
        Assert.Equal(
            "arguments",
            Assert.Throws<ArgumentNullException>(() => new ExecuteContextProxy<ActivityArguments>(execute, null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ExecuteContextScope<ActivityArguments>(null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new CompensateContextProxy<ActivityLog>(null!, new ActivityLog("value"))).ParamName);
        Assert.Equal(
            "log",
            Assert.Throws<ArgumentNullException>(() => new CompensateContextProxy<ActivityLog>(compensate, null!)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new CompensateContextScope<ActivityLog>(null!)).ParamName);
        Assert.Equal(
            "activity",
            Assert.Throws<ArgumentNullException>(() =>
                new ExecuteContextProxy<ActivityArguments>(execute, new ActivityArguments("value"))
                    .CreateActivityContext<ExecuteActivity>(null!)).ParamName);
        Assert.Equal(
            "activity",
            Assert.Throws<ArgumentNullException>(() =>
                ((CompensateContext<ActivityLog>)new CompensateContextProxy<ActivityLog>(compensate, new ActivityLog("value")))
                    .CreateActivityContext<CompensateActivity>(null!)).ParamName);
    }

    private static async Task AssertActivityMetadataAsync(ActivityContext context, RecordingActivityContextProxy recording)
    {
        Assert.Equal(recording.Timestamp, context.Timestamp);
        Assert.Equal(recording.Elapsed, context.Elapsed);
        Assert.Equal(recording.TrackingNumber, context.TrackingNumber);
        Assert.Equal(recording.ExecutionId, context.ExecutionId);
        Assert.Equal(recording.ActivityName, context.ActivityName);
        Assert.Same(recording.Variables, context.Variables);
        Assert.Same(
            recording.NotificationTask,
            context.NotifyActivityConsumedAsync(TimeSpan.FromSeconds(4), "activity-consumer", TestContext.Current.CancellationToken));
        await recording.NotificationTask;
    }

    private static async Task AssertEveryExecutionResultFactoryAsync(
        ExecuteContext<ActivityArguments> adapter,
        RecordingActivityContextProxy recording)
    {
        MethodInfo[] factories = typeof(ExecuteContext)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(static method => method.ReturnType == typeof(ExecutionResult))
            .OrderBy(static method => method.ToString(), StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(24, factories.Length);
        foreach (MethodInfo definition in factories)
        {
            MethodInfo factory = definition.IsGenericMethodDefinition
                ? definition.MakeGenericMethod(typeof(ActivityLog))
                : definition;
            object?[] arguments = CreateExecutionArguments(factory);
            recording.OperationNames.Clear();

            object? result = factory.Invoke(adapter, arguments);

            Assert.Same(recording.ExecutionResult, result);
            Assert.Equal(factory.Name, Assert.Single(recording.OperationNames));
        }

        await Task.CompletedTask;
    }

    private static object?[] CreateExecutionArguments(MethodInfo method) =>
        method.GetParameters().Select<ParameterInfo, object?>(static parameter =>
        {
            Type type = parameter.ParameterType;
            if (type == typeof(ActivityLog))
                return new ActivityLog("log");
            if (type == typeof(Exception))
                return new InvalidOperationException("execution failed");
            if (type == typeof(IEnumerable<KeyValuePair<string, object>>))
                return new Dictionary<string, object> { ["value"] = 1 };
            if (type == typeof(object))
                return new { Value = 2 };
            if (typeof(Delegate).IsAssignableFrom(type))
                return null;

            throw new InvalidOperationException($"No execute-adapter test value is defined for parameter type {type}.");
        }).ToArray();

    private static ExecuteContext<ActivityArguments> CreateExecuteContext(out RecordingActivityContextProxy recording)
    {
        ExecuteContext<ActivityArguments> context = DispatchProxy.Create<ExecuteContext<ActivityArguments>, RecordingActivityContextProxy>();
        recording = (RecordingActivityContextProxy)(object)context;
        recording.Arguments = new ActivityArguments("source");
        recording.ConfigureInfrastructure();
        return context;
    }

    private static CompensateContext<ActivityLog> CreateCompensateContext(out RecordingActivityContextProxy recording)
    {
        CompensateContext<ActivityLog> context = DispatchProxy.Create<CompensateContext<ActivityLog>, RecordingActivityContextProxy>();
        recording = (RecordingActivityContextProxy)(object)context;
        recording.Log = new ActivityLog("source");
        recording.ConfigureInfrastructure();
        return context;
    }

    private sealed record ActivityArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed record LocalPayload(string Value);

    private sealed class ExecuteActivity
    {
    }

    private sealed class CompensateActivity
    {
    }

    private class InfrastructureProxy : DispatchProxy
    {
        public IPublishEndpointProvider? PublishEndpointProvider { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_PublishEndpointProvider")
                return PublishEndpointProvider;

            throw new InvalidOperationException($"Activity infrastructure unexpectedly invoked {targetMethod?.Name}.");
        }
    }

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Activity test state unexpectedly invoked {targetMethod?.Name}.");
    }

    private class RecordingActivityContextProxy : DispatchProxy
    {
        public string ActivityName { get; } = "InventoryReservation";

        public ActivityArguments? Arguments { get; set; }

        public CompensationResult CompensationResult { get; } =
            DispatchProxy.Create<CompensationResult, UnexpectedInvocationProxy>();

        public TimeSpan Elapsed { get; } = TimeSpan.FromMilliseconds(240);

        public ExecutionResult ExecutionResult { get; } =
            DispatchProxy.Create<ExecutionResult, UnexpectedInvocationProxy>();

        public Guid ExecutionId { get; } = Guid.Parse("f6c980bc-4961-43a5-a7bd-09f0cc6c5cf9");

        public ActivityLog? Log { get; set; }

        public Task NotificationTask { get; } = Task.CompletedTask;

        public List<string> OperationNames { get; } = [];

        public ReceiveContext ReceiveContext { get; private set; } = null!;

        public object? Result { get; set; }

        public SerializerContext SerializerContext { get; private set; } = null!;

        public DateTimeOffset Timestamp { get; } = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

        public Guid TrackingNumber { get; } = Guid.Parse("f2045df2-685c-4125-a48c-31fa38a92847");

        public IReadOnlyDictionary<string, object> Variables { get; } =
            new Dictionary<string, object> { ["tenant"] = "north" };

        public void ConfigureInfrastructure()
        {
            IPublishEndpointProvider publishEndpointProvider =
                DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();
            ReceiveContext = DispatchProxy.Create<ReceiveContext, InfrastructureProxy>();
            ((InfrastructureProxy)(object)ReceiveContext).PublishEndpointProvider = publishEndpointProvider;
            SerializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The activity proxy supplied no method metadata.");
            OperationNames.Add(method.Name);
            return method.Name switch
            {
                "get_ActivityName" => ActivityName,
                "get_Arguments" => Arguments,
                "get_Elapsed" => Elapsed,
                "get_ExecutionId" => ExecutionId,
                "get_Log" => Log,
                "get_ReceiveContext" => ReceiveContext,
                "get_Result" => Result,
                "get_SerializerContext" => SerializerContext,
                "get_Timestamp" => Timestamp,
                "get_TrackingNumber" => TrackingNumber,
                "get_Variables" => Variables,
                "set_Result" => SetResult(args![0]),
                nameof(ActivityContext.NotifyActivityConsumedAsync) => NotificationTask,
                "Completed" or "CompletedWithVariables" or "Faulted" or "FaultedWithVariables" or "ReviseItinerary" or "Terminate" => ExecutionResult,
                "Compensated" or "Failed" => CompensationResult,
                _ => throw new InvalidOperationException($"The activity test has no behavior for {method.Name}."),
            };
        }

        private object? SetResult(object? result)
        {
            Result = result;
            return null;
        }
    }
}
