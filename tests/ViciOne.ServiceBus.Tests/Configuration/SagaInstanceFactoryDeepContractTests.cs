using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaInstanceFactoryDeepContractTests
{
    private const string ConstructorFactoryTypeName =
        "ViciOne.ServiceBus.Configuration.ConstructorSagaInstanceFactory`1";
    private const string MetadataCacheTypeName =
        "ViciOne.ServiceBus.Configuration.SagaMetadataCache`1";
    private const string PropertyFactoryTypeName =
        "ViciOne.ServiceBus.Configuration.PropertySagaInstanceFactory`1";

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "exact-public-internal-surface-and-generic-constraints")]
    public void Surface_ExposesOnePublicFactoryAndTwoInternalCompiledFactories()
    {
        Type factory = typeof(DefaultSagaFactory<,>);
        Type[] arguments = factory.GetGenericArguments();

        Assert.True(factory.IsPublic && factory.IsClass);
        Assert.False(factory.IsAbstract);
        Assert.False(factory.IsSealed);
        AssertReferenceConstraint(arguments[0], typeof(ISaga));
        AssertReferenceConstraint(arguments[1]);

        Type contract = Assert.Single(factory.GetInterfaces());
        Assert.Equal(typeof(ISagaFactory<,>), contract.GetGenericTypeDefinition());
        Assert.Equal(arguments, contract.GetGenericArguments());

        ConstructorInfo constructor = Assert.Single(factory.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(constructor.GetParameters());

        MethodInfo[] methods = factory.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(2, methods.Length);
        MethodInfo create = Assert.Single(methods, method => method.Name == nameof(ISagaFactory<ISaga, object>.Create));
        Assert.Equal(arguments[0], create.ReturnType);
        ParameterInfo createContext = Assert.Single(create.GetParameters());
        Assert.Equal("context", createContext.Name);
        Assert.Equal(typeof(ConsumeContext<>).MakeGenericType(arguments[1]), createContext.ParameterType);

        MethodInfo send = Assert.Single(methods, method => method.Name == nameof(ISagaFactory<ISaga, object>.SendAsync));
        Assert.Equal(typeof(Task), send.ReturnType);
        Assert.Equal(["context", "next"], send.GetParameters().Select(parameter => parameter.Name));
        Assert.Equal(typeof(ConsumeContext<>).MakeGenericType(arguments[1]), send.GetParameters()[0].ParameterType);
        Assert.Equal(
            typeof(IPipe<>).MakeGenericType(
                typeof(SagaConsumeContext<,>).MakeGenericType(arguments[0], arguments[1])),
            send.GetParameters()[1].ParameterType);

        AssertInternalFactorySurface(ConstructorFactoryTypeName);
        AssertInternalFactorySurface(PropertyFactoryTypeName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "constructor-factory-rejects-interface-and-abstract-saga-first")]
    public void ConstructorFactory_RejectsInterfaceAndAbstractSagaTypesBeforeConstructorDiscovery()
    {
        ArgumentException interfaceFailure = CreateInternalFactoryFailure<IContractSaga>(ConstructorFactoryTypeName);
        ArgumentException abstractFailure = CreateInternalFactoryFailure<AbstractGuidSaga>(ConstructorFactoryTypeName);

        Assert.Equal($"The saga must be a concrete class: {ShortName<IContractSaga>()}", interfaceFailure.Message);
        Assert.Equal($"The saga must be a concrete class: {ShortName<AbstractGuidSaga>()}", abstractFailure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "constructor-factory-requires-exact-public-guid-constructor")]
    public void ConstructorFactory_RequiresAnExactPublicGuidConstructorWithStableDiagnostics()
    {
        ArgumentException missing = CreateInternalFactoryFailure<ParameterlessOnlySaga>(ConstructorFactoryTypeName);
        ArgumentException nonPublic = CreateInternalFactoryFailure<PrivateGuidSaga>(ConstructorFactoryTypeName);

        Assert.Equal(ConstructorDiagnostic<ParameterlessOnlySaga>(), missing.Message);
        Assert.Equal(ConstructorDiagnostic<PrivateGuidSaga>(), nonPublic.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "constructor-delegate-identity-guid-freshness-and-unwrapped-failure")]
    public void ConstructorFactory_CompiledDelegatePreservesIdentityGuidFreshnessAndException()
    {
        ConstructorAssignedSaga.Reset();
        object factory = CreateInternalFactory<ConstructorAssignedSaga>(ConstructorFactoryTypeName);
        SagaInstanceFactoryMethod<ConstructorAssignedSaga> method = ReadFactoryMethod<ConstructorAssignedSaga>(factory);
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();

        ConstructorAssignedSaga first = method(firstId);
        ConstructorAssignedSaga second = method(secondId);

        Assert.Same(method, ReadFactoryMethod<ConstructorAssignedSaga>(factory));
        Assert.NotSame(first, second);
        Assert.Equal(firstId, first.CorrelationId);
        Assert.Equal(secondId, second.CorrelationId);
        Assert.Equal(2, ConstructorAssignedSaga.ConstructorCount);

        SagaInstanceFactoryMethod<ThrowingConstructorSaga> throwing =
            ReadFactoryMethod<ThrowingConstructorSaga>(
                CreateInternalFactory<ThrowingConstructorSaga>(ConstructorFactoryTypeName));
        FactoryFailureException exception = Assert.Throws<FactoryFailureException>(() => throwing(NewId.NextGuid()));
        Assert.Same(ThrowingConstructorSaga.ExpectedFailure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "property-factory-rejects-interface-and-abstract-saga-first")]
    public void PropertyFactory_RejectsInterfaceAndAbstractSagaTypesBeforeMemberDiscovery()
    {
        ArgumentException interfaceFailure = CreateInternalFactoryFailure<IContractSaga>(PropertyFactoryTypeName);
        ArgumentException abstractFailure = CreateInternalFactoryFailure<AbstractPropertySaga>(PropertyFactoryTypeName);

        Assert.Equal($"The saga must be a concrete class: {ShortName<IContractSaga>()}", interfaceFailure.Message);
        Assert.Equal($"The saga must be a concrete class: {ShortName<AbstractPropertySaga>()}", abstractFailure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "property-factory-public-default-and-writable-property-diagnostics")]
    public void PropertyFactory_RequiresAPublicDefaultConstructorAndPublicWritableCorrelationProperty()
    {
        ArgumentException constructorFailure = CreateInternalFactoryFailure<NoPublicDefaultSaga>(PropertyFactoryTypeName);
        ArgumentException propertyFailure = CreateInternalFactoryFailure<ReadOnlyCorrelationSaga>(PropertyFactoryTypeName);

        Assert.Equal(
            $"The saga {ShortName<NoPublicDefaultSaga>()} does not have a default public constructor",
            constructorFailure.Message);
        Assert.Equal(
            $"The saga {ShortName<ReadOnlyCorrelationSaga>()} does not have a writable CorrelationId property",
            propertyFailure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "property-delegate-identity-guid-freshness-and-unwrapped-failure")]
    public void PropertyFactory_CompiledDelegateAssignsGuidPreservesFreshnessAndDoesNotWrapSetterFailure()
    {
        PropertyAssignedSaga.Reset();
        object factory = CreateInternalFactory<PropertyAssignedSaga>(PropertyFactoryTypeName);
        SagaInstanceFactoryMethod<PropertyAssignedSaga> method = ReadFactoryMethod<PropertyAssignedSaga>(factory);
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();

        PropertyAssignedSaga first = method(firstId);
        PropertyAssignedSaga second = method(secondId);

        Assert.Same(method, ReadFactoryMethod<PropertyAssignedSaga>(factory));
        Assert.NotSame(first, second);
        Assert.Equal(firstId, first.CorrelationId);
        Assert.Equal(secondId, second.CorrelationId);
        Assert.Equal(2, PropertyAssignedSaga.ConstructorCount);
        Assert.Equal(2, PropertyAssignedSaga.SetterCount);

        SagaInstanceFactoryMethod<ThrowingSetterSaga> throwing =
            ReadFactoryMethod<ThrowingSetterSaga>(CreateInternalFactory<ThrowingSetterSaga>(PropertyFactoryTypeName));
        FactoryFailureException exception = Assert.Throws<FactoryFailureException>(() => throwing(NewId.NextGuid()));
        Assert.Same(ThrowingSetterSaga.ExpectedFailure, exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "metadata-prefers-guid-constructor-and-caches-one-delegate")]
    public void MetadataCache_PrefersTheGuidConstructorAndSharesOneDelegateAcrossDefaultFactories()
    {
        GuidPreferenceSaga.Reset();
        SagaInstanceFactoryMethod<GuidPreferenceSaga> firstMethod = GetCachedFactoryMethod<GuidPreferenceSaga>();
        SagaInstanceFactoryMethod<GuidPreferenceSaga> secondMethod = GetCachedFactoryMethod<GuidPreferenceSaga>();
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        var firstFactory = new DefaultSagaFactory<GuidPreferenceSaga, FactoryMessage>();
        var secondFactory = new DefaultSagaFactory<GuidPreferenceSaga, FactoryMessage>();

        GuidPreferenceSaga first = firstFactory.Create(CreateContext(firstId));
        GuidPreferenceSaga second = secondFactory.Create(CreateContext(secondId));

        Assert.Same(firstMethod, secondMethod);
        Assert.NotSame(first, second);
        Assert.Equal(firstId, first.CorrelationId);
        Assert.Equal(secondId, second.CorrelationId);
        Assert.Equal(2, GuidPreferenceSaga.GuidConstructorCount);
        Assert.Equal(0, GuidPreferenceSaga.DefaultConstructorCount);
        Assert.Equal(0, GuidPreferenceSaga.SetterCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "metadata-property-fallback-and-actionable-unsupported-diagnostic")]
    public void MetadataCache_FallsBackToThePropertyFactoryAndReportsUnsupportedSagaConfiguration()
    {
        PropertyFallbackSaga.Reset();
        Guid correlationId = NewId.NextGuid();

        PropertyFallbackSaga result = GetCachedFactoryMethod<PropertyFallbackSaga>()(correlationId);

        Assert.Equal(correlationId, result.CorrelationId);
        Assert.Equal(1, PropertyFallbackSaga.ConstructorCount);
        Assert.Equal(1, PropertyFallbackSaga.SetterCount);

        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() =>
            GetMetadataFactoryProperty<UnsupportedSaga>().GetValue(null));
        ConfigurationException exception = Assert.IsType<ConfigurationException>(invocation.InnerException);
        Assert.Equal(
            $"Saga for bus 'unknown': The saga {ShortName<UnsupportedSaga>()} must have either a public constructor "
            + "with one Guid parameter, or a public parameterless constructor and a writable CorrelationId property. "
            + "Correct the named configuration before starting the host.",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-factory-context-before-next-pre-effect-guards")]
    public void DefaultFactory_ValidatesContextThenNextBeforeCreatingASaga()
    {
        GuardSaga.Reset();
        var factory = new DefaultSagaFactory<GuardSaga, FactoryMessage>();
        ConsumeContext<FactoryMessage> context = CreateContext();

        ArgumentNullException createContext = Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
        ArgumentNullException sendContext = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = factory.SendAsync(null!, null!);
        });
        ArgumentNullException next = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = factory.SendAsync(context, null!);
        });

        Assert.Equal("context", createContext.ParamName);
        Assert.Equal("context", sendContext.ParamName);
        Assert.Equal("next", next.ParamName);
        Assert.Equal(0, GuardSaga.ConstructorCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-factory-correlation-failure-before-construction-or-pipeline")]
    public void DefaultFactory_MissingCorrelationReportsTypedFailureBeforeConstructionOrPipeline()
    {
        MissingCorrelationSaga.Reset();
        var factory = new DefaultSagaFactory<MissingCorrelationSaga, FactoryMessage>();
        var pipe = new RecordingPipe<MissingCorrelationSaga>();
        ConsumeContext<FactoryMessage> context = CreateContext();

        SagaException createFailure = Assert.Throws<SagaException>(() => factory.Create(context));
        SagaException sendFailure = Assert.Throws<SagaException>(() =>
        {
            _ = factory.SendAsync(context, pipe);
        });

        Assert.Equal(createFailure.Message, sendFailure.Message);
        Assert.Contains("correlationId was not present and the saga could not be created", createFailure.Message,
            StringComparison.Ordinal);
        Assert.Same(typeof(MissingCorrelationSaga), createFailure.SagaType);
        Assert.Same(typeof(FactoryMessage), createFailure.MessageType);
        Assert.Null(createFailure.CorrelationId);
        Assert.Same(typeof(MissingCorrelationSaga), sendFailure.SagaType);
        Assert.Same(typeof(FactoryMessage), sendFailure.MessageType);
        Assert.Null(sendFailure.CorrelationId);
        Assert.Equal(0, MissingCorrelationSaga.ConstructorCount);
        Assert.Equal(0, pipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-send-proxy-saga-source-context-and-task-identity")]
    public void DefaultSend_WrapsTheExactCreatedSagaAndSourceContextAndReturnsTheExactTask()
    {
        ProxySaga.Reset();
        Guid correlationId = NewId.NextGuid();
        var message = new FactoryMessage();
        ConsumeContext<FactoryMessage> source = CreateContext(correlationId, message);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipe = new RecordingPipe<ProxySaga> { Result = completion.Task };
        var factory = new DefaultSagaFactory<ProxySaga, FactoryMessage>();

        Task result = factory.SendAsync(source, pipe);

        Assert.Same(completion.Task, result);
        Assert.Equal(1, pipe.Count);
        DefaultSagaConsumeContext<ProxySaga, FactoryMessage> proxy =
            Assert.IsType<DefaultSagaConsumeContext<ProxySaga, FactoryMessage>>(pipe.Context);
        Assert.Same(ProxySaga.LastCreated, proxy.Saga);
        Assert.Equal(correlationId, proxy.Saga.CorrelationId);
        Assert.Equal(correlationId, proxy.CorrelationId);
        Assert.Same(message, proxy.Message);
        FieldInfo sourceField = typeof(ConsumeContextScope<FactoryMessage>).GetField(
            "_context", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.NotNull(sourceField);
        Assert.Same(source, sourceField.GetValue(proxy));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-send-faulted-task-and-synchronous-exception-identity")]
    public async Task DefaultSend_PreservesFaultedTaskAndDoesNotWrapSynchronousPipelineExceptionAsync()
    {
        var factory = new DefaultSagaFactory<PipelineSaga, FactoryMessage>();
        var expectedFault = new PipelineFailureException("faulted downstream task");
        Task faultedTask = Task.FromException(expectedFault);
        var faultingPipe = new RecordingPipe<PipelineSaga> { Result = faultedTask };

        Task result = factory.SendAsync(CreateContext(NewId.NextGuid()), faultingPipe);
        PipelineFailureException observedFault = await Assert.ThrowsAsync<PipelineFailureException>(() => result);

        Assert.Same(faultedTask, result);
        Assert.Same(expectedFault, observedFault);
        Assert.Equal(1, faultingPipe.Count);

        var expectedSynchronous = new PipelineFailureException("synchronous downstream failure");
        var throwingPipe = new RecordingPipe<PipelineSaga> { SynchronousException = expectedSynchronous };
        PipelineFailureException observedSynchronous = Assert.Throws<PipelineFailureException>(() =>
        {
            _ = factory.SendAsync(CreateContext(NewId.NextGuid()), throwingPipe);
        });

        Assert.Same(expectedSynchronous, observedSynchronous);
        Assert.Equal(1, throwingPipe.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-send-null-task-stable-diagnostic-after-one-pipeline-call")]
    public void DefaultSend_RejectsANullDownstreamTaskWithAStableDiagnostic()
    {
        var factory = new DefaultSagaFactory<PipelineSaga, FactoryMessage>();
        var pipe = new RecordingPipe<PipelineSaga> { Result = null };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = factory.SendAsync(CreateContext(NewId.NextGuid()), pipe);
        });

        Assert.Equal("The saga pipeline returned no task.", exception.Message);
        Assert.Equal(1, pipe.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "default-created-debug-does-not-prevent-owned-downstream-progress")]
    public async Task DefaultSend_CreatedDebugDoesNotPreventDownstreamProgressAsync(bool loggerThrows)
    {
        const string template = "SAGA:{SagaType}:{CorrelationId} Created {MessageType}";
        var expectedFailure = new IOException("Default saga Created diagnostic failure");
        var logger = new CreatedDiagnosticLogger(template, loggerThrows ? expectedFailure : null);
        Guid correlationId = NewId.NextGuid();
        var message = new FactoryMessage();
        ConsumeContext<FactoryMessage> source = CreateContext(correlationId, message);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipe = new RecordingPipe<PipelineSaga> { Result = completion.Task };
        var factory = new DefaultSagaFactory<PipelineSaga, FactoryMessage>();
        ILogContext? previous = LogContext.Current;
        Task? returned = null;
        Task? operation = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            operation = InvokeAsync();
            CreatedDiagnosticLogger.Entry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Equal(LogLevel.Debug, selected.Level);
            Assert.Equal(correlationId, selected.CorrelationId);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (loggerThrows)
                Assert.Same(expectedFailure, logger.ThrownFailure);
            else
            {
                Assert.Null(logger.ThrownFailure);
                Assert.False(operation.IsCompleted);
                Assert.Same(completion.Task, returned);
            }

            completion.TrySetResult();
            Exception? failure = await Record.ExceptionAsync(() =>
                operation.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
            Assert.IsNotType<TimeoutException>(failure);
            Assert.True(operation.IsCompleted);
            Assert.True(completion.Task.IsCompletedSuccessfully);
            Assert.Null(failure);
            Assert.True(operation.IsCompletedSuccessfully);
            Assert.Same(completion.Task, returned);
            Assert.Equal(1, pipe.Count);
            DefaultSagaConsumeContext<PipelineSaga, FactoryMessage> actual =
                Assert.IsType<DefaultSagaConsumeContext<PipelineSaga, FactoryMessage>>(pipe.Context);
            Assert.Equal(correlationId, actual.Saga.CorrelationId);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Same(message, actual.Message);
            Assert.Same(source.Advanced().ReceiveContext, actual.ReceiveContext);
            Assert.Same(source.Advanced().SerializerContext, actual.SerializerContext);
        }
        finally
        {
            completion.TrySetResult();
            try
            {
                await ObserveCreatedDiagnosticTaskAsync(completion.Task);
            }
            finally
            {
                try
                {
                    if (operation is not null)
                        await ObserveCreatedDiagnosticTaskAsync(operation);
                }
                finally
                {
                    LogContext.Current = previous;
                }
            }
        }

        async Task InvokeAsync()
        {
            returned = factory.SendAsync(source, pipe);
            await returned;
        }
    }

    private static async Task ObserveCreatedDiagnosticTaskAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception failure) when (failure is not TimeoutException && (task.IsFaulted || task.IsCanceled))
        {
        }
    }

    private sealed class CreatedDiagnosticLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, Guid? CorrelationId);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> values = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            string? template = values.FirstOrDefault(value => value.Key == "{OriginalFormat}").Value as string;
            object? correlation = values.FirstOrDefault(value => value.Key == "CorrelationId").Value;
            Entries.Add(new Entry(logLevel, template, correlation is Guid id ? id : null));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }

    private static void AssertInternalFactorySurface(string typeName)
    {
        Type factory = GetOpenInternalType(typeName);
        Type saga = Assert.Single(factory.GetGenericArguments());

        Assert.True(factory.IsNotPublic && factory.IsClass && factory.IsSealed);
        Assert.False(factory.IsAbstract);
        AssertReferenceConstraint(saga, typeof(ISaga));

        ConstructorInfo constructor = Assert.Single(factory.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(constructor.GetParameters());
        PropertyInfo property = Assert.Single(factory.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal("FactoryMethod", property.Name);
        Assert.Equal(typeof(SagaInstanceFactoryMethod<>).MakeGenericType(saga), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.True(property.GetMethod.IsPublic);
        Assert.Null(property.SetMethod);
    }

    private static void AssertReferenceConstraint(Type parameter, params Type[] typeConstraints)
    {
        GenericParameterAttributes specialConstraints =
            parameter.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint, specialConstraints);
        Assert.Equal(typeConstraints, parameter.GetGenericParameterConstraints());
    }

    private static object CreateInternalFactory<TSaga>(string typeName)
        where TSaga : class, ISaga =>
        Activator.CreateInstance(GetOpenInternalType(typeName).MakeGenericType(typeof(TSaga)))!;

    private static ArgumentException CreateInternalFactoryFailure<TSaga>(string typeName)
        where TSaga : class, ISaga
    {
        TargetInvocationException invocation = Assert.Throws<TargetInvocationException>(() =>
            CreateInternalFactory<TSaga>(typeName));

        return Assert.IsType<ArgumentException>(invocation.InnerException);
    }

    private static SagaInstanceFactoryMethod<TSaga> ReadFactoryMethod<TSaga>(object factory)
        where TSaga : class, ISaga
    {
        PropertyInfo property = factory.GetType().GetProperty(
            "FactoryMethod", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        Assert.NotNull(property);

        return Assert.IsType<SagaInstanceFactoryMethod<TSaga>>(property.GetValue(factory));
    }

    private static SagaInstanceFactoryMethod<TSaga> GetCachedFactoryMethod<TSaga>()
        where TSaga : class, ISaga =>
        Assert.IsType<SagaInstanceFactoryMethod<TSaga>>(GetMetadataFactoryProperty<TSaga>().GetValue(null));

    private static PropertyInfo GetMetadataFactoryProperty<TSaga>()
        where TSaga : class, ISaga
    {
        Type cache = GetOpenInternalType(MetadataCacheTypeName).MakeGenericType(typeof(TSaga));
        PropertyInfo property = cache.GetProperty(
            "FactoryMethod", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        Assert.NotNull(property);
        return property;
    }

    private static Type GetOpenInternalType(string name)
    {
        Type? type = typeof(DefaultSagaFactory<,>).Assembly.GetType(name, throwOnError: false);
        Assert.NotNull(type);
        return type;
    }

    private static string ShortName<T>() => TypeCache.GetShortName(typeof(T));

    private static string ConstructorDiagnostic<T>() =>
        "The saga does not have a public constructor with a single Guid correlationId parameter: " + ShortName<T>();

    private static ConsumeContext<FactoryMessage> CreateContext(
        Guid? correlationId = null,
        FactoryMessage? message = null) =>
        InMemoryOutboxTestContextFactory.Create(
            message ?? new FactoryMessage(),
            TestContext.Current.CancellationToken,
            correlationId: correlationId);

    private sealed class RecordingPipe<TSaga> : IPipe<SagaConsumeContext<TSaga, FactoryMessage>>
        where TSaga : class, ISaga
    {
        private int _count;

        public SagaConsumeContext<TSaga, FactoryMessage>? Context { get; private set; }
        public int Count => Volatile.Read(ref _count);
        public Task? Result { get; init; } = Task.CompletedTask;
        public Exception? SynchronousException { get; init; }

        public Task SendAsync(SagaConsumeContext<TSaga, FactoryMessage> context)
        {
            Context = context;
            Interlocked.Increment(ref _count);
            if (SynchronousException is not null)
                throw SynchronousException;

            return Result!;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    public sealed class FactoryMessage;

    public interface IContractSaga : ISaga;

    public abstract class AbstractGuidSaga : ISaga
    {
        protected AbstractGuidSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }
    }

    public abstract class AbstractPropertySaga : ISaga
    {
        protected AbstractPropertySaga()
        {
        }

        public Guid CorrelationId { get; set; }
    }

    public sealed class ParameterlessOnlySaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class PrivateGuidSaga : ISaga
    {
        public PrivateGuidSaga()
        {
        }

        private PrivateGuidSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }
    }

    public sealed class ConstructorAssignedSaga : ISaga
    {
        private static int _constructorCount;

        public ConstructorAssignedSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _constructorCount);
            CorrelationId = correlationId;
        }

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public Guid CorrelationId { get; set; }

        public static void Reset() => Volatile.Write(ref _constructorCount, 0);
    }

    public sealed class ThrowingConstructorSaga : ISaga
    {
        public static readonly FactoryFailureException ExpectedFailure = new("constructor failure");

        public ThrowingConstructorSaga(Guid correlationId) => throw ExpectedFailure;

        public Guid CorrelationId { get; set; }
    }

    public sealed class NoPublicDefaultSaga : ISaga
    {
        public NoPublicDefaultSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }
    }

    public sealed class ReadOnlyCorrelationSaga : ISaga
    {
        private Guid _correlationId;

        public Guid CorrelationId => _correlationId;

        Guid ISaga.CorrelationId
        {
            get => _correlationId;
            set => _correlationId = value;
        }
    }

    public sealed class PropertyAssignedSaga : ISaga
    {
        private static int _constructorCount;
        private static int _setterCount;
        private Guid _correlationId;

        public PropertyAssignedSaga() => Interlocked.Increment(ref _constructorCount);

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public static int SetterCount => Volatile.Read(ref _setterCount);

        public Guid CorrelationId
        {
            get => _correlationId;
            set
            {
                Interlocked.Increment(ref _setterCount);
                _correlationId = value;
            }
        }

        public static void Reset()
        {
            Volatile.Write(ref _constructorCount, 0);
            Volatile.Write(ref _setterCount, 0);
        }
    }

    public sealed class ThrowingSetterSaga : ISaga
    {
        public static readonly FactoryFailureException ExpectedFailure = new("setter failure");

        public Guid CorrelationId
        {
            get => Guid.Empty;
            set => throw ExpectedFailure;
        }
    }

    public sealed class GuidPreferenceSaga : ISaga
    {
        private static int _defaultConstructorCount;
        private static int _guidConstructorCount;
        private static int _setterCount;
        private Guid _correlationId;

        public GuidPreferenceSaga() => Interlocked.Increment(ref _defaultConstructorCount);

        public GuidPreferenceSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _guidConstructorCount);
            _correlationId = correlationId;
        }

        public static int DefaultConstructorCount => Volatile.Read(ref _defaultConstructorCount);
        public static int GuidConstructorCount => Volatile.Read(ref _guidConstructorCount);
        public static int SetterCount => Volatile.Read(ref _setterCount);

        public Guid CorrelationId
        {
            get => _correlationId;
            set
            {
                Interlocked.Increment(ref _setterCount);
                _correlationId = value;
            }
        }

        public static void Reset()
        {
            Volatile.Write(ref _defaultConstructorCount, 0);
            Volatile.Write(ref _guidConstructorCount, 0);
            Volatile.Write(ref _setterCount, 0);
        }
    }

    public sealed class PropertyFallbackSaga : ISaga
    {
        private static int _constructorCount;
        private static int _setterCount;
        private Guid _correlationId;

        public PropertyFallbackSaga() => Interlocked.Increment(ref _constructorCount);

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public static int SetterCount => Volatile.Read(ref _setterCount);

        public Guid CorrelationId
        {
            get => _correlationId;
            set
            {
                Interlocked.Increment(ref _setterCount);
                _correlationId = value;
            }
        }

        public static void Reset()
        {
            Volatile.Write(ref _constructorCount, 0);
            Volatile.Write(ref _setterCount, 0);
        }
    }

    public sealed class UnsupportedSaga : ISaga
    {
        private UnsupportedSaga()
        {
        }

        public Guid CorrelationId { get; set; }
    }

    public sealed class GuardSaga : ISaga
    {
        private static int _constructorCount;

        public GuardSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _constructorCount);
            CorrelationId = correlationId;
        }

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public Guid CorrelationId { get; set; }

        public static void Reset() => Volatile.Write(ref _constructorCount, 0);
    }

    public sealed class MissingCorrelationSaga : ISaga
    {
        private static int _constructorCount;

        public MissingCorrelationSaga(Guid correlationId)
        {
            Interlocked.Increment(ref _constructorCount);
            CorrelationId = correlationId;
        }

        public static int ConstructorCount => Volatile.Read(ref _constructorCount);
        public Guid CorrelationId { get; set; }

        public static void Reset() => Volatile.Write(ref _constructorCount, 0);
    }

    public sealed class ProxySaga : ISaga
    {
        public ProxySaga(Guid correlationId)
        {
            CorrelationId = correlationId;
            LastCreated = this;
        }

        public static ProxySaga? LastCreated { get; private set; }
        public Guid CorrelationId { get; set; }

        public static void Reset() => LastCreated = null;
    }

    public sealed class PipelineSaga : ISaga
    {
        public PipelineSaga(Guid correlationId) => CorrelationId = correlationId;

        public Guid CorrelationId { get; set; }
    }

    public sealed class FactoryFailureException(string message) : Exception(message);

    public sealed class PipelineFailureException(string message) : Exception(message);
}
