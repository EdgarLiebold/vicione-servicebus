using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.SagaStateMachine;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineStateEventFilterDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-state-event-filter-public-contract")]
    public void StateEventFilters_ExposeTheExactPublicContract()
    {
        Type contract = typeof(IStateEventFilter<>);
        Assert.True(contract.IsPublic);
        Assert.True(contract.IsInterface);
        Assert.Single(contract.GetGenericArguments());
        AssertSagaParameter(contract.GetGenericArguments()[0]);

        MethodInfo[] contractMethods = contract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(2, contractMethods.Length);

        MethodInfo untypedFilter = Assert.Single(contractMethods, method => !method.IsGenericMethod);
        Assert.Equal(nameof(IStateEventFilter<TestSaga>.Filter), untypedFilter.Name);
        Assert.Equal(typeof(bool), untypedFilter.ReturnType);
        ParameterInfo untypedContext = Assert.Single(untypedFilter.GetParameters());
        Assert.Equal(
            typeof(IBehaviorContext<>).MakeGenericType(contract.GetGenericArguments()[0]),
            untypedContext.ParameterType);
        Assert.Equal("context", untypedContext.Name);

        MethodInfo typedFilter = Assert.Single(contractMethods, method => method.IsGenericMethodDefinition);
        Type message = Assert.Single(typedFilter.GetGenericArguments());
        Assert.Equal(nameof(IStateEventFilter<TestSaga>.Filter), typedFilter.Name);
        AssertReferenceTypeParameter(message);
        Assert.Equal(typeof(bool), typedFilter.ReturnType);
        ParameterInfo typedContext = Assert.Single(typedFilter.GetParameters());
        Assert.Equal(
            typeof(IBehaviorContext<,>).MakeGenericType(contract.GetGenericArguments()[0], message),
            typedContext.ParameterType);
        Assert.Equal("context", typedContext.Name);

        Type allDefinition = typeof(AllStateEventFilter<>);
        AssertSagaParameter(Assert.Single(allDefinition.GetGenericArguments()));
        Type allFilter = allDefinition.MakeGenericType(typeof(TestSaga));
        Assert.True(allFilter.IsPublic);
        Assert.False(allFilter.IsSealed);
        Assert.Equal([typeof(IStateEventFilter<TestSaga>)], allFilter.GetInterfaces());
        Assert.Empty(Assert.Single(allFilter.GetConstructors()).GetParameters());
        Assert.Equal(2, allFilter.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length);

        Type selectedDefinition = typeof(SelectedStateEventFilter<,>);
        Assert.True(selectedDefinition.IsPublic);
        Assert.False(selectedDefinition.IsSealed);
        Type[] selectedParameters = selectedDefinition.GetGenericArguments();
        Assert.Equal(2, selectedParameters.Length);
        AssertSagaParameter(selectedParameters[0]);
        AssertReferenceTypeParameter(selectedParameters[1]);

        Type selectedFilter = typeof(SelectedStateEventFilter<TestSaga, BaseMessage>);
        Assert.Equal([typeof(IStateEventFilter<TestSaga>)], selectedFilter.GetInterfaces());
        ConstructorInfo selectedConstructor = Assert.Single(selectedFilter.GetConstructors());
        ParameterInfo filterParameter = Assert.Single(selectedConstructor.GetParameters());
        Assert.Equal("filter", filterParameter.Name);
        Assert.Equal(typeof(StateMachineCondition<TestSaga, BaseMessage>), filterParameter.ParameterType);
        Assert.Equal(2, selectedFilter.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-all-state-event-filter-context-boundaries-and-total-valid-predicate")]
    public void AllStateEventFilter_RejectsNullAndReturnsTrueForEveryValidContextWithoutObservation()
    {
        var filter = new AllStateEventFilter<TestSaga>();
        IStateEventFilter<TestSaga> contract = filter;
        IBehaviorContext<TestSaga> untyped = CreateStrictContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, DerivedMessage> typed =
            CreateStrictContext<IBehaviorContext<TestSaga, DerivedMessage>>();

        Assert.True(filter.Filter(untyped));
        Assert.True(contract.Filter(untyped));
        Assert.True(contract.Filter(typed));
        Assert.True(filter.Filter(typed));
        ArgumentNullException untypedFailure = Assert.Throws<ArgumentNullException>(
            () => contract.Filter((IBehaviorContext<TestSaga>)null!));
        ArgumentNullException typedFailure = Assert.Throws<ArgumentNullException>(
            () => contract.Filter<DerivedMessage>(null!));
        Assert.Equal("context", untypedFailure.ParamName);
        Assert.Equal("context", typedFailure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-selected-state-event-filter-null-collaborator")]
    public void SelectedStateEventFilter_RejectsANullConditionAtConstruction()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new SelectedStateEventFilter<TestSaga, BaseMessage>(null!));

        Assert.Equal("filter", exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-selected-state-event-filter-exact-dispatch-identity-result")]
    public void SelectedStateEventFilter_InvokesTheConditionOnceForTheExactMessageContextAndPreservesItsIdentityAndResult(
        bool expected)
    {
        IBehaviorContext<TestSaga, BaseMessage> context =
            CreateStrictContext<IBehaviorContext<TestSaga, BaseMessage>>();
        IBehaviorContext<TestSaga, BaseMessage>? observed = null;
        var calls = 0;
        var filter = new SelectedStateEventFilter<TestSaga, BaseMessage>(candidate =>
        {
            calls++;
            observed = candidate;
            return expected;
        });

        bool actual = filter.Filter(context);

        Assert.Equal(expected, actual);
        Assert.Equal(1, calls);
        Assert.Same(context, observed);

        calls = 0;
        observed = null;
        IStateEventFilter<TestSaga> contract = filter;
        bool interfaceActual = contract.Filter(context);

        Assert.Equal(expected, interfaceActual);
        Assert.Equal(1, calls);
        Assert.Same(context, observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-selected-state-event-filter-covariant-generic-dispatch")]
    public void SelectedStateEventFilter_HonorsCovarianceAndRejectsInverseAndUnrelatedMessageContexts()
    {
        IBehaviorContext<TestSaga, DerivedMessage> derivedContext =
            CreateStrictContext<IBehaviorContext<TestSaga, DerivedMessage>>();
        IBehaviorContext<TestSaga, BaseMessage> baseContext =
            CreateStrictContext<IBehaviorContext<TestSaga, BaseMessage>>();
        IBehaviorContext<TestSaga, IMessageContract> interfaceContext =
            CreateStrictContext<IBehaviorContext<TestSaga, IMessageContract>>();
        IBehaviorContext<TestSaga, OtherMessage> unrelatedContext =
            CreateStrictContext<IBehaviorContext<TestSaga, OtherMessage>>();
        var baseObservations = new List<IBehaviorContext<TestSaga, BaseMessage>>();
        var derivedCalls = 0;
        var interfaceCalls = 0;
        IStateEventFilter<TestSaga> baseFilter = new SelectedStateEventFilter<TestSaga, BaseMessage>(context =>
        {
            baseObservations.Add(context);
            return true;
        });
        IStateEventFilter<TestSaga> derivedFilter = new SelectedStateEventFilter<TestSaga, DerivedMessage>(_ =>
        {
            derivedCalls++;
            return true;
        });
        IStateEventFilter<TestSaga> interfaceFilter = new SelectedStateEventFilter<TestSaga, IMessageContract>(_ =>
        {
            interfaceCalls++;
            return true;
        });
        IBehaviorContext<TestSaga, IMessageContract> widenedDerivedContext = derivedContext;

        Assert.True(baseFilter.Filter(derivedContext));
        Assert.True(baseFilter.Filter(widenedDerivedContext));
        Assert.False(derivedFilter.Filter(baseContext));
        Assert.True(interfaceFilter.Filter(derivedContext));
        Assert.False(derivedFilter.Filter(interfaceContext));
        Assert.False(baseFilter.Filter(unrelatedContext));
        Assert.Equal(2, baseObservations.Count);
        Assert.All(baseObservations, observed => Assert.Same(derivedContext, observed));
        Assert.Equal(0, derivedCalls);
        Assert.Equal(1, interfaceCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-selected-state-event-filter-untyped-incompatible-and-null-boundaries")]
    public void SelectedStateEventFilter_RejectsNullAndReturnsFalseForUntypedOrIncompatibleContexts()
    {
        var calls = 0;
        var concrete = new SelectedStateEventFilter<TestSaga, BaseMessage>(_ =>
        {
            calls++;
            return true;
        });
        IStateEventFilter<TestSaga> filter = concrete;
        IBehaviorContext<TestSaga> untyped = CreateStrictContext<IBehaviorContext<TestSaga>>();
        IBehaviorContext<TestSaga, BaseMessage> typed =
            CreateStrictContext<IBehaviorContext<TestSaga, BaseMessage>>();
        IBehaviorContext<TestSaga> erasedTyped = typed;
        IBehaviorContext<TestSaga, OtherMessage> incompatible =
            CreateStrictContext<IBehaviorContext<TestSaga, OtherMessage>>();

        Assert.False(concrete.Filter(untyped));
        Assert.False(filter.Filter(untyped));
        Assert.False(concrete.Filter(erasedTyped));
        Assert.False(filter.Filter(erasedTyped));
        Assert.False(filter.Filter(incompatible));
        ArgumentNullException untypedFailure = Assert.Throws<ArgumentNullException>(
            () => filter.Filter((IBehaviorContext<TestSaga>)null!));
        ArgumentNullException typedFailure = Assert.Throws<ArgumentNullException>(
            () => filter.Filter<BaseMessage>(null!));
        Assert.Equal("context", untypedFailure.ParamName);
        Assert.Equal("context", typedFailure.ParamName);
        Assert.Equal(0, calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-FAULT", "iteration-217-selected-state-event-filter-exception-identity")]
    public void SelectedStateEventFilter_PropagatesTheExactConditionException()
    {
        var expected = new MarkerException("filter failed");
        IStateEventFilter<TestSaga> filter = new SelectedStateEventFilter<TestSaga, BaseMessage>(_ => throw expected);
        IBehaviorContext<TestSaga, BaseMessage> context =
            CreateStrictContext<IBehaviorContext<TestSaga, BaseMessage>>();

        MarkerException actual = Assert.Throws<MarkerException>(() => filter.Filter(context));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-RUNTIME", "iteration-217-selected-state-event-filter-concurrent-stateless-dispatch")]
    public async Task SelectedStateEventFilter_ConcurrentCallsRemainIndependentAndPreserveEveryContextIdentityAsync()
    {
        const int callCount = 8;
        IBehaviorContext<TestSaga, BaseMessage>[] contexts = Enumerable.Range(0, callCount)
            .Select(_ => CreateStrictContext<IBehaviorContext<TestSaga, BaseMessage>>())
            .ToArray();
        var expectedByContext = new Dictionary<IBehaviorContext<TestSaga, BaseMessage>, bool>(
            ReferenceEqualityComparer.Instance);
        for (var index = 0; index < contexts.Length; index++)
            expectedByContext.Add(contexts[index], index % 3 == 0);
        var observations = new ConcurrentBag<IBehaviorContext<TestSaga, BaseMessage>>();
        using var entrants = new CountdownEvent(callCount);
        using var release = new ManualResetEventSlim();
        var active = 0;
        var maxActive = 0;
        var concreteFilter = new SelectedStateEventFilter<TestSaga, BaseMessage>(context =>
        {
            int currentActive = Interlocked.Increment(ref active);
            int observedMaximum;
            while (currentActive > (observedMaximum = Volatile.Read(ref maxActive)))
                Interlocked.CompareExchange(ref maxActive, currentActive, observedMaximum);
            observations.Add(context);
            entrants.Signal();
            release.Wait();
            try
            {
                return expectedByContext[context];
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        });
        IStateEventFilter<TestSaga> filter = concreteFilter;

        Task<bool>[] executions = contexts
            .Select(context => Task.Factory.StartNew(
                () => filter.Filter(context),
                TestContext.Current.CancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        try
        {
            Assert.True(
                entrants.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
                "Every filter invocation must enter before release.");
            Assert.True(Volatile.Read(ref maxActive) > 1, "The shared filter must permit overlapping dispatch.");
        }
        finally
        {
            release.Set();
        }
        bool[] results = await Task.WhenAll(executions);

        Assert.Equal(contexts.Select(context => expectedByContext[context]), results);
        Assert.Equal(callCount, observations.Count);
        Assert.All(contexts, expected => Assert.Single(observations, actual => ReferenceEquals(expected, actual)));
    }

    private static void AssertSagaParameter(Type parameter)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal([typeof(ISagaStateMachineInstance)], parameter.GetGenericParameterConstraints());
    }

    private static void AssertReferenceTypeParameter(Type parameter)
    {
        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Empty(parameter.GetGenericParameterConstraints());
    }

    private static T CreateStrictContext<T>()
        where T : class => DispatchProxy.Create<T, StrictContextProxy>();

    public class StrictContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new Xunit.Sdk.XunitException($"The filter unexpectedly observed context member '{targetMethod?.Name}'.");
    }

    public sealed class TestSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }

    public interface IMessageContract
    {
    }
    public record BaseMessage : IMessageContract;
    public sealed record DerivedMessage : BaseMessage;
    public sealed record OtherMessage;
    public sealed class MarkerException(string message) : Exception(message);
}
