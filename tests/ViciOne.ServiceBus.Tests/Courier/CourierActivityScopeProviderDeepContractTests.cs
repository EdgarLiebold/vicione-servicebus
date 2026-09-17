using System.Collections.Concurrent;
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

public sealed class CourierActivityScopeProviderDeepContractTests
{
    [Theory]
    [InlineData(ScopeKind.Execute)]
    [InlineData(ScopeKind.Compensate)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-provider-api-async-token-shape")]
    public void ProviderInterfaces_ExposeConstrainedAsyncScopeOperations(ScopeKind kind)
    {
        Type providerType = kind == ScopeKind.Execute
            ? typeof(IExecuteActivityScopeProvider<,>)
            : typeof(ICompensateActivityScopeProvider<,>);
        Type activityContract = kind == ScopeKind.Execute
            ? typeof(IExecuteActivity<>)
            : typeof(ICompensateActivity<>);
        Type contextContract = kind == ScopeKind.Execute
            ? typeof(ExecuteContext<>)
            : typeof(CompensateContext<>);
        Type scopeContract = kind == ScopeKind.Execute
            ? typeof(IExecuteScopeContext<>)
            : typeof(ICompensateScopeContext<>);
        Type activityScopeContract = kind == ScopeKind.Execute
            ? typeof(IExecuteActivityScopeContext<,>)
            : typeof(ICompensateActivityScopeContext<,>);

        Type[] genericArguments = providerType.GetGenericArguments();
        Assert.Equal(2, genericArguments.Length);
        AssertInvariantReferenceType(genericArguments[0]);
        AssertInvariantReferenceType(genericArguments[1]);
        Type activityConstraint = Assert.Single(genericArguments[0].GetGenericParameterConstraints());
        Assert.Equal(activityContract, activityConstraint.GetGenericTypeDefinition());
        Assert.Same(genericArguments[1], Assert.Single(activityConstraint.GetGenericArguments()));
        Assert.Empty(genericArguments[1].GetGenericParameterConstraints());
        Assert.Contains(typeof(IProbeSite), providerType.GetInterfaces());

        MethodInfo[] methods = providerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Equal(["GetActivityScopeAsync", "GetScopeAsync"], methods.Select(x => x.Name).Order());
        Assert.All(methods, method =>
        {
            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(contextContract, parameters[0].ParameterType.GetGenericTypeDefinition());
            Assert.Same(genericArguments[1], Assert.Single(parameters[0].ParameterType.GetGenericArguments()));
            Assert.Equal("context", parameters[0].Name);
            Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
            Assert.Equal("cancellationToken", parameters[1].Name);
            Assert.True(parameters[1].IsOptional);
            Assert.Equal(typeof(ValueTask<>), method.ReturnType.GetGenericTypeDefinition());
        });

        Type scopeResult = Assert.Single(methods.Single(x => x.Name == "GetScopeAsync").ReturnType.GetGenericArguments());
        Assert.Equal(scopeContract, scopeResult.GetGenericTypeDefinition());
        Assert.Same(genericArguments[1], Assert.Single(scopeResult.GetGenericArguments()));
        Type activityScopeResult = Assert.Single(methods.Single(x => x.Name == "GetActivityScopeAsync").ReturnType.GetGenericArguments());
        Assert.Equal(activityScopeContract, activityScopeResult.GetGenericTypeDefinition());
        Assert.Equal(genericArguments, activityScopeResult.GetGenericArguments());
    }

    [Theory]
    [InlineData(ScopeKind.Execute)]
    [InlineData(ScopeKind.Compensate)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-scope-context-covariance-and-service-api")]
    public void ActivityScopeContexts_AreCovariantAsyncDisposableServiceBoundaries(ScopeKind kind)
    {
        Type scopeType = kind == ScopeKind.Execute
            ? typeof(IExecuteActivityScopeContext<,>)
            : typeof(ICompensateActivityScopeContext<,>);
        Type activityContract = kind == ScopeKind.Execute
            ? typeof(IExecuteActivity<>)
            : typeof(ICompensateActivity<>);
        Type contextContract = kind == ScopeKind.Execute
            ? typeof(ExecuteActivityContext<,>)
            : typeof(CompensateActivityContext<,>);

        Type[] genericArguments = scopeType.GetGenericArguments();
        Assert.Equal(2, genericArguments.Length);
        AssertCovariantReferenceType(genericArguments[0]);
        AssertCovariantReferenceType(genericArguments[1]);
        Type activityConstraint = Assert.Single(genericArguments[0].GetGenericParameterConstraints());
        Assert.Equal(activityContract, activityConstraint.GetGenericTypeDefinition());
        Assert.Same(genericArguments[1], Assert.Single(activityConstraint.GetGenericArguments()));
        Assert.Empty(genericArguments[1].GetGenericParameterConstraints());
        Assert.Contains(typeof(IAsyncDisposable), scopeType.GetInterfaces());

        PropertyInfo context = Assert.Single(scopeType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal("Context", context.Name);
        Assert.True(context.CanRead);
        Assert.False(context.CanWrite);
        Assert.Equal(contextContract, context.PropertyType.GetGenericTypeDefinition());
        Assert.Equal(genericArguments, context.PropertyType.GetGenericArguments());

        MethodInfo[] declaredMethods = scopeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.Equal(["GetService", "get_Context"], declaredMethods.Select(x => x.Name).Order(StringComparer.Ordinal));
        MethodInfo getService = Assert.Single(declaredMethods, x => !x.IsSpecialName);
        Assert.Equal("GetService", getService.Name);
        Assert.True(getService.IsGenericMethodDefinition);
        Assert.Empty(getService.GetParameters());
        Type serviceType = Assert.Single(getService.GetGenericArguments());
        Assert.Same(serviceType, getService.ReturnType);
        Assert.True(serviceType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
        Assert.Empty(serviceType.GetGenericParameterConstraints());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-registration-constructor-and-probe-boundaries")]
    public void RegistrationConstructors_RejectIncompatibleContextsAndProbeExactProviderMetadata()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ExecuteActivityScopeProvider<TestActivity, TestArguments>((IRegistrationContext)null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new CompensateActivityScopeProvider<TestActivity, TestLog>((IRegistrationContext)null!)).ParamName);

        RegistrationContextOnly incompatible = DispatchProxy.Create<RegistrationContextOnly, PassiveProxy>();
        Assert.Equal("context", Assert.Throws<ArgumentException>(() =>
            new ExecuteActivityScopeProvider<TestActivity, TestArguments>(incompatible)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentException>(() =>
            new CompensateActivityScopeProvider<TestActivity, TestLog>(incompatible)).ParamName);

        using ServiceProvider services = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var setter = new RecordingScopedContextSetter();
        var execute = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, setter);
        var compensate = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, setter);
        var probe = new RecordingProbeContext();

        execute.Probe(probe);
        compensate.Probe(probe);

        Assert.Equal(
            [("provider", (object?)"dependencyInjection"), ("provider", (object?)"dependencyInjection")],
            probe.Values);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => execute.Probe(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => compensate.Probe(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "deep-scope-provider-null-before-cancellation-all-paths")]
    public async Task ScopeProviders_ValidateContextBeforeCancellationAndPreserveTheExactTokenAsync()
    {
        var scopeFactory = new RecordingScopeFactory();
        var services = new ScopeFactoryServiceProvider(scopeFactory);
        var setter = new RecordingScopedContextSetter();
        var execute = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, setter);
        var compensate = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, setter);
        using var source = new CancellationTokenSource();
        source.Cancel();
        CancellationToken token = source.Token;

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => execute.GetScopeAsync(null!, token)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => execute.GetActivityScopeAsync(null!, token)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => compensate.GetScopeAsync(null!, token)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => compensate.GetActivityScopeAsync(null!, token)).ParamName);

        await AssertCanceledAsync(execute.GetScopeAsync(CreateExecuteContext(new TestArguments("execute")), token), token);
        await AssertCanceledAsync(execute.GetActivityScopeAsync(CreateExecuteContext(new TestArguments("activity")), token), token);
        await AssertCanceledAsync(compensate.GetScopeAsync(CreateCompensateContext(new TestLog("compensate")), token), token);
        await AssertCanceledAsync(compensate.GetActivityScopeAsync(CreateCompensateContext(new TestLog("activity")), token), token);
        Assert.Equal(0, scopeFactory.CreateCount);
        Assert.Equal(0, setter.PushCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-payload-selected-provider-identity-and-propagation")]
    public async Task PayloadServiceProvider_SelectsExactActivitiesAndPreservesPayloadsAndScopedOwnershipAsync()
    {
        var fallbackDependencies = new ConcurrentBag<TrackedDependency>();
        var selectedDependencies = new ConcurrentBag<TrackedDependency>();
        await using ServiceProvider fallback = CreateActivityServices("fallback", fallbackDependencies);
        await using ServiceProvider selected = CreateActivityServices("selected", selectedDependencies);
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(fallback, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(fallback, compensateSetter);
        var executeMarker = new Marker("execute-marker");
        var compensateMarker = new Marker("compensate-marker");
        var arguments = new TestArguments("execute-value");
        var log = new TestLog("compensate-value");

        IExecuteActivityScopeContext<TestActivity, TestArguments> executeScope = await executeProvider.GetActivityScopeAsync(
            CreateExecuteContext(arguments, serviceProvider: selected, marker: executeMarker),
            TestContext.Current.CancellationToken);
        ICompensateActivityScopeContext<TestActivity, TestLog> compensateScope = await compensateProvider.GetActivityScopeAsync(
            CreateCompensateContext(log, serviceProvider: selected, marker: compensateMarker),
            TestContext.Current.CancellationToken);

        Assert.Equal("selected", executeScope.Context.Activity.Dependency.Source);
        Assert.Equal("selected", compensateScope.Context.Activity.Dependency.Source);
        Assert.NotSame(executeScope.Context.Activity, compensateScope.Context.Activity);
        Assert.Same(arguments, executeScope.Context.Arguments);
        Assert.Same(log, compensateScope.Context.Log);
        Assert.Same(executeScope.Context.Activity.Dependency, executeScope.GetService<TrackedDependency>());
        Assert.Same(compensateScope.Context.Activity.Dependency, compensateScope.GetService<TrackedDependency>());
        Assert.True(executeScope.Context.TryGetPayload(out Marker? actualExecuteMarker));
        Assert.Same(executeMarker, actualExecuteMarker);
        Assert.True(compensateScope.Context.TryGetPayload(out Marker? actualCompensateMarker));
        Assert.Same(compensateMarker, actualCompensateMarker);
        Assert.True(executeScope.Context.TryGetPayload(out IServiceScope? executeServiceScope));
        Assert.Same(Assert.Single(executeSetter.PushedScopes), executeServiceScope);
        Assert.True(compensateScope.Context.TryGetPayload(out IServiceScope? compensateServiceScope));
        Assert.Same(Assert.Single(compensateSetter.PushedScopes), compensateServiceScope);
        Assert.Empty(fallbackDependencies);
        Assert.Equal(2, selectedDependencies.Count);

        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.All(selectedDependencies, dependency => Assert.Equal(1, dependency.DisposeCount));
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-existing-scope-only-paths-borrow-and-preserve-context")]
    public async Task ExistingScopeOnlyPaths_BorrowScopesAndPreserveTypedContextsAsync()
    {
        var executeScope = new RecordingScope(new EmptyServiceProvider());
        var compensateScope = new RecordingScope(new EmptyServiceProvider());
        var services = new ScopeFactoryServiceProvider(new RecordingScopeFactory());
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, compensateSetter);
        var arguments = new TestArguments("borrowed-execute");
        var log = new TestLog("borrowed-compensate");

        IExecuteScopeContext<TestArguments> execute = await executeProvider.GetScopeAsync(
            CreateExecuteContext(arguments, executeScope), TestContext.Current.CancellationToken);
        ICompensateScopeContext<TestLog> compensate = await compensateProvider.GetScopeAsync(
            CreateCompensateContext(log, compensateScope), TestContext.Current.CancellationToken);
        IExecuteScopeContext<TestArguments> repeatedExecute = await executeProvider.GetScopeAsync(
            CreateExecuteContext(arguments, executeScope), TestContext.Current.CancellationToken);
        ICompensateScopeContext<TestLog> repeatedCompensate = await compensateProvider.GetScopeAsync(
            CreateCompensateContext(log, compensateScope), TestContext.Current.CancellationToken);

        Assert.Same(arguments, execute.Context.Arguments);
        Assert.Same(log, compensate.Context.Log);
        await execute.DisposeAsync();
        await compensate.DisposeAsync();
        await repeatedExecute.DisposeAsync();
        await repeatedCompensate.DisposeAsync();
        Assert.Equal(0, executeScope.DisposeAsyncCount);
        Assert.Equal(0, compensateScope.DisposeAsyncCount);
        Assert.Equal(2, executeSetter.RestoreCount);
        Assert.Equal(2, compensateSetter.RestoreCount);

        await executeScope.DisposeAsync();
        await compensateScope.DisposeAsync();
        Assert.Equal(1, executeScope.DisposeAsyncCount);
        Assert.Equal(1, compensateScope.DisposeAsyncCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-resolution-failure-created-versus-borrowed-ownership")]
    public async Task ActivityResolutionFailure_ReleasesCreatedScopeButLeavesBorrowedScopeOwnedByCallerAsync()
    {
        var dependencies = new List<TrackedDependency>();
        var failure = new ActivityResolutionException("activity factory failed");
        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            var dependency = new TrackedDependency($"dependency-{dependencies.Count}");
            dependencies.Add(dependency);
            return dependency;
        });
        services.AddScoped<TestActivity>(provider =>
        {
            _ = provider.GetRequiredService<TrackedDependency>();
            throw failure;
        });
        await using ServiceProvider root = services.BuildServiceProvider(validateScopes: true);
        var executeSetter = new RecordingScopedContextSetter();
        var compensateSetter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(root, executeSetter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(root, compensateSetter);

        ActivityResolutionException createdExecuteFailure = await Assert.ThrowsAsync<ActivityResolutionException>(async () =>
            await executeProvider.GetActivityScopeAsync(
                CreateExecuteContext(new TestArguments("created")),
                TestContext.Current.CancellationToken));
        ActivityResolutionException createdCompensateFailure = await Assert.ThrowsAsync<ActivityResolutionException>(async () =>
            await compensateProvider.GetActivityScopeAsync(
                CreateCompensateContext(new TestLog("created")),
                TestContext.Current.CancellationToken));
        Assert.Same(failure, createdExecuteFailure);
        Assert.Same(failure, createdCompensateFailure);
        Assert.Equal(2, dependencies.Count);
        Assert.Equal(1, dependencies[0].DisposeCount);
        Assert.Equal(1, dependencies[1].DisposeCount);
        Assert.Equal(1, executeSetter.RestoreCount);
        Assert.Equal(1, compensateSetter.RestoreCount);

        AsyncServiceScope borrowedExecuteScope = root.CreateAsyncScope();
        AsyncServiceScope borrowedCompensateScope = root.CreateAsyncScope();
        ActivityResolutionException borrowedExecuteFailure = await Assert.ThrowsAsync<ActivityResolutionException>(async () =>
            await executeProvider.GetActivityScopeAsync(
                CreateExecuteContext(new TestArguments("borrowed"), borrowedExecuteScope),
                TestContext.Current.CancellationToken));
        ActivityResolutionException borrowedCompensateFailure = await Assert.ThrowsAsync<ActivityResolutionException>(async () =>
            await compensateProvider.GetActivityScopeAsync(
                CreateCompensateContext(new TestLog("borrowed"), borrowedCompensateScope),
                TestContext.Current.CancellationToken));
        Assert.Same(failure, borrowedExecuteFailure);
        Assert.Same(failure, borrowedCompensateFailure);
        Assert.Equal(4, dependencies.Count);
        Assert.Equal(0, dependencies[2].DisposeCount);
        Assert.Equal(0, dependencies[3].DisposeCount);
        Assert.Equal(2, executeSetter.RestoreCount);
        Assert.Equal(2, compensateSetter.RestoreCount);

        await borrowedExecuteScope.DisposeAsync();
        await borrowedCompensateScope.DisposeAsync();
        Assert.Equal(1, dependencies[2].DisposeCount);
        Assert.Equal(1, dependencies[3].DisposeCount);

        await using ServiceProvider emptyRoot = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var missingExecuteSetter = new RecordingScopedContextSetter();
        var missingCompensateSetter = new RecordingScopedContextSetter();
        var missingExecute = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(emptyRoot, missingExecuteSetter);
        var missingCompensate = new CompensateActivityScopeProvider<TestActivity, TestLog>(emptyRoot, missingCompensateSetter);
        AsyncServiceScope missingExecuteScope = emptyRoot.CreateAsyncScope();

        ConsumerException missingExistingExecute = await Assert.ThrowsAsync<ConsumerException>(async () =>
            await missingExecute.GetActivityScopeAsync(
                CreateExecuteContext(new TestArguments("missing"), missingExecuteScope),
                TestContext.Current.CancellationToken));
        ConsumerException missingCreatedCompensate = await Assert.ThrowsAsync<ConsumerException>(async () =>
            await missingCompensate.GetActivityScopeAsync(
                CreateCompensateContext(new TestLog("missing")),
                TestContext.Current.CancellationToken));

        Assert.Contains(nameof(TestActivity), missingExistingExecute.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TestActivity), missingCreatedCompensate.Message, StringComparison.Ordinal);
        Assert.Equal(1, missingExecuteSetter.RestoreCount);
        Assert.Equal(1, missingCompensateSetter.RestoreCount);
        await missingExecuteScope.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-context-install-failure-created-versus-borrowed-ownership")]
    public async Task ContextInstallationFailure_ReleasesCreatedScopeButNeverBorrowedScopeAsync()
    {
        var scopeFactory = new RecordingScopeFactory();
        var services = new ScopeFactoryServiceProvider(scopeFactory);
        var createdFailure = new ContextInstallationException("created install failed");
        var createdSetter = new ThrowingScopedContextSetter(createdFailure);
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, createdSetter);

        ContextInstallationException actualCreated = await Assert.ThrowsAsync<ContextInstallationException>(async () =>
            await executeProvider.GetScopeAsync(
                CreateExecuteContext(new TestArguments("created")),
                TestContext.Current.CancellationToken));

        Assert.Same(createdFailure, actualCreated);
        RecordingScope createdScope = Assert.Single(scopeFactory.Scopes);
        Assert.Equal(1, createdScope.DisposeAsyncCount);
        Assert.Equal(1, createdSetter.PushCount);

        var borrowedScope = new RecordingScope(new EmptyServiceProvider());
        var borrowedFailure = new ContextInstallationException("borrowed install failed");
        var borrowedSetter = new ThrowingScopedContextSetter(borrowedFailure);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, borrowedSetter);
        ContextInstallationException actualBorrowed = await Assert.ThrowsAsync<ContextInstallationException>(async () =>
            await compensateProvider.GetScopeAsync(
                CreateCompensateContext(new TestLog("borrowed"), borrowedScope),
                TestContext.Current.CancellationToken));

        Assert.Same(borrowedFailure, actualBorrowed);
        Assert.Equal(0, borrowedScope.DisposeAsyncCount);
        Assert.Equal(1, borrowedSetter.PushCount);
        Assert.Equal(1, scopeFactory.CreateCount);
        await borrowedScope.DisposeAsync();
        Assert.Equal(1, borrowedScope.DisposeAsyncCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITY-SCOPE", "deep-concurrent-scope-isolation-and-release")]
    public async Task ConcurrentActivityScopes_IsolateInstancesAndReleaseEveryScopeExactlyOnceAsync()
    {
        var dependencies = new ConcurrentBag<TrackedDependency>();
        await using ServiceProvider services = CreateActivityServices("concurrent", dependencies);
        var setter = new RecordingScopedContextSetter();
        var executeProvider = new ExecuteActivityScopeProvider<TestActivity, TestArguments>(services, setter);
        var compensateProvider = new CompensateActivityScopeProvider<TestActivity, TestLog>(services, setter);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Task<ScopeObservation>[] operations = Enumerable.Range(0, 8)
            .Select(index => ObserveExecuteScopeAsync(executeProvider, index, cancellationToken))
            .Concat(Enumerable.Range(0, 8)
                .Select(index => ObserveCompensateScopeAsync(compensateProvider, index, cancellationToken)))
            .ToArray();
        ScopeObservation[] observations = await Task.WhenAll(operations);

        Assert.Equal(16, observations.Length);
        Assert.Equal(16, observations.Select(x => x.Activity).Distinct().Count());
        Assert.Equal(16, observations.Select(x => x.Dependency).Distinct().Count());
        Assert.Equal(16, observations.Select(x => x.Scope).Distinct().Count());
        Assert.Equal(16, setter.PushCount);
        Assert.Equal(8, observations.Count(x => x.Value.StartsWith("execute-", StringComparison.Ordinal)));
        Assert.Equal(8, observations.Count(x => x.Value.StartsWith("compensate-", StringComparison.Ordinal)));
        Assert.All(observations, observation => Assert.Same(observation.Activity.Dependency, observation.Dependency));

        await Task.WhenAll(observations.Select(x => x.Lifetime.DisposeAsync().AsTask()));

        Assert.Equal(16, setter.RestoreCount);
        Assert.Equal(16, dependencies.Count);
        Assert.All(dependencies, dependency => Assert.Equal(1, dependency.DisposeCount));
    }

    private static async Task<ScopeObservation> ObserveExecuteScopeAsync(
        ExecuteActivityScopeProvider<TestActivity, TestArguments> provider,
        int index,
        CancellationToken cancellationToken)
    {
        var arguments = new TestArguments($"execute-{index}");
        IExecuteActivityScopeContext<TestActivity, TestArguments> scope = await provider.GetActivityScopeAsync(
            CreateExecuteContext(arguments), cancellationToken);
        Assert.Same(arguments, scope.Context.Arguments);
        Assert.True(scope.Context.TryGetPayload(out IServiceScope? serviceScope));
        return new ScopeObservation(scope, scope.Context.Activity, scope.GetService<TrackedDependency>(), serviceScope!, arguments.Value);
    }

    private static async Task<ScopeObservation> ObserveCompensateScopeAsync(
        CompensateActivityScopeProvider<TestActivity, TestLog> provider,
        int index,
        CancellationToken cancellationToken)
    {
        var log = new TestLog($"compensate-{index}");
        ICompensateActivityScopeContext<TestActivity, TestLog> scope = await provider.GetActivityScopeAsync(
            CreateCompensateContext(log), cancellationToken);
        Assert.Same(log, scope.Context.Log);
        Assert.True(scope.Context.TryGetPayload(out IServiceScope? serviceScope));
        return new ScopeObservation(scope, scope.Context.Activity, scope.GetService<TrackedDependency>(), serviceScope!, log.Value);
    }

    private static ServiceProvider CreateActivityServices(string source, ConcurrentBag<TrackedDependency> dependencies)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ =>
        {
            var dependency = new TrackedDependency(source);
            dependencies.Add(dependency);
            return dependency;
        });
        services.AddScoped<TestActivity>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ExecuteContext<TestArguments> CreateExecuteContext(
        TestArguments arguments,
        IServiceScope? existingScope = null,
        IServiceProvider? serviceProvider = null,
        Marker? marker = null)
    {
        TestExecuteContext context = DispatchProxy.Create<TestExecuteContext, ExecuteContextProxy>();
        var proxy = (ExecuteContextProxy)(object)context;
        proxy.Arguments = arguments;
        proxy.Transport = CreateTransportContext().Advanced();
        proxy.AddPayload(existingScope);
        proxy.AddPayload(serviceProvider);
        proxy.AddPayload(marker);
        return context;
    }

    private static CompensateContext<TestLog> CreateCompensateContext(
        TestLog log,
        IServiceScope? existingScope = null,
        IServiceProvider? serviceProvider = null,
        Marker? marker = null)
    {
        TestCompensateContext context = DispatchProxy.Create<TestCompensateContext, CompensateContextProxy>();
        var proxy = (CompensateContextProxy)(object)context;
        proxy.Log = log;
        proxy.Transport = CreateTransportContext().Advanced();
        proxy.AddPayload(existingScope);
        proxy.AddPayload(serviceProvider);
        proxy.AddPayload(marker);
        return context;
    }

    private static ConsumeContext<IRoutingSlip> CreateTransportContext()
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());
        return InMemoryOutboxTestContextFactory.Create(builder.Build());
    }

    private static void AssertInvariantReferenceType(Type parameter)
    {
        Assert.Equal(GenericParameterAttributes.None,
            parameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
    }

    private static void AssertCovariantReferenceType(Type parameter)
    {
        Assert.Equal(GenericParameterAttributes.Covariant,
            parameter.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        Assert.True(parameter.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
    }

    private static async Task AssertCanceledAsync<T>(ValueTask<T> task, CancellationToken expectedToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Equal(expectedToken, exception.CancellationToken);
    }

    public enum ScopeKind
    {
        Execute,
        Compensate,
    }

    private interface TestExecuteContext : ExecuteContext<TestArguments>, ConsumeContext;

    private interface TestCompensateContext : CompensateContext<TestLog>, ConsumeContext;

    private interface RegistrationContextOnly : IRegistrationContext;

    private abstract class ActivityContextProxy : DispatchProxy
    {
        readonly Dictionary<Type, object> _payloads = [];

        public ConsumeContext Transport { get; set; } = null!;
        public Guid TrackingNumber { get; } = NewId.NextGuid();
        public Guid ExecutionId { get; } = NewId.NextGuid();

        public void AddPayload<T>(T? payload) where T : class
        {
            if (payload is not null)
                _payloads[typeof(T)] = payload;
        }

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
                    result = TrackingNumber;
                    return true;
                case "get_ExecutionId":
                    result = ExecutionId;
                    return true;
                case "get_ActivityName":
                    result = nameof(TestActivity);
                    return true;
                case "get_Variables":
                    result = new Dictionary<string, object>();
                    return true;
                case "get_Result":
                case "set_Result":
                    result = null;
                    return true;
                case "HasPayloadType":
                    result = args![0] is Type payloadType && _payloads.ContainsKey(payloadType);
                    return true;
                case "TryGetPayload":
                    Type requestedType = targetMethod.GetGenericArguments()[0];
                    bool found = _payloads.TryGetValue(requestedType, out object? payload);
                    args![0] = payload;
                    result = found;
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
        public TestArguments Arguments { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Arguments")
                return Arguments;
            if (TryInvokeShared(targetMethod, args, out object? result))
                return result;
            return InvokeTransport(targetMethod, args);
        }
    }

    private class CompensateContextProxy : ActivityContextProxy
    {
        public TestLog Log { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == "get_Log")
                return Log;
            if (TryInvokeShared(targetMethod, args, out object? result))
                return result;
            return InvokeTransport(targetMethod, args);
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected member: {targetMethod?.Name}");
    }

    private sealed class RecordingProbeContext : ProbeContext
    {
        public CancellationToken CancellationToken => default;
        public List<(string Key, object? Value)> Values { get; } = [];

        public void Add(string key, string? value) => Values.Add((key, value));
        public void Add(string key, object? value) => Values.Add((key, value));
        public void Set(object values) => throw new NotSupportedException();
        public void Set(IEnumerable<KeyValuePair<string, object?>> values) => throw new NotSupportedException();
        public ProbeContext CreateScope(string key) => throw new NotSupportedException();
    }

    private sealed class RecordingScopedContextSetter : ISetScopedConsumeContext
    {
        readonly ConcurrentQueue<IServiceScope> _pushedScopes = new();
        int _restoreCount;

        public IReadOnlyCollection<IServiceScope> PushedScopes => _pushedScopes.ToArray();
        public int PushCount => _pushedScopes.Count;
        public int RestoreCount => Volatile.Read(ref _restoreCount);

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            _pushedScopes.Enqueue(serviceProvider);
            return new CallbackDisposable(() => Interlocked.Increment(ref _restoreCount));
        }
    }

    private sealed class ThrowingScopedContextSetter(Exception failure) : ISetScopedConsumeContext
    {
        int _pushCount;

        public int PushCount => Volatile.Read(ref _pushCount);

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            Interlocked.Increment(ref _pushCount);
            throw failure;
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose() => callback();
    }

    private sealed class ScopeFactoryServiceProvider(RecordingScopeFactory scopeFactory) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IServiceScopeFactory) ? scopeFactory : null;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingScopeFactory : IServiceScopeFactory
    {
        readonly ConcurrentQueue<RecordingScope> _scopes = new();

        public int CreateCount => _scopes.Count;
        public IReadOnlyCollection<RecordingScope> Scopes => _scopes.ToArray();

        public IServiceScope CreateScope()
        {
            var scope = new RecordingScope(new EmptyServiceProvider());
            _scopes.Enqueue(scope);
            return scope;
        }
    }

    private sealed class RecordingScope(IServiceProvider serviceProvider) : IServiceScope, IAsyncDisposable
    {
        int _disposeAsyncCount;

        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public int DisposeAsyncCount => Volatile.Read(ref _disposeAsyncCount);

        public void Dispose() => throw new InvalidOperationException("The asynchronous disposal path must be used.");

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeAsyncCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TrackedDependency(string source) : IAsyncDisposable
    {
        int _disposeCount;

        public string Source { get; } = source;
        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestActivity(TrackedDependency dependency) : IActivity<TestArguments, TestLog>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TrackedDependency Dependency { get; } = dependency;

        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<TestArguments> context) => throw new NotSupportedException();
        public Task<CompensationResult> CompensateAsync(CompensateContext<TestLog> context) => throw new NotSupportedException();
    }

    private sealed record ScopeObservation(
        IAsyncDisposable Lifetime,
        TestActivity Activity,
        TrackedDependency Dependency,
        IServiceScope Scope,
        string Value);

    private sealed record TestArguments(string Value);
    private sealed record TestLog(string Value);
    private sealed record Marker(string Value);
    private sealed class ActivityResolutionException(string message) : Exception(message);
    private sealed class ContextInstallationException(string message) : Exception(message);
}
