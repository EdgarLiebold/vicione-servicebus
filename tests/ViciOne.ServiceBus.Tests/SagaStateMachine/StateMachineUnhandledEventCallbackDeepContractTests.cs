using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineUnhandledEventCallbackDeepContractTests
{
    const BindingFlags DeclaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "iteration-217-state-machine-unhandled-callback-exact-public-contract")]
    public void PublicContract_DeclaresExactSignatureConstraintAndNullability()
    {
        Type definition = typeof(StateMachineUnhandledEventCallback<>);

        Assert.True(definition.IsPublic);
        Assert.True(definition.IsSealed);
        Assert.True(definition.IsGenericTypeDefinition);
        Assert.Equal(typeof(MulticastDelegate), definition.BaseType);
        Assert.Equal("ViciOne.ServiceBus.SagaStateMachine", definition.Namespace);
        Assert.Equal("ViciOne.ServiceBus.Sagas", definition.Assembly.GetName().Name);
        Assert.Empty(definition.GetFields(DeclaredPublic));
        Assert.Empty(definition.GetProperties(DeclaredPublic));
        Assert.Empty(definition.GetEvents(DeclaredPublic));

        ConstructorInfo constructor = Assert.Single(definition.GetConstructors(DeclaredPublic));
        Assert.Equal([typeof(object), typeof(nint)], constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(["object", "method"], constructor.GetParameters().Select(parameter => parameter.Name));

        Type sagaParameter = Assert.Single(definition.GetGenericArguments());
        Assert.Equal("TSaga", sagaParameter.Name);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            sagaParameter.GenericParameterAttributes
            & (GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask));
        Assert.Equal([typeof(ISagaStateMachineInstance)], sagaParameter.GetGenericParameterConstraints());

        MethodInfo[] methods = definition.GetMethods(DeclaredPublic);
        Assert.Equal(["BeginInvoke", "EndInvoke", "Invoke"], methods.Select(method => method.Name).Order());
        MethodInfo invoke = Assert.Single(methods, method => method.Name == "Invoke");
        Assert.True(invoke.IsPublic);
        Assert.False(invoke.IsStatic);
        Assert.True(invoke.IsVirtual);
        Assert.Equal(typeof(Task), invoke.ReturnType);

        ParameterInfo[] parameters = invoke.GetParameters();
        Assert.Equal(["context", "state"], parameters.Select(parameter => parameter.Name));
        Assert.Equal(2, parameters.Length);
        Assert.True(parameters[0].ParameterType.IsGenericType);
        Assert.Equal(typeof(IBehaviorContext<>), parameters[0].ParameterType.GetGenericTypeDefinition());
        Assert.Same(sagaParameter, Assert.Single(parameters[0].ParameterType.GetGenericArguments()));
        Assert.Equal(typeof(IState), parameters[1].ParameterType);
        Assert.All(parameters, parameter =>
        {
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            Assert.False(parameter.ParameterType.IsByRef);
            Assert.False(parameter.IsOut);
        });

        var nullability = new NullabilityInfoContext();
        Assert.Equal(NullabilityState.NotNull, nullability.Create(invoke.ReturnParameter).ReadState);
        Assert.All(parameters, parameter => Assert.Equal(NullabilityState.NotNull, nullability.Create(parameter).ReadState));

        AssertRuntimeMethod(
            methods,
            "BeginInvoke",
            typeof(IAsyncResult),
            [parameters[0].ParameterType, typeof(IState), typeof(AsyncCallback), typeof(object)],
            ["context", "state", "callback", "object"]);
        AssertRuntimeMethod(methods, "EndInvoke", typeof(Task), [typeof(IAsyncResult)], ["result"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "iteration-217-state-machine-unhandled-callback-argument-and-task-identity")]
    public async Task Invocation_PreservesContextStateAndTaskIdentityAsync()
    {
        IBehaviorContext<CallbackSaga> context = CreateProxy<IBehaviorContext<CallbackSaga>>();
        IState state = CreateProxy<IState>();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object? observedContext = null;
        object? observedState = null;
        var calls = 0;
        StateMachineUnhandledEventCallback<CallbackSaga> callback = (callbackContext, callbackState) =>
        {
            calls++;
            observedContext = callbackContext;
            observedState = callbackState;
            return completion.Task;
        };

        Task returned = callback(context, state);

        Assert.Same(completion.Task, returned);
        Assert.False(returned.IsCompleted);
        Assert.Equal(1, calls);
        Assert.Same(context, observedContext);
        Assert.Same(state, observedState);

        completion.SetResult();
        await returned;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "iteration-217-state-machine-unhandled-callback-failure-and-cancellation-shapes")]
    public async Task Invocation_PreservesSynchronousFaultedAndCanceledTaskShapesAsync()
    {
        IBehaviorContext<CallbackSaga> context = CreateProxy<IBehaviorContext<CallbackSaga>>();
        IState state = CreateProxy<IState>();
        var synchronousFailure = new InvalidOperationException("synchronous callback failure");
        StateMachineUnhandledEventCallback<CallbackSaga> synchronousFault = (_, _) => throw synchronousFailure;
        Action invokeSynchronousFault = () => _ = synchronousFault(context, state);

        Assert.Same(synchronousFailure, Assert.Throws<InvalidOperationException>(invokeSynchronousFault));

        var asynchronousFailure = new ApplicationException("asynchronous callback failure");
        Task faultedTask = Task.FromException(asynchronousFailure);
        StateMachineUnhandledEventCallback<CallbackSaga> asynchronousFault = (_, _) => faultedTask;

        Task returnedFaultedTask = asynchronousFault(context, state);
        Assert.Same(faultedTask, returnedFaultedTask);
        Assert.Same(asynchronousFailure, await Assert.ThrowsAsync<ApplicationException>(() => returnedFaultedTask));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceledTask = Task.FromCanceled(cancellation.Token);
        StateMachineUnhandledEventCallback<CallbackSaga> canceled = (_, _) => canceledTask;

        Task returnedCanceledTask = canceled(context, state);
        Assert.Same(canceledTask, returnedCanceledTask);
        OperationCanceledException cancellationFailure =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returnedCanceledTask);
        Assert.Equal(cancellation.Token, cancellationFailure.CancellationToken);

    }

    static void AssertRuntimeMethod(
        MethodInfo[] methods,
        string name,
        Type returnType,
        Type[] parameterTypes,
        string[] parameterNames)
    {
        MethodInfo method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(parameterTypes, method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.Equal(parameterNames, method.GetParameters().Select(parameter => parameter.Name));
    }

    static TContract CreateProxy<TContract>()
        where TContract : class => DispatchProxy.Create<TContract, UnusedContractProxy>();

    class UnusedContractProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"The contract member '{targetMethod?.Name}' is not used by this delegate test.");
    }

    sealed class CallbackSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }
}
