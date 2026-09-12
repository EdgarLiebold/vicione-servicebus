using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierConsumerKindContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "consumer-kinds-declare-stable-planning-order")]
    public void ConsumerKinds_DeclareDistinctNonFallbackCategories()
    {
        var activity = new ActivityConsumerKind();
        var executeOnly = new ExecuteActivityConsumerKind();

        Assert.Equal("Activity", activity.Name);
        Assert.Equal(20, activity.Order);
        Assert.False(activity.IsFallback);
        Assert.Equal("ExecuteActivity", executeOnly.Name);
        Assert.Equal(30, executeOnly.Order);
        Assert.False(executeOnly.IsFallback);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-DISPATCH", "consumer-kind-routes-each-activity-shape-to-its-execute-endpoint")]
    public void ConsumerKinds_CreateDispatchersOnlyForTheirOwnedActivityShape()
    {
        var factory = new RecordingDispatcherFactory();
        IEndpointNameFormatter formatter = KebabCaseEndpointNameFormatter.Instance;
        var activity = new ActivityConsumerKind();
        var executeOnly = new ExecuteActivityConsumerKind();

        Assert.True(activity.TryCreateDispatcher(typeof(CompensatableActivity), factory, formatter,
            out IReceiveEndpointDispatcher? compensatableDispatcher));
        Assert.Same(factory.Dispatcher, compensatableDispatcher);
        Assert.Equal(formatter.ExecuteActivity<CompensatableActivity, ActivityArguments>(), factory.QueueName);
        Assert.NotNull(factory.Configure);

        factory.Reset();

        Assert.True(executeOnly.TryCreateDispatcher(typeof(ExecuteOnlyActivity), factory, formatter,
            out IReceiveEndpointDispatcher? executeDispatcher));
        Assert.Same(factory.Dispatcher, executeDispatcher);
        Assert.Equal(formatter.ExecuteActivity<ExecuteOnlyActivity, ActivityArguments>(), factory.QueueName);
        Assert.NotNull(factory.Configure);

        Assert.False(activity.TryCreateDispatcher(typeof(ExecuteOnlyActivity), factory, formatter,
            out IReceiveEndpointDispatcher? unsupportedActivity));
        Assert.Null(unsupportedActivity);
        Assert.False(executeOnly.TryCreateDispatcher(typeof(CompensatableActivity), factory, formatter,
            out IReceiveEndpointDispatcher? unsupportedExecute));
        Assert.Null(unsupportedExecute);
        InvalidOperationException ambiguous = Assert.Throws<InvalidOperationException>(() =>
            executeOnly.TryCreateDispatcher(typeof(AmbiguousExecuteActivity), factory, formatter, out _));
        Assert.Contains(nameof(AmbiguousExecuteActivity), ambiguous.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IExecuteActivity<object>).Split('`')[0], ambiguous.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-DISPATCH", "dispatcher-construction-rejects-missing-collaborators")]
    public void DispatcherCreation_RejectsMissingFactoryAndFormatter()
    {
        var activity = new ActivityConsumerKind();
        var executeOnly = new ExecuteActivityConsumerKind();
        var factory = new RecordingDispatcherFactory();
        IEndpointNameFormatter formatter = KebabCaseEndpointNameFormatter.Instance;

        Assert.Equal("registrationType", Assert.Throws<ArgumentNullException>(() =>
            activity.TryCreateDispatcher(null!, factory, formatter, out _)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            activity.TryCreateDispatcher(typeof(CompensatableActivity), null!, formatter, out _)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            executeOnly.TryCreateDispatcher(typeof(ExecuteOnlyActivity), factory, null!, out _)).ParamName);

        var dispatcherFactory = new ExecuteActivityReceiveEndpointDispatcher<ExecuteOnlyActivity, ActivityArguments>();
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            dispatcherFactory.Create(null!, formatter)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            dispatcherFactory.Create(factory, null!)).ParamName);
    }

    private sealed record ActivityArguments(string Value);

    private sealed record AlternativeArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed class CompensatableActivity : IActivity<ActivityArguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            throw new NotSupportedException();
    }

    private sealed class ExecuteOnlyActivity : IExecuteActivity<ActivityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            throw new NotSupportedException();
    }

    private sealed class AmbiguousExecuteActivity :
        IExecuteActivity<ActivityArguments>,
        IExecuteActivity<AlternativeArguments>
    {
        Task<ExecutionResult> IExecuteActivity<ActivityArguments>.ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            throw new NotSupportedException();

        Task<ExecutionResult> IExecuteActivity<AlternativeArguments>.ExecuteAsync(ExecuteContext<AlternativeArguments> context) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingDispatcherFactory : IReceiveEndpointDispatcherFactory
    {
        public RecordingDispatcherFactory()
        {
            Dispatcher = DispatchProxy.Create<IReceiveEndpointDispatcher, DispatcherProxy>();
        }

        public Action<IReceiveEndpointConfigurator, IRegistrationContext>? Configure { get; private set; }

        public IReceiveEndpointDispatcher Dispatcher { get; }

        public string? QueueName { get; private set; }

        public IReceiveEndpointDispatcher CreateReceiver(string queueName) =>
            throw new NotSupportedException();

        public IReceiveEndpointDispatcher CreateReceiver(string queueName,
            Action<IReceiveEndpointConfigurator, IRegistrationContext> configure)
        {
            QueueName = queueName;
            Configure = configure;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateRegistrationReceiver(Type registrationType, string fallbackQueueName,
            IEndpointNameFormatter formatter) => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Reset()
        {
            QueueName = null;
            Configure = null;
        }
    }

    private class DispatcherProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected dispatcher member: {targetMethod?.Name}");
    }
}
