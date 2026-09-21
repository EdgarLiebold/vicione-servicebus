using System.Reflection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga;

public sealed class SagaPolicyContextQueryDeepContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "any-existing-policy-read-only-and-preinsert-contract")]
    public void AnyExistingPolicy_PreservesReadOnlyModeAndNeverPreInserts(bool readOnly)
    {
        ConsumeContext<PolicyMessage> context = CreateContext(new PolicyMessage());
        var policy = new AnyExistingSagaPolicy<PolicySaga, PolicyMessage>(readOnly: readOnly);

        bool result = policy.PreInsertInstance(context, out PolicySaga? instance);

        Assert.Equal(readOnly, policy.IsReadOnly);
        Assert.False(result);
        Assert.Null(instance);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "any-existing-policy-exact-task-fault-and-cancellation-identity")]
    public async Task AnyExistingPolicy_ForwardsExactExistingAndConfiguredMissingTasksAsync()
    {
        var expectedFault = new InvalidOperationException("existing failed");
        Task faulted = Task.FromException(expectedFault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceled = Task.FromCanceled(cancellation.Token);
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        SagaConsumeContext<PolicySaga, PolicyMessage>? observedExisting = null;
        ConsumeContext<PolicyMessage>? observedMissing = null;
        var existingPipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(context =>
        {
            observedExisting = context;
            return faulted;
        });
        var missingPipe = new DelegatePipe<ConsumeContext<PolicyMessage>>(context =>
        {
            observedMissing = context;
            return canceled;
        });
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new AnyExistingSagaPolicy<PolicySaga, PolicyMessage>(missingPipe);

        Task existing = policy.ExistingAsync(sagaContext, existingPipe);
        Task missing = policy.MissingAsync(consumeContext, existingPipe);

        Assert.Same(faulted, existing);
        Assert.Same(canceled, missing);
        Assert.Same(sagaContext, observedExisting);
        Assert.Same(consumeContext, observedMissing);
        Assert.Same(expectedFault, await Assert.ThrowsAsync<InvalidOperationException>(() => existing));
        OperationCanceledException canceledException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => missing);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "any-existing-policy-default-missing-pipeline-is-terminal")]
    public async Task AnyExistingPolicy_DefaultMissingPipelineCompletesWithoutInvokingTheSagaPipeAsync()
    {
        ConsumeContext<PolicyMessage> context = CreateContext(new PolicyMessage());
        var sagaPipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(_ =>
            throw new InvalidOperationException("The saga pipe must not run for a missing instance."));
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new AnyExistingSagaPolicy<PolicySaga, PolicyMessage>();

        await policy.MissingAsync(context, sagaPipe);
    }

    [Theory]
    [InlineData("preinsert", "context")]
    [InlineData("existing-context", "context")]
    [InlineData("existing-next", "next")]
    [InlineData("missing-context", "context")]
    [InlineData("missing-next", "next")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "any-existing-policy-null-argument-matrix")]
    public async Task AnyExistingPolicy_RejectsEveryNullArgumentAsync(string boundary, string parameterName)
    {
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        var pipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(_ => Task.CompletedTask);
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new AnyExistingSagaPolicy<PolicySaga, PolicyMessage>();

        ArgumentNullException exception = boundary switch
        {
            "preinsert" => Assert.Throws<ArgumentNullException>(() => policy.PreInsertInstance(null!, out _)),
            "existing-context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(null!, pipe);
            }),
            "existing-next" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(sagaContext, null!);
            }),
            "missing-context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.MissingAsync(null!, pipe);
            }),
            _ => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.MissingAsync(consumeContext, null!);
            }),
        };

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(false, "existing saga pipeline")]
    [InlineData(true, "missing saga pipeline")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "any-existing-policy-null-task-boundaries")]
    public async Task AnyExistingPolicy_ConvertsEveryCollaboratorNullTaskIntoADeterministicFaultAsync(
        bool missing,
        string expectedSource)
    {
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        var nullSagaPipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(_ => null!);
        var missingPipe = new DelegatePipe<ConsumeContext<PolicyMessage>>(_ => null!);
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new AnyExistingSagaPolicy<PolicySaga, PolicyMessage>(missingPipe);

        Task operation = missing
            ? policy.MissingAsync(consumeContext, nullSagaPipe)
            : policy.ExistingAsync(sagaContext, nullSagaPipe);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);

        Assert.Contains(expectedSource, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("null task", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-or-existing-policy-preinsert-lifecycle")]
    public void NewOrExistingPolicy_PreInsertCallsTheFactoryOnlyWhenEnabled(bool insertOnInitial)
    {
        ConsumeContext<PolicyMessage> context = CreateContext(new PolicyMessage());
        var expected = new PolicySaga { CorrelationId = NewId.NextGuid() };
        var factory = new RecordingSagaFactory { CreateResult = expected };
        var policy = new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(factory, insertOnInitial);

        bool result = policy.PreInsertInstance(context, out PolicySaga? instance);

        Assert.False(policy.IsReadOnly);
        Assert.Equal(insertOnInitial, result);
        Assert.Equal(insertOnInitial ? 1 : 0, factory.CreateCount);
        Assert.Same(insertOnInitial ? context : null, factory.CreateContext);
        if (insertOnInitial)
            Assert.Same(expected, instance);
        else
            Assert.Null(instance);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-or-existing-policy-nonnull-factory-and-result-contract")]
    public void NewOrExistingPolicy_RejectsNullFactoryAndNullCreatedInstance()
    {
        ArgumentNullException factoryException = Assert.Throws<ArgumentNullException>(() =>
            new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(null!, true));
        var factory = new RecordingSagaFactory { CreateResult = null };
        var policy = new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(factory, true);

        InvalidOperationException resultException = Assert.Throws<InvalidOperationException>(() =>
            policy.PreInsertInstance(CreateContext(new PolicyMessage()), out _));

        Assert.Equal("sagaFactory", factoryException.ParamName);
        Assert.Contains("null instance", resultException.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, factory.CreateCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-or-existing-policy-exact-task-fault-and-cancellation-identity")]
    public async Task NewOrExistingPolicy_ForwardsExactExistingAndMissingTasksAsync()
    {
        var expectedFault = new InvalidOperationException("existing failed");
        Task faulted = Task.FromException(expectedFault);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task canceled = Task.FromCanceled(cancellation.Token);
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        SagaConsumeContext<PolicySaga, PolicyMessage>? observedExisting = null;
        var existingPipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(context =>
        {
            observedExisting = context;
            return faulted;
        });
        var factory = new RecordingSagaFactory { SendTask = canceled };
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(factory, false);

        Task existing = policy.ExistingAsync(sagaContext, existingPipe);
        Task missing = policy.MissingAsync(consumeContext, existingPipe);

        Assert.Same(faulted, existing);
        Assert.Same(canceled, missing);
        Assert.Same(sagaContext, observedExisting);
        Assert.Same(consumeContext, factory.SendContext);
        Assert.Same(existingPipe, factory.SendPipe);
        Assert.Same(expectedFault, await Assert.ThrowsAsync<InvalidOperationException>(() => existing));
        OperationCanceledException canceledException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => missing);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Theory]
    [InlineData("preinsert", "context")]
    [InlineData("existing-context", "context")]
    [InlineData("existing-next", "next")]
    [InlineData("missing-context", "context")]
    [InlineData("missing-next", "next")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-or-existing-policy-null-argument-matrix")]
    public async Task NewOrExistingPolicy_RejectsEveryNullArgumentAsync(string boundary, string parameterName)
    {
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        var pipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(_ => Task.CompletedTask);
        ISagaPolicy<PolicySaga, PolicyMessage> policy =
            new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(new RecordingSagaFactory(), false);

        ArgumentNullException exception = boundary switch
        {
            "preinsert" => Assert.Throws<ArgumentNullException>(() => policy.PreInsertInstance(null!, out _)),
            "existing-context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(null!, pipe);
            }),
            "existing-next" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(sagaContext, null!);
            }),
            "missing-context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.MissingAsync(null!, pipe);
            }),
            _ => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.MissingAsync(consumeContext, null!);
            }),
        };

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Theory]
    [InlineData(false, "existing saga pipeline")]
    [InlineData(true, "saga factory")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-or-existing-policy-null-task-boundaries")]
    public async Task NewOrExistingPolicy_ConvertsEveryCollaboratorNullTaskIntoADeterministicFaultAsync(
        bool missing,
        string expectedSource)
    {
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(new PolicyMessage());
        SagaConsumeContext<PolicySaga, PolicyMessage> sagaContext = await CreateSagaContextAsync(consumeContext);
        var nullSagaPipe = new DelegatePipe<SagaConsumeContext<PolicySaga, PolicyMessage>>(_ => null!);
        var factory = new RecordingSagaFactory { SendTask = null };
        ISagaPolicy<PolicySaga, PolicyMessage> policy = new NewOrExistingSagaPolicy<PolicySaga, PolicyMessage>(factory, false);

        Task operation = missing
            ? policy.MissingAsync(consumeContext, nullSagaPipe)
            : policy.ExistingAsync(sagaContext, nullSagaPipe);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);

        Assert.Contains(expectedSource, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("null task", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "saga-query-concurrent-single-delegate-identity")]
    public async Task SagaQuery_ConcurrentCallersReceiveTheExactSameCompiledDelegateAsync()
    {
        var query = new SagaQuery<PolicySaga>(saga => saga.CorrelationId != Guid.Empty);
        Task<Func<PolicySaga, bool>>[] callers = Enumerable.Range(0, 64)
            .Select(_ => Task.Run(query.GetFilter, TestContext.Current.CancellationToken))
            .ToArray();

        Func<PolicySaga, bool>[] filters = await Task.WhenAll(callers);

        Assert.All(filters, filter => Assert.Same(filters[0], filter));
        Assert.True(filters[0](new PolicySaga { CorrelationId = NewId.NextGuid() }));
        Assert.False(filters[0](new PolicySaga { CorrelationId = Guid.Empty }));
    }

    [Theory]
    [InlineData(SagaConsumeContextMode.Add)]
    [InlineData(SagaConsumeContextMode.Insert)]
    [InlineData(SagaConsumeContextMode.Load)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "default-context-factory-exact-context-message-and-state-identity")]
    public async Task SagaConsumeContextFactory_EachModePreservesExactContextMessageAndStateIdentityAsync(
        SagaConsumeContextMode mode)
    {
        var message = new PolicyMessage();
        ConsumeContext<PolicyMessage> consumeContext = CreateContext(message);
        var marker = new ContextMarker();
        consumeContext.GetOrAddPayload(() => marker);
        var saga = new PolicySaga { CorrelationId = NewId.NextGuid() };
        var repositoryContext = new object();
        var factory = new SagaConsumeContextFactory<object, PolicySaga>();

        SagaConsumeContext<PolicySaga, PolicyMessage> result = await factory.CreateSagaConsumeContextAsync(
            repositoryContext,
            consumeContext,
            saga,
            mode);

        Assert.Same(message, result.Message);
        Assert.Same(saga, result.Saga);
        Assert.Equal(consumeContext.MessageId, result.MessageId);
        Assert.Equal(consumeContext.CancellationToken, result.CancellationToken);
        Assert.True(result.TryGetPayload(out ContextMarker? observedMarker));
        Assert.Same(marker, observedMarker);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "saga-filter-factory-public-delegate-shape")]
    public void SagaFilterFactory_DeclaresTheExactRepositoryPolicyPipeAndFilterShape()
    {
        Type openDelegate = typeof(SagaFilterFactory<,>);
        Type[] genericArguments = openDelegate.GetGenericArguments();
        MethodInfo invoke = Assert.Single(typeof(SagaFilterFactory<PolicySaga, PolicyMessage>)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), method => method.Name == "Invoke");
        Type[] expectedParameters =
        [
            typeof(ISagaRepository<PolicySaga>),
            typeof(ISagaPolicy<PolicySaga, PolicyMessage>),
            typeof(IPipe<SagaConsumeContext<PolicySaga, PolicyMessage>>),
        ];

        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            genericArguments[0].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            genericArguments[1].GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Contains(typeof(ISaga), genericArguments[0].GetGenericParameterConstraints());
        Assert.Empty(genericArguments[1].GetGenericParameterConstraints());
        Assert.Equal(typeof(IFilter<ConsumeContext<PolicyMessage>>), invoke.ReturnType);
        Assert.Equal(expectedParameters, invoke.GetParameters().Select(parameter => parameter.ParameterType));
    }

    private static ConsumeContext<PolicyMessage> CreateContext(PolicyMessage message) =>
        InMemoryOutboxTestContextFactory.Create(message, TestContext.Current.CancellationToken, correlationId: NewId.NextGuid());

    private static Task<SagaConsumeContext<PolicySaga, PolicyMessage>> CreateSagaContextAsync(
        ConsumeContext<PolicyMessage> consumeContext) =>
        new SagaConsumeContextFactory<object, PolicySaga>().CreateSagaConsumeContextAsync(
            new object(),
            consumeContext,
            new PolicySaga { CorrelationId = consumeContext.CorrelationId!.Value },
            SagaConsumeContextMode.Load);

    private sealed class RecordingSagaFactory : ISagaFactory<PolicySaga, PolicyMessage>
    {
        public PolicySaga? CreateResult { get; init; } = new() { CorrelationId = NewId.NextGuid() };

        public Task? SendTask { get; init; } = Task.CompletedTask;

        public int CreateCount { get; private set; }

        public ConsumeContext<PolicyMessage>? CreateContext { get; private set; }

        public ConsumeContext<PolicyMessage>? SendContext { get; private set; }

        public IPipe<SagaConsumeContext<PolicySaga, PolicyMessage>>? SendPipe { get; private set; }

        public PolicySaga Create(ConsumeContext<PolicyMessage> context)
        {
            CreateCount++;
            CreateContext = context;
            return CreateResult!;
        }

        public Task SendAsync(
            ConsumeContext<PolicyMessage> context,
            IPipe<SagaConsumeContext<PolicySaga, PolicyMessage>> next)
        {
            SendContext = context;
            SendPipe = next;
            return SendTask!;
        }
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class ContextMarker;

    public sealed class PolicySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record PolicyMessage;
}
