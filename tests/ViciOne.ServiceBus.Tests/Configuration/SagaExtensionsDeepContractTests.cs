using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "exact-two-method-public-surface")]
    public void PublicSurface_ContainsExactlyTheSagaAndConnectSagaGenericMethods()
    {
        MethodInfo[] methods = typeof(SagaExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        Assert.Collection(
            methods.OrderBy(method => method.Name),
            method =>
            {
                Assert.Equal(nameof(SagaExtensions.ConnectSaga), method.Name);
                Assert.True(method.IsGenericMethodDefinition);
                Assert.Equal(typeof(ConnectHandle), method.ReturnType);
                Type sagaType = Assert.Single(method.GetGenericArguments());
                ParameterInfo[] parameters = method.GetParameters();
                Assert.Equal(typeof(IConsumePipeConnector), parameters[0].ParameterType);
                Assert.Equal(typeof(ISagaRepository<>).MakeGenericType(sagaType), parameters[1].ParameterType);
                Assert.Equal(
                    typeof(IPipeSpecification<>).MakeGenericType(
                        typeof(SagaConsumeContext<>).MakeGenericType(sagaType)).MakeArrayType(),
                    parameters[2].ParameterType);
                Assert.NotNull(parameters[2].GetCustomAttribute<ParamArrayAttribute>());
            },
            method =>
            {
                Assert.Equal(nameof(SagaExtensions.Saga), method.Name);
                Assert.True(method.IsGenericMethodDefinition);
                Assert.Equal(typeof(void), method.ReturnType);
                Type sagaType = Assert.Single(method.GetGenericArguments());
                ParameterInfo[] parameters = method.GetParameters();
                Assert.Equal(typeof(IReceiveEndpointConfigurator), parameters[0].ParameterType);
                Assert.Equal(typeof(ISagaRepository<>).MakeGenericType(sagaType), parameters[1].ParameterType);
                Assert.Equal(
                    typeof(Action<>).MakeGenericType(typeof(ISagaConfigurator<>).MakeGenericType(sagaType)),
                    parameters[2].ParameterType);
            });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "required-input-guard-order-and-owned-pipe-elements")]
    public void RequiredInputs_AreRejectedInSignatureOrderBeforeConnectorEffects()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out _);
        var repository = new InMemorySagaRepository<ExtensionSaga>();
        IConsumePipeConnector connector = ConsumePipe(out ConsumePipeRecorder consumePipe);

        AssertParameter("configurator", () => SagaExtensions.Saga<ExtensionSaga>(null!, null!));
        AssertParameter("sagaRepository", () => SagaExtensions.Saga<ExtensionSaga>(endpoint, null!));

        AssertParameter("connector", () => SagaExtensions.ConnectSaga<ExtensionSaga>(null!, null!, null!));
        AssertParameter("sagaRepository", () => SagaExtensions.ConnectSaga<ExtensionSaga>(connector, null!, null!));
        AssertParameter("pipeSpecifications", () => SagaExtensions.ConnectSaga(connector, repository,
            (IPipeSpecification<SagaConsumeContext<ExtensionSaga>>[])null!));

        var first = new RecordingPipeSpecification("first", []);
        AssertParameter("pipeSpecification", () => SagaExtensions.ConnectSaga(connector, repository, first, null!));
        Assert.Equal(0, consumePipe.ConnectCalls);
        Assert.Equal(0, first.ApplyCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "callback-and-endpoint-specification-identity-order")]
    public void Saga_PassesOneConfiguratorToTheCallbackBeforeAddingTheSameEndpointSpecification()
    {
        using var logScope = new DebugLogScope();
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        var repository = new InMemorySagaRepository<ExtensionSaga>();
        ISagaConfigurator<ExtensionSaga>? callbackConfigurator = null;

        endpoint.Saga<ExtensionSaga>(repository, configurator =>
        {
            recorder.Events.Add("callback");
            callbackConfigurator = configurator;
        });

        Assert.Equal(["callback", "add-endpoint-specification"], recorder.Events);
        IReceiveEndpointSpecification endpointSpecification = Assert.Single(recorder.Specifications);
        Assert.IsType<SagaConfigurator<ExtensionSaga>>(endpointSpecification);
        Assert.Same(callbackConfigurator, endpointSpecification);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "callback-failure-does-not-mutate-endpoint")]
    public void Saga_WhenTheCallbackFails_DoesNotAddAnEndpointSpecification()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        var repository = new InMemorySagaRepository<ExtensionSaga>();
        var failure = new InvalidOperationException("callback failed");

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
            endpoint.Saga<ExtensionSaga>(repository, _ => throw failure));

        Assert.Same(failure, thrown);
        Assert.Empty(recorder.Events);
        Assert.Empty(recorder.Specifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "pipe-order-identity-and-exact-connect-forwarding")]
    public void ConnectSaga_PreservesSpecificationOrderAndIdentityAndForwardsExactCollaboratorsAndHandle()
    {
        using var logScope = new DebugLogScope();
        var repository = new InMemorySagaRepository<ExtensionSaga>();
        IConsumePipeConnector consumePipe = ConsumePipe(out _);
        var childHandle = new RecordingConnectHandle();
        var messageSpecification = MessageSpecification(out MessageSpecificationRecorder specificationRecorder);
        var messageConnector = new RecordingMessageConnector(messageSpecification, childHandle);
        var first = new RecordingPipeSpecification("first", []);
        var second = new RecordingPipeSpecification("second", []);

        SagaConnector<ExtensionSaga> sagaConnector = Assert.IsType<SagaConnector<ExtensionSaga>>(
            SagaConnectorCache<ExtensionSaga>.Connector);
        FieldInfo connectorsField = typeof(SagaConnector<ExtensionSaga>).GetField("_connectors",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var connectors = Assert.IsType<List<ISagaMessageConnector<ExtensionSaga>>>(connectorsField.GetValue(sagaConnector));
        ISagaMessageConnector<ExtensionSaga>[] savedConnectors = [.. connectors];

        try
        {
            connectors.Clear();
            connectors.Add(messageConnector);

            AssertParameter("pipeSpecification", () => consumePipe.ConnectSaga(repository, first, null!));
            Assert.Empty(specificationRecorder.PipeSpecifications);
            Assert.Equal(0, messageConnector.ConnectCalls);

            ConnectHandle returned = consumePipe.ConnectSaga(repository, first, second);

            Assert.Collection(
                specificationRecorder.PipeSpecifications,
                specification => Assert.Same(first, specification),
                specification => Assert.Same(second, specification));
            Assert.Equal(1, messageConnector.ConnectCalls);
            Assert.Same(consumePipe, messageConnector.ConsumePipe);
            Assert.Same(repository, messageConnector.Repository);
            var forwardedSpecification = Assert.IsType<SagaSpecification<ExtensionSaga>>(messageConnector.Specification);
            FieldInfo messageTypesField = typeof(SagaSpecification<ExtensionSaga>).GetField("_messageTypes",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var messageTypes = Assert.IsAssignableFrom<IReadOnlyDictionary<Type, ISagaMessageSpecification<ExtensionSaga>>>(
                messageTypesField.GetValue(forwardedSpecification));
            Assert.Same(messageSpecification, Assert.Single(messageTypes).Value);

            var composite = Assert.IsType<MultipleConnectHandle>(returned);
            FieldInfo handlesField = typeof(MultipleConnectHandle).GetField("_handles",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            ConnectHandle[] handles = Assert.IsType<ConnectHandle[]>(handlesField.GetValue(composite));
            Assert.Collection(handles, handle => Assert.Same(childHandle, handle));

            returned.Disconnect();
            Assert.Equal(1, childHandle.DisconnectCalls);
        }
        finally
        {
            connectors.Clear();
            connectors.AddRange(savedConnectors);
        }
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static IReceiveEndpointConfigurator Endpoint(out EndpointRecorder recorder)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        recorder = (EndpointRecorder)(object)endpoint;
        return endpoint;
    }

    private static IConsumePipeConnector ConsumePipe(out ConsumePipeRecorder recorder)
    {
        IConsumePipeConnector connector = DispatchProxy.Create<IConsumePipeConnector, ConsumePipeRecorder>();
        recorder = (ConsumePipeRecorder)(object)connector;
        return connector;
    }

    private static ISagaMessageSpecification<ExtensionSaga> MessageSpecification(out MessageSpecificationRecorder recorder)
    {
        ISagaMessageSpecification<ExtensionSaga> specification =
            DispatchProxy.Create<ISagaMessageSpecification<ExtensionSaga>, MessageSpecificationRecorder>();
        recorder = (MessageSpecificationRecorder)(object)specification;
        return specification;
    }

    private class EndpointRecorder : DispatchProxy
    {
        public List<string> Events { get; } = [];

        public List<IReceiveEndpointSpecification> Specifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IReceiveEndpointConfigurator.AddEndpointSpecification))
            {
                Events.Add("add-endpoint-specification");
                Specifications.Add((IReceiveEndpointSpecification)args![0]!);
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class ConsumePipeRecorder : DispatchProxy
    {
        public int ConnectCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ConnectCalls++;
            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class MessageSpecificationRecorder : DispatchProxy
    {
        public List<IPipeSpecification<SagaConsumeContext<ExtensionSaga>>> PipeSpecifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_MessageType" => typeof(ExtensionMessage),
                nameof(ISagaConfigurationObserverConnector.ConnectSagaConfigurationObserver) => new RecordingConnectHandle(),
                nameof(IPipeConfigurator<SagaConsumeContext<ExtensionSaga>>.AddPipeSpecification) => Add(args![0]!),
                nameof(ISpecification.Validate) => Array.Empty<ValidationResult>(),
                _ => throw new NotSupportedException(targetMethod.Name)
            };
        }

        private object? Add(object specification)
        {
            PipeSpecifications.Add((IPipeSpecification<SagaConsumeContext<ExtensionSaga>>)specification);
            return null;
        }
    }

    private sealed class RecordingMessageConnector(
        ISagaMessageSpecification<ExtensionSaga> messageSpecification,
        ConnectHandle handle) : ISagaMessageConnector<ExtensionSaga>
    {
        public Type MessageType => typeof(ExtensionMessage);

        public int ConnectCalls { get; private set; }

        public IConsumePipeConnector? ConsumePipe { get; private set; }

        public ISagaRepository<ExtensionSaga>? Repository { get; private set; }

        public ISagaSpecification<ExtensionSaga>? Specification { get; private set; }

        public ISagaMessageSpecification<ExtensionSaga> CreateSagaMessageSpecification() => messageSpecification;

        public ConnectHandle ConnectSaga(IConsumePipeConnector consumePipe, ISagaRepository<ExtensionSaga> repository,
            ISagaSpecification<ExtensionSaga> specification)
        {
            ConnectCalls++;
            ConsumePipe = consumePipe;
            Repository = repository;
            Specification = specification;
            return handle;
        }
    }

    private sealed class RecordingPipeSpecification(string name, List<string> applicationOrder) :
        IPipeSpecification<SagaConsumeContext<ExtensionSaga>>
    {
        public int ApplyCalls { get; private set; }

        public void Apply(IPipeBuilder<SagaConsumeContext<ExtensionSaga>> builder)
        {
            ApplyCalls++;
            applicationOrder.Add(name);
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class RecordingConnectHandle : ConnectHandle
    {
        public int DisconnectCalls { get; private set; }

        public void Disconnect() => DisconnectCalls++;

        public void Dispose() => Disconnect();
    }

    private sealed class DebugLogScope : IDisposable
    {
        readonly ILogContext? _previous = LogContext.Current;

        public DebugLogScope()
        {
            LogContext.ConfigureCurrentLogContext(new AlwaysEnabledLogger());
        }

        public void Dispose()
        {
            LogContext.Current = _previous;
        }
    }

    private sealed class AlwaysEnabledLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    public sealed record ExtensionMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ExtensionSaga : ISaga, IInitiatedBy<ExtensionMessage>
    {
        public Guid CorrelationId { get; set; }

        public Task Consume(ConsumeContext<ExtensionMessage> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<ExtensionMessage> context)
        {
            throw new NotImplementedException();
        }
    }
}
