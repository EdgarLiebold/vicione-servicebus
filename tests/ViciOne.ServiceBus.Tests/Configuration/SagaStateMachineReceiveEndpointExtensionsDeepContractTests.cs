using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaStateMachineReceiveEndpointExtensionsDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "state-machine-exact-two-method-public-surface")]
    public void PublicSurface_ContainsExactlyTheStateMachineSagaAndConnectMethods()
    {
        MethodInfo[] methods = typeof(SagaStateMachineReceiveEndpointExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.Name)
            .ToArray();

        Assert.Collection(
            methods,
            method => AssertSurface(method, nameof(SagaStateMachineReceiveEndpointExtensions.ConnectStateMachineSaga),
                typeof(ConnectHandle), typeof(IConsumePipeConnector)),
            method => AssertSurface(method, nameof(SagaStateMachineReceiveEndpointExtensions.StateMachineSaga),
                typeof(void), typeof(IReceiveEndpointConfigurator)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "state-machine-required-input-receiver-first-guard-order")]
    public void RequiredInputs_AreRejectedInSignatureOrderBeforeReceiverEffects()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder endpointRecorder);
        IConsumePipeConnector bus = ConsumePipe(new RecordingConnectHandle(), [], out ConsumePipeRecorder busRecorder);
        var stateMachine = new ExtensionMachine();
        var repository = new InMemorySagaRepository<ExtensionState>();

        AssertParameter("configurator", () =>
            SagaStateMachineReceiveEndpointExtensions.StateMachineSaga<ExtensionState>(null!, null!, null!));
        AssertParameter("stateMachine", () =>
            SagaStateMachineReceiveEndpointExtensions.StateMachineSaga<ExtensionState>(endpoint, null!, null!));
        AssertParameter("repository", () =>
            SagaStateMachineReceiveEndpointExtensions.StateMachineSaga(endpoint, stateMachine, null!));

        AssertParameter("bus", () =>
            SagaStateMachineReceiveEndpointExtensions.ConnectStateMachineSaga<ExtensionState>(null!, null!, null!));
        AssertParameter("stateMachine", () =>
            SagaStateMachineReceiveEndpointExtensions.ConnectStateMachineSaga<ExtensionState>(bus, null!, null!));
        AssertParameter("repository", () =>
            SagaStateMachineReceiveEndpointExtensions.ConnectStateMachineSaga(bus, stateMachine, null!));

        Assert.Empty(endpointRecorder.Events);
        Assert.Empty(endpointRecorder.Specifications);
        Assert.Equal(0, busRecorder.ConnectCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "state-machine-callback-specification-identity-order")]
    public void StateMachineSaga_PassesOneConfiguratorToTheCallbackBeforeAddingTheSameSpecification()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        var stateMachine = new ExtensionMachine();
        var repository = new InMemorySagaRepository<ExtensionState>();
        ISagaConfigurator<ExtensionState>? callbackConfigurator = null;

        endpoint.StateMachineSaga(stateMachine, repository, configurator =>
        {
            recorder.Events.Add("callback");
            callbackConfigurator = configurator;
        });

        Assert.Equal(["callback", "add-endpoint-specification"], recorder.Events);
        IReceiveEndpointSpecification specification = Assert.Single(recorder.Specifications);
        Assert.Same(callbackConfigurator, specification);
        Assert.Same(repository, ReadField(specification, "_repository"));
        object connector = ReadField(specification, "_connector");
        Assert.Same(stateMachine, ReadField(connector, "_stateMachine"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "state-machine-callback-failure-isolation")]
    public void Callbacks_WhenTheyFail_DoNotMutateTheEndpointOrConnectTheBus()
    {
        IReceiveEndpointConfigurator endpoint = Endpoint(out EndpointRecorder recorder);
        IConsumePipeConnector bus = ConsumePipe(new RecordingConnectHandle(), [], out ConsumePipeRecorder busRecorder);
        var stateMachine = new ExtensionMachine();
        var repository = new InMemorySagaRepository<ExtensionState>();
        var failure = new InvalidOperationException("callback failed");

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
            endpoint.StateMachineSaga(stateMachine, repository, _ => throw failure));
        InvalidOperationException connectThrown = Assert.Throws<InvalidOperationException>(() =>
            bus.ConnectStateMachineSaga(stateMachine, repository, _ => throw failure));

        Assert.Same(failure, thrown);
        Assert.Same(failure, connectThrown);
        Assert.Empty(recorder.Events);
        Assert.Empty(recorder.Specifications);
        Assert.Equal(0, busRecorder.ConnectCalls);
        Assert.Empty(busRecorder.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTION-EXTENSIONS", "state-machine-connect-exact-forwarding-and-handle")]
    public void ConnectStateMachineSaga_ForwardsExactCollaboratorsAndReturnsTheOwnedConnectHandle()
    {
        var events = new List<string>();
        var childHandle = new RecordingConnectHandle();
        IConsumePipeConnector bus = ConsumePipe(childHandle, events, out ConsumePipeRecorder recorder);
        var stateMachine = new ExtensionMachine();
        var repository = new InMemorySagaRepository<ExtensionState>();
        ISagaConfigurator<ExtensionState>? callbackSpecification = null;

        ConnectHandle returned = bus.ConnectStateMachineSaga(stateMachine, repository, specification =>
        {
            events.Add("callback");
            callbackSpecification = specification;
        });

        Assert.Equal(["callback", "connect-consume-pipe"], events);
        Assert.Equal(1, recorder.ConnectCalls);
        Assert.Equal(typeof(ExtensionMessage), recorder.MessageType);
        Assert.NotNull(recorder.ConnectedPipe);
        Assert.True(ContainsReference(recorder.ConnectedPipe, stateMachine));
        Assert.True(ContainsReference(recorder.ConnectedPipe, repository));
        Assert.Same(stateMachine, ReadField(callbackSpecification!, "_stateMachine"));

        var composite = Assert.IsType<MultipleConnectHandle>(returned);
        ConnectHandle[] handles = Assert.IsType<ConnectHandle[]>(ReadField(composite, "_handles"));
        Assert.Collection(handles, handle => Assert.Same(childHandle, handle));

        returned.Disconnect();
        Assert.Equal(1, childHandle.DisconnectCalls);
    }

    private static void AssertSurface(MethodInfo method, string expectedName, Type expectedReturnType, Type receiverType)
    {
        Assert.Equal(expectedName, method.Name);
        Assert.Equal(expectedReturnType, method.ReturnType);
        Assert.True(method.IsGenericMethodDefinition);
        Assert.NotNull(method.GetCustomAttribute<ExtensionAttribute>());

        Type instanceType = Assert.Single(method.GetGenericArguments());
        Assert.Equal(
            GenericParameterAttributes.ReferenceTypeConstraint,
            instanceType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Collection(instanceType.GetGenericParameterConstraints(), constraint =>
            Assert.Equal(typeof(ISagaStateMachineInstance), constraint));

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(receiverType, parameters[0].ParameterType);
        Assert.Equal(typeof(ISagaStateMachine<>).MakeGenericType(instanceType), parameters[1].ParameterType);
        Assert.Equal(typeof(ISagaRepository<>).MakeGenericType(instanceType), parameters[2].ParameterType);
        Assert.Equal(
            typeof(Action<>).MakeGenericType(typeof(ISagaConfigurator<>).MakeGenericType(instanceType)),
            parameters[3].ParameterType);
        Assert.True(parameters[3].IsOptional);
        Assert.Null(parameters[3].DefaultValue);
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static object ReadField(object owner, string name)
    {
        for (Type? type = owner.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null)
                return field.GetValue(owner) ?? throw new InvalidOperationException($"Field '{name}' was null.");
        }

        throw new InvalidOperationException($"Field '{name}' was not found on '{owner.GetType()}'.");
    }

    private static bool ContainsReference(object root, object expected)
    {
        var pending = new Stack<(object Value, int Depth)>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        pending.Push((root, 0));

        while (pending.TryPop(out (object Value, int Depth) current))
        {
            if (ReferenceEquals(current.Value, expected))
                return true;
            if (current.Depth >= 16 || !visited.Add(current.Value))
                continue;

            Type valueType = current.Value.GetType();
            if (valueType.IsPrimitive || valueType.IsEnum || current.Value is string or Type or MemberInfo or Delegate)
                continue;

            if (current.Value is Array array)
            {
                foreach (object? item in array)
                {
                    if (item != null)
                        pending.Push((item, current.Depth + 1));
                }
            }

            for (Type? type = valueType; type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    object? value = field.GetValue(current.Value);
                    if (value != null)
                        pending.Push((value, current.Depth + 1));
                }
            }
        }

        return false;
    }

    private static IReceiveEndpointConfigurator Endpoint(out EndpointRecorder recorder)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        recorder = (EndpointRecorder)(object)endpoint;
        return endpoint;
    }

    private static IConsumePipeConnector ConsumePipe(
        ConnectHandle handle,
        List<string> events,
        out ConsumePipeRecorder recorder)
    {
        IConsumePipeConnector connector = DispatchProxy.Create<IConsumePipeConnector, ConsumePipeRecorder>();
        recorder = (ConsumePipeRecorder)(object)connector;
        recorder.Handle = handle;
        recorder.Events = events;
        return connector;
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
        public ConnectHandle Handle { get; set; } = null!;

        public List<string> Events { get; set; } = null!;

        public int ConnectCalls { get; private set; }

        public Type? MessageType { get; private set; }

        public object? ConnectedPipe { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IConsumePipeConnector.ConnectConsumePipe))
            {
                ConnectCalls++;
                Events.Add("connect-consume-pipe");
                MessageType = Assert.Single(targetMethod.GetGenericArguments());
                ConnectedPipe = args![0];
                return Handle;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class RecordingConnectHandle : ConnectHandle
    {
        public int DisconnectCalls { get; private set; }

        public void Disconnect() => DisconnectCalls++;

        public void Dispose() => Disconnect();
    }

    public sealed record ExtensionMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ExtensionState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ExtensionMachine : ViciOneServiceBusStateMachine<ExtensionState>
    {
        public ExtensionMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Running));
        }

        public IState Running { get; } = null!;

        public IEvent<ExtensionMessage> Start { get; } = null!;
    }
}
