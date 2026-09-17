using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRepositoryFactoryPolicyDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-constructor-requires-method")]
    public void FactoryConstructor_RejectsANullFactoryMethod()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(null!));

        Assert.Equal("factoryMethod", exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-create-requires-context-and-correlation")]
    public void FactoryCreate_RequiresContextAndCorrelationBeforeInvokingTheMethod(bool nullContext)
    {
        var calls = 0;
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(context =>
        {
            Interlocked.Increment(ref calls);
            return new FactorySaga { CorrelationId = context.CorrelationId!.Value };
        });

        Exception exception = nullContext
            ? Assert.Throws<ArgumentNullException>(() => factory.Create(null!))
            : Assert.Throws<SagaException>(() => factory.Create(CreateContext()));

        if (nullContext)
            Assert.Equal("context", Assert.IsType<ArgumentNullException>(exception).ParamName);
        else
            Assert.Contains("correlationId was not present", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref calls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-create-preserves-input-and-result")]
    public void FactoryCreate_InvokesTheMethodOnceWithTheExactContextAndReturnsItsExactSaga()
    {
        Guid correlationId = NewId.NextGuid();
        ConsumeContext<FactoryMessage> context = CreateContext(correlationId);
        var expected = new FactorySaga { CorrelationId = correlationId };
        ConsumeContext<FactoryMessage>? observed = null;
        var calls = 0;
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(received =>
        {
            observed = received;
            Interlocked.Increment(ref calls);
            return expected;
        });

        FactorySaga result = factory.Create(context);

        Assert.Same(expected, result);
        Assert.Same(context, observed);
        Assert.Equal(1, Volatile.Read(ref calls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-null-result-is-explicit")]
    public void FactoryMethods_RejectANullResultFromTheConfiguredMethod()
    {
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(_ => null!);
        var pipe = new RecordingSagaPipe();
        ConsumeContext<FactoryMessage> context = CreateContext(NewId.NextGuid());

        InvalidOperationException createException = Assert.Throws<InvalidOperationException>(() =>
            factory.Create(context));
        InvalidOperationException sendException = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factory.SendAsync(context, pipe);
        });

        Assert.Equal("The saga factory method returned no instance.", createException.Message);
        Assert.Equal(createException.Message, sendException.Message);
        Assert.Equal(0, pipe.Count);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("next")]
    [InlineData("correlation")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-send-requires-context-next-and-correlation")]
    public void FactorySend_RejectsInvalidInputsBeforeFactoryOrPipelineEffects(string input)
    {
        var factoryCalls = 0;
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(_ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new FactorySaga();
        });
        var pipe = new RecordingSagaPipe();
        ConsumeContext<FactoryMessage> context = input == "correlation"
            ? CreateContext()
            : CreateContext(NewId.NextGuid());

        Exception exception = input switch
        {
            "context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = factory.SendAsync(null!, pipe);
            }),
            "next" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = factory.SendAsync(context, null!);
            }),
            _ => Assert.Throws<SagaException>(() =>
            {
                _ = factory.SendAsync(context, pipe);
            }),
        };

        if (input != "correlation")
            Assert.Equal(input, Assert.IsType<ArgumentNullException>(exception).ParamName);
        else
            Assert.Contains("correlationId was not present", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref factoryCalls));
        Assert.Equal(0, pipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-send-preserves-state-context-and-cancellation")]
    public async Task FactorySend_WrapsTheExactStateAndPreservesTheConsumeCancellationAsync()
    {
        Guid correlationId = NewId.NextGuid();
        using var source = new CancellationTokenSource();
        source.Cancel();
        ConsumeContext<FactoryMessage> context = CreateContextWithToken(correlationId, source.Token);
        var expected = new FactorySaga { CorrelationId = correlationId };
        var canceledTask = Task.FromCanceled(source.Token);
        var pipe = new RecordingSagaPipe { Result = canceledTask };
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(_ => expected);

        Task result = factory.SendAsync(context, pipe);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => result);

        Assert.Same(canceledTask, result);
        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, pipe.Count);
        Assert.NotNull(pipe.Context);
        Assert.Same(expected, pipe.Context.Saga);
        Assert.Equal(correlationId, pipe.Context.CorrelationId);
        Assert.Equal(source.Token, pipe.Context.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-send-invokes-method-once-with-exact-context")]
    public async Task FactorySend_InvokesTheMethodOnceWithTheExactContextAsync()
    {
        Guid correlationId = NewId.NextGuid();
        ConsumeContext<FactoryMessage> context = CreateContext(correlationId);
        var expected = new FactorySaga { CorrelationId = correlationId };
        ConsumeContext<FactoryMessage>? observed = null;
        var calls = 0;
        var pipe = new RecordingSagaPipe();
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(received =>
        {
            observed = received;
            Interlocked.Increment(ref calls);
            return expected;
        });

        await factory.SendAsync(context, pipe);

        Assert.Equal(1, Volatile.Read(ref calls));
        Assert.Same(context, observed);
        Assert.Equal(1, pipe.Count);
        Assert.NotNull(pipe.Context);
        Assert.Same(expected, pipe.Context.Saga);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "factory-send-distinguishes-pipeline-fault-and-null-task")]
    public async Task FactorySend_DistinguishesPipelineExceptionFromANullTaskAsync(bool nullTask)
    {
        var expected = new ApplicationException("pipeline failed");
        var pipe = new RecordingSagaPipe
        {
            Result = nullTask ? null : Task.FromException(expected),
        };
        var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(context =>
            new FactorySaga { CorrelationId = context.CorrelationId!.Value });

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(() =>
            factory.SendAsync(CreateContext(NewId.NextGuid()), pipe));

        if (nullTask)
            Assert.Equal("The saga pipeline returned no task.", Assert.IsType<InvalidOperationException>(exception).Message);
        else
            Assert.Same(expected, exception);
        Assert.Equal(1, pipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-constructor-and-write-mode")]
    public void NewPolicy_RequiresAFactoryAndIsAlwaysWritable()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new NewSagaPolicy<FactorySaga, FactoryMessage>(null!, false));
        var policy = new NewSagaPolicy<FactorySaga, FactoryMessage>(new RecordingSagaFactory(), false);

        Assert.Equal("sagaFactory", exception.ParamName);
        Assert.False(policy.IsReadOnly);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-preinsert-controls-factory-lifecycle")]
    public void NewPolicy_PreInsertCreatesOnlyWhenConfigured(bool insertOnInitial)
    {
        Guid correlationId = NewId.NextGuid();
        ConsumeContext<FactoryMessage> context = CreateContext(correlationId);
        var expected = new FactorySaga { CorrelationId = correlationId };
        var factory = new RecordingSagaFactory { CreateResult = expected };
        var policy = new NewSagaPolicy<FactorySaga, FactoryMessage>(factory, insertOnInitial);

        bool result = policy.PreInsertInstance(context, out FactorySaga? instance);

        Assert.Equal(insertOnInitial, result);
        Assert.Equal(insertOnInitial ? 1 : 0, factory.CreateCount);
        if (insertOnInitial)
        {
            Assert.Same(expected, instance);
            Assert.Same(context, factory.CreateContext);
        }
        else
            Assert.Null(instance);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("result")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-preinsert-enforces-nonnull-contract")]
    public void NewPolicy_PreInsertRejectsNullContextOrFactoryResult(string boundary)
    {
        var factory = new RecordingSagaFactory { CreateResult = null };
        var policy = new NewSagaPolicy<FactorySaga, FactoryMessage>(factory, true);

        Exception exception = boundary == "context"
            ? Assert.Throws<ArgumentNullException>(() => policy.PreInsertInstance(null!, out _))
            : Assert.Throws<InvalidOperationException>(() => policy.PreInsertInstance(
                CreateContext(NewId.NextGuid()), out _));

        if (boundary == "context")
        {
            Assert.Equal("context", Assert.IsType<ArgumentNullException>(exception).ParamName);
            Assert.Equal(0, factory.CreateCount);
        }
        else
        {
            Assert.Equal("The saga factory returned no instance.", exception.Message);
            Assert.Equal(1, factory.CreateCount);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-preinsert-false-mode-still-requires-context")]
    public void NewPolicy_PreInsertFalseModeStillRejectsANullContext()
    {
        var factory = new RecordingSagaFactory();
        var policy = new NewSagaPolicy<FactorySaga, FactoryMessage>(factory, false);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            policy.PreInsertInstance(null!, out _));

        Assert.Equal("context", exception.ParamName);
        Assert.Equal(0, factory.CreateCount);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("next")]
    [InlineData("existing")]
    [InlineData("existing-without-correlation")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-existing-rejects-inputs-and-existing-state")]
    public void NewPolicy_ExistingRejectsRequiredInputsOrTheExistingSaga(string boundary)
    {
        Guid correlationId = NewId.NextGuid();
        var saga = new FactorySaga { CorrelationId = correlationId };
        var pipe = new RecordingSagaPipe();
        SagaConsumeContext<FactorySaga, FactoryMessage> context = boundary == "existing-without-correlation"
            ? DispatchProxy.Create<SagaConsumeContext<FactorySaga, FactoryMessage>, NullCorrelationSagaContextProxy>()
            : new DefaultSagaConsumeContext<FactorySaga, FactoryMessage>(CreateContext(correlationId), saga);
        ISagaPolicy<FactorySaga, FactoryMessage> policy =
            new NewSagaPolicy<FactorySaga, FactoryMessage>(new RecordingSagaFactory(), false);

        Exception exception = boundary switch
        {
            "context" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(null!, pipe);
            }),
            "next" => Assert.Throws<ArgumentNullException>(() =>
            {
                _ = policy.ExistingAsync(context, null!);
            }),
            _ => Assert.Throws<SagaException>(() =>
            {
                _ = policy.ExistingAsync(context, pipe);
            }),
        };

        if (boundary is "context" or "next")
            Assert.Equal(boundary, Assert.IsType<ArgumentNullException>(exception).ParamName);
        else
            Assert.Contains("cannot be accepted by an existing saga", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, pipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-existing-null-correlation-uses-empty-diagnostic-id")]
    public void NewPolicy_ExistingUsesAnEmptyDiagnosticIdWhenCorrelationIsMissing()
    {
        var pipe = new RecordingSagaPipe();
        SagaConsumeContext<FactorySaga, FactoryMessage> context =
            DispatchProxy.Create<SagaConsumeContext<FactorySaga, FactoryMessage>, NullCorrelationSagaContextProxy>();
        ISagaPolicy<FactorySaga, FactoryMessage> policy =
            new NewSagaPolicy<FactorySaga, FactoryMessage>(new RecordingSagaFactory(), false);

        SagaException exception = Assert.Throws<SagaException>(() =>
        {
            _ = policy.ExistingAsync(context, pipe);
        });

        Assert.Equal(Guid.Empty, exception.CorrelationId);
        Assert.Same(typeof(FactorySaga), exception.SagaType);
        Assert.Same(typeof(FactoryMessage), exception.MessageType);
        Assert.Equal(0, pipe.Count);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("next")]
    [InlineData("task")]
    [InlineData("fault")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "new-policy-missing-delegates-task-and-fault-contract")]
    public async Task NewPolicy_MissingValidatesInputsAndPreservesFactoryTaskOrFaultAsync(string boundary)
    {
        var expectedFault = new ApplicationException("factory send failed");
        var expectedTask = boundary == "fault" ? Task.FromException(expectedFault) : Task.CompletedTask;
        var factory = new RecordingSagaFactory
        {
            SendResult = boundary == "task" ? null : expectedTask,
        };
        var pipe = new RecordingSagaPipe();
        ConsumeContext<FactoryMessage> context = CreateContext(NewId.NextGuid());
        ISagaPolicy<FactorySaga, FactoryMessage> policy =
            new NewSagaPolicy<FactorySaga, FactoryMessage>(factory, false);

        if (boundary is "context" or "next" or "task")
        {
            Exception exception = boundary switch
            {
                "context" => Assert.Throws<ArgumentNullException>(() =>
                {
                    _ = policy.MissingAsync(null!, pipe);
                }),
                "next" => Assert.Throws<ArgumentNullException>(() =>
                {
                    _ = policy.MissingAsync(context, null!);
                }),
                _ => Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = policy.MissingAsync(context, pipe);
                }),
            };

            if (boundary is "context" or "next")
                Assert.Equal(boundary, Assert.IsType<ArgumentNullException>(exception).ParamName);
            else
                Assert.Equal("The saga factory returned no send task.", exception.Message);
            Assert.Equal(boundary == "task" ? 1 : 0, factory.SendCount);
            return;
        }

        Task returned = policy.MissingAsync(context, pipe);
        ApplicationException failure = await Assert.ThrowsAsync<ApplicationException>(() => returned);

        Assert.Same(expectedTask, returned);
        Assert.Same(expectedFault, failure);
        Assert.Equal(1, factory.SendCount);
        Assert.Same(context, factory.SendContext);
        Assert.Same(pipe, factory.Next);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("send")]
    [InlineData("preinsert")]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "synchronous-factory-exception-identity-across-entry-points")]
    public void FactoryEntryPoints_PreserveTheExactSynchronousFactoryException(string entryPoint)
    {
        var expected = new ApplicationException("factory failed synchronously");
        ConsumeContext<FactoryMessage> context = CreateContext(NewId.NextGuid());
        var pipe = new RecordingSagaPipe();

        Exception exception;
        if (entryPoint == "preinsert")
        {
            var sagaFactory = new RecordingSagaFactory { CreateException = expected };
            var policy = new NewSagaPolicy<FactorySaga, FactoryMessage>(sagaFactory, true);
            exception = Assert.Throws<ApplicationException>(() => policy.PreInsertInstance(context, out _));
            Assert.Equal(1, sagaFactory.CreateCount);
        }
        else
        {
            var factory = new FactoryMethodSagaFactory<FactorySaga, FactoryMessage>(_ => throw expected);
            exception = entryPoint == "create"
                ? Assert.Throws<ApplicationException>(() => factory.Create(context))
                : Assert.Throws<ApplicationException>(() =>
                {
                    _ = factory.SendAsync(context, pipe);
                });
        }

        Assert.Same(expected, exception);
        Assert.Equal(0, pipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "removed-exception-identifies-exact-state")]
    public void RemovedException_RequiresTheTypeAndIdentifiesTheExactSagaInstance()
    {
        Guid correlationId = NewId.NextGuid();
        Type? exceptionType = typeof(SagaConsumeContextMode).Assembly.GetType(
            "ViciOne.ServiceBus.Saga.SagaInstanceRemovedException");
        Assert.NotNull(exceptionType);
        ConstructorInfo constructor = Assert.Single(exceptionType.GetConstructors());

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() =>
            constructor.Invoke([null, correlationId]));
        ArgumentNullException nullType = Assert.IsType<ArgumentNullException>(invocation.InnerException);
        var exception = Assert.IsAssignableFrom<InvalidOperationException>(
            constructor.Invoke([typeof(FactorySaga), correlationId]));

        Assert.Equal("sagaType", nullType.ParamName);
        Assert.StartsWith("The saga instance was removed: ", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(FactorySaga), exception.Message, StringComparison.Ordinal);
        Assert.EndsWith($": {correlationId}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-CONTEXT", "mode-and-instance-delegate-public-contract")]
    public void ModeAndInstanceDelegate_KeepStableValuesAndCorrelationIdentity()
    {
        Assert.Equal(["Load", "Add", "Insert"], Enum.GetNames<SagaConsumeContextMode>());
        Assert.Equal([0, 1, 2], Enum.GetValues<SagaConsumeContextMode>().Select(x => (int)x));
        Guid correlationId = NewId.NextGuid();
        SagaInstanceFactoryMethod<DerivedFactorySaga> derived = id => new DerivedFactorySaga { CorrelationId = id };
        SagaInstanceFactoryMethod<FactorySaga> covariant = derived;

        FactorySaga saga = covariant(correlationId);

        Assert.IsType<DerivedFactorySaga>(saga);
        Assert.Equal(correlationId, saga.CorrelationId);
    }

    private static ConsumeContext<FactoryMessage> CreateContext(Guid? correlationId = null) =>
        InMemoryOutboxTestContextFactory.Create(
            new FactoryMessage(),
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    private static ConsumeContext<FactoryMessage> CreateContextWithToken(
        Guid? correlationId,
        CancellationToken cancellationToken) =>
        InMemoryOutboxTestContextFactory.Create(
            new FactoryMessage(),
            cancellationToken,
            correlationId: correlationId);

    private sealed class RecordingSagaPipe : IPipe<SagaConsumeContext<FactorySaga, FactoryMessage>>
    {
        private int _count;

        public SagaConsumeContext<FactorySaga, FactoryMessage>? Context { get; private set; }
        public int Count => Volatile.Read(ref _count);
        public Task? Result { get; init; } = Task.CompletedTask;

        public Task SendAsync(SagaConsumeContext<FactorySaga, FactoryMessage> context)
        {
            Context = context;
            Interlocked.Increment(ref _count);
            return Result!;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingSagaFactory : ISagaFactory<FactorySaga, FactoryMessage>
    {
        private int _createCount;
        private int _sendCount;

        public int CreateCount => Volatile.Read(ref _createCount);
        public ConsumeContext<FactoryMessage>? CreateContext { get; private set; }
        public Exception? CreateException { get; init; }
        public FactorySaga? CreateResult { get; init; } = new();
        public IPipe<SagaConsumeContext<FactorySaga, FactoryMessage>>? Next { get; private set; }
        public int SendCount => Volatile.Read(ref _sendCount);
        public ConsumeContext<FactoryMessage>? SendContext { get; private set; }
        public Task? SendResult { get; init; } = Task.CompletedTask;

        public FactorySaga Create(ConsumeContext<FactoryMessage> context)
        {
            CreateContext = context;
            Interlocked.Increment(ref _createCount);
            if (CreateException is not null)
                throw CreateException;

            return CreateResult!;
        }

        public Task SendAsync(
            ConsumeContext<FactoryMessage> context,
            IPipe<SagaConsumeContext<FactorySaga, FactoryMessage>> next)
        {
            SendContext = context;
            Next = next;
            Interlocked.Increment(ref _sendCount);
            return SendResult!;
        }
    }

    public class NullCorrelationSagaContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_CorrelationId")
                return null;

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    public class FactorySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class DerivedFactorySaga : FactorySaga;

    public sealed record FactoryMessage;
}
