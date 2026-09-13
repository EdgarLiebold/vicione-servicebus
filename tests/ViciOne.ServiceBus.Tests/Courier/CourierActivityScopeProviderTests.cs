using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierActivityScopeProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "created-scopes-resolve-activity-and-own-lifetime")]
    public async Task CreatedScopes_ResolveBothActivityKindsAndOwnTheirLifetimeAsync()
    {
        var executeDependency = new TrackedScopedDependency();
        var compensateDependency = new TrackedScopedDependency();
        await using ServiceProvider executeServices = new ServiceCollection()
            .AddScoped<TestActivity>()
            .AddScoped(_ => executeDependency)
            .BuildServiceProvider(validateScopes: true);
        await using ServiceProvider compensateServices = new ServiceCollection()
            .AddScoped<TestActivity>()
            .AddScoped(_ => compensateDependency)
            .BuildServiceProvider(validateScopes: true);
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(executeServices, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(compensateServices, compensateSetter);
        ExecuteContext<TestArguments> executeContext = CreateExecuteContext(new TestArguments("execute"));
        CompensateContext<TestLog> compensateContext = CreateCompensateContext(new TestLog("compensate"));

        IExecuteActivityScopeContext<TestActivity, TestArguments> executeScope =
            await executeProvider.GetActivityScopeAsync(executeContext, TestContext.Current.CancellationToken);
        ICompensateActivityScopeContext<TestActivity, TestLog> compensateScope =
            await compensateProvider.GetActivityScopeAsync(compensateContext, TestContext.Current.CancellationToken);

        Assert.IsType<TestActivity>(executeScope.Context.Activity);
        Assert.IsType<TestActivity>(compensateScope.Context.Activity);
        Assert.Same(executeDependency, executeScope.GetService<TrackedScopedDependency>());
        Assert.Same(compensateDependency, compensateScope.GetService<TrackedScopedDependency>());
        Assert.Single(executeSetter.PushedScopes);
        Assert.Single(compensateSetter.PushedScopes);

        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);
        Assert.Equal(1, executeDependency.DisposeCount);
        Assert.Equal(1, compensateDependency.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "existing-scopes-are-borrowed-and-restored")]
    public async Task ExistingScopes_AreBorrowedAndOnlyRestoreTheAmbientContextAsync()
    {
        var executeDependency = new TrackedScopedDependency();
        var compensateDependency = new TrackedScopedDependency();
        await using ServiceProvider executeServices = new ServiceCollection()
            .AddScoped<TestActivity>()
            .AddScoped(_ => executeDependency)
            .BuildServiceProvider(validateScopes: true);
        await using ServiceProvider compensateServices = new ServiceCollection()
            .AddScoped<TestActivity>()
            .AddScoped(_ => compensateDependency)
            .BuildServiceProvider(validateScopes: true);
        AsyncServiceScope borrowedExecuteScope = executeServices.CreateAsyncScope();
        AsyncServiceScope borrowedCompensateScope = compensateServices.CreateAsyncScope();
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(executeServices, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(compensateServices, compensateSetter);
        ExecuteContext<TestArguments> executeContext = CreateExecuteContext(new TestArguments("execute"), borrowedExecuteScope);
        CompensateContext<TestLog> compensateContext = CreateCompensateContext(new TestLog("compensate"), borrowedCompensateScope);

        IExecuteActivityScopeContext<TestActivity, TestArguments> executeScope =
            await executeProvider.GetActivityScopeAsync(executeContext, TestContext.Current.CancellationToken);
        ICompensateActivityScopeContext<TestActivity, TestLog> compensateScope =
            await compensateProvider.GetActivityScopeAsync(compensateContext, TestContext.Current.CancellationToken);
        Assert.Same(executeDependency, executeScope.GetService<TrackedScopedDependency>());
        Assert.Same(compensateDependency, compensateScope.GetService<TrackedScopedDependency>());

        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);
        Assert.Equal(0, executeDependency.DisposeCount);
        Assert.Equal(0, compensateDependency.DisposeCount);

        await borrowedExecuteScope.DisposeAsync();
        await borrowedCompensateScope.DisposeAsync();
        Assert.Equal(1, executeDependency.DisposeCount);
        Assert.Equal(1, compensateDependency.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "scope-only-paths-preserve-typed-context")]
    public async Task ScopeOnlyPaths_PreserveTypedContextAndOwnOnlyNewScopesAsync()
    {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, compensateSetter);
        ExecuteContext<TestArguments> executeContext = CreateExecuteContext(new TestArguments("execute"));
        CompensateContext<TestLog> compensateContext = CreateCompensateContext(new TestLog("compensate"));

        IExecuteScopeContext<TestArguments> executeScope =
            await executeProvider.GetScopeAsync(executeContext, TestContext.Current.CancellationToken);
        ICompensateScopeContext<TestLog> compensateScope =
            await compensateProvider.GetScopeAsync(compensateContext, TestContext.Current.CancellationToken);

        Assert.Same(executeContext.Arguments, executeScope.Context.Arguments);
        Assert.Same(compensateContext.Log, compensateScope.Context.Log);
        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "missing-activity-resolution-fails-explicitly-and-restores-created-or-borrowed-scope")]
    public async Task MissingActivityResolution_FailsExplicitlyAndRestoresCreatedAndBorrowedScopesAsync()
    {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        AsyncServiceScope borrowedScope = services.CreateAsyncScope();
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, compensateSetter);

        ConsumerException createdException = await Assert.ThrowsAsync<ConsumerException>(async () =>
            await executeProvider.GetActivityScopeAsync(
                CreateExecuteContext(new TestArguments("value")),
                TestContext.Current.CancellationToken));
        ConsumerException borrowedException = await Assert.ThrowsAsync<ConsumerException>(async () =>
            await compensateProvider.GetActivityScopeAsync(
                CreateCompensateContext(new TestLog("value"), borrowedScope),
                TestContext.Current.CancellationToken));

        Assert.Contains(nameof(TestActivity), createdException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestActivity), borrowedException.Message, StringComparison.Ordinal);
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);
        await borrowedScope.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "scope-providers-reject-null-and-pre-canceled-contexts-before-resolution")]
    public async Task ScopeProviders_RejectNullAndPreCanceledContextsBeforeResolutionAsync()
    {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var setter = new RecordingScopedContextSetter();
        var execute = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, setter);
        var compensate = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, setter);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            execute.GetScopeAsync(null!, TestContext.Current.CancellationToken)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            execute.GetActivityScopeAsync(null!, TestContext.Current.CancellationToken)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            compensate.GetScopeAsync(null!, TestContext.Current.CancellationToken)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            compensate.GetActivityScopeAsync(null!, TestContext.Current.CancellationToken)).ParamName);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await execute.GetScopeAsync(
            CreateExecuteContext(new TestArguments("value")), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await compensate.GetActivityScopeAsync(
            CreateCompensateContext(new TestLog("value")), cancellation.Token));
        Assert.Empty(setter.PushedScopes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "scope-provider-constructor-boundaries")]
    public void ScopeProviders_RejectMissingInfrastructureDependencies()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var setter = new RecordingScopedContextSetter();

        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new ExecuteActivityScopeProvider<TestActivity, TestArguments>(null!, setter)).ParamName);
        Assert.Equal("setScopedConsumeContext", Assert.Throws<ArgumentNullException>(() =>
            new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, null!)).ParamName);
        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new CompensateActivityScopeProvider<TestActivity, TestLog>(null!, setter)).ParamName);
        Assert.Equal("setScopedConsumeContext", Assert.Throws<ArgumentNullException>(() =>
            new CompensateActivityScopeProvider<TestActivity, TestLog>(services, null!)).ParamName);
    }

    private static ExecuteContext<TestArguments> CreateExecuteContext(TestArguments arguments, IServiceScope? existingScope = null)
    {
        ConsumeContext<IRoutingSlip> transport = CreateTransportContext();
        TestExecuteContext context = DispatchProxy.Create<TestExecuteContext, ExecuteContextProxy>();
        var proxy = (ExecuteContextProxy)(object)context;
        proxy.Arguments = arguments;
        proxy.Transport = transport.Advanced();
        proxy.ExistingScope = existingScope;
        return context;
    }

    private static CompensateContext<TestLog> CreateCompensateContext(TestLog log, IServiceScope? existingScope = null)
    {
        ConsumeContext<IRoutingSlip> transport = CreateTransportContext();
        TestCompensateContext context = DispatchProxy.Create<TestCompensateContext, CompensateContextProxy>();
        var proxy = (CompensateContextProxy)(object)context;
        proxy.Log = log;
        proxy.Transport = transport.Advanced();
        proxy.ExistingScope = existingScope;
        return context;
    }

    private static ConsumeContext<IRoutingSlip> CreateTransportContext()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        return InMemoryOutboxTestContextFactory.Create(builder.Build());
    }

    private interface TestExecuteContext : ExecuteContext<TestArguments>, ConsumeContext;

    private interface TestCompensateContext : CompensateContext<TestLog>, ConsumeContext;

    private interface TestExecuteActivityContext<out TActivity> : ExecuteActivityContext<TActivity, TestArguments>
        where TActivity : class;

    private interface TestCompensateActivityContext<out TActivity> : CompensateActivityContext<TActivity, TestLog>
        where TActivity : class;

    private abstract class ActivityContextProxy : DispatchProxy
    {
        public IServiceScope? ExistingScope { get; set; }
        public ConsumeContext Transport { get; set; } = null!;

        protected bool TryInvokeShared(MethodInfo targetMethod, object?[]? args, out object? result)
        {
            switch (targetMethod.Name)
            {
                case "get_CancellationToken":
                    result = Transport.CancellationToken;
                    return true;
                case "get_Timestamp":
                    result = DateTimeOffset.UnixEpoch;
                    return true;
                case "get_Elapsed":
                    result = TimeSpan.Zero;
                    return true;
                case "get_TrackingNumber":
                case "get_ExecutionId":
                    result = NewId.NextGuid();
                    return true;
                case "get_ActivityName":
                    result = "TestActivity";
                    return true;
                case "get_Variables":
                    result = new Dictionary<string, object>();
                    return true;
                case "get_Result":
                    result = null;
                    return true;
                case "set_Result":
                    result = null;
                    return true;
                case "HasPayloadType" when ExistingScope is not null && (Type)args![0]! == typeof(IServiceScope):
                    result = true;
                    return true;
                case "TryGetPayload" when ExistingScope is not null
                    && targetMethod.GetGenericArguments()[0] == typeof(IServiceScope):
                    args![0] = ExistingScope;
                    result = true;
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        protected object? InvokeTransport(MethodInfo targetMethod, object?[]? args)
        {
            try
            {
                return targetMethod.Invoke(Transport, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }
    }

    private class ExecuteContextProxy : ActivityContextProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(ExecuteContextProxy)
            .GetMethod(nameof(CreateActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Arguments")
                return Arguments;
            if (targetMethod.Name == "CreateActivityContext")
            {
                return CreateContextMethod.MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                    .Invoke(null, [args![0]!, Arguments]);
            }
            if (TryInvokeShared(targetMethod, args, out object? result))
                return result;
            return InvokeTransport(targetMethod, args);
        }

        private static object CreateActivityContext<TActivity>(object activity, TestArguments arguments)
            where TActivity : class
        {
            TestExecuteActivityContext<TActivity> context =
                DispatchProxy.Create<TestExecuteActivityContext<TActivity>, ExecuteActivityContextProxy<TActivity>>();
            var proxy = (ExecuteActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = (TActivity)activity;
            proxy.Arguments = arguments;
            return context;
        }
    }

    private class CompensateContextProxy : ActivityContextProxy
    {
        private static readonly MethodInfo CreateContextMethod = typeof(CompensateContextProxy)
            .GetMethod(nameof(CreateActivityContext), BindingFlags.NonPublic | BindingFlags.Static)!;

        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Log")
                return Log;
            if (targetMethod.Name == "CreateActivityContext")
            {
                return CreateContextMethod.MakeGenericMethod(targetMethod.GetGenericArguments()[0])
                    .Invoke(null, [args![0]!, Log]);
            }
            if (TryInvokeShared(targetMethod, args, out object? result))
                return result;
            return InvokeTransport(targetMethod, args);
        }

        private static object CreateActivityContext<TActivity>(object activity, TestLog log)
            where TActivity : class
        {
            TestCompensateActivityContext<TActivity> context =
                DispatchProxy.Create<TestCompensateActivityContext<TActivity>, CompensateActivityContextProxy<TActivity>>();
            var proxy = (CompensateActivityContextProxy<TActivity>)(object)context;
            proxy.Activity = (TActivity)activity;
            proxy.Log = log;
            return context;
        }
    }

    private class ExecuteActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Arguments" => Arguments,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private class CompensateActivityContextProxy<TActivity> : DispatchProxy
        where TActivity : class
    {
        public TActivity Activity { get; set; } = null!;
        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Activity" => Activity,
            "get_Log" => Log,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    private sealed class RecordingScopedContextSetter : ISetScopedConsumeContext
    {
        public List<IServiceScope> PushedScopes { get; } = [];
        public int RestoreCount { get; private set; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            PushedScopes.Add(serviceProvider);
            return new CallbackDisposable(() => RestoreCount++);
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose() => callback();
    }

    private sealed class TrackedScopedDependency : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestActivity : IActivity<TestArguments, TestLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<TestLog> context) => throw new NotSupportedException();
    }

    private sealed record TestArguments(string Value);

    private sealed record TestLog(string Value);
}
