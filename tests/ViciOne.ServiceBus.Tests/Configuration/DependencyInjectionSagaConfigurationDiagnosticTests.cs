using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class DependencyInjectionSagaConfigurationDiagnosticTests
{
    const string Template = "Configured endpoint {Endpoint}, Saga: {SagaType}, State Machine: {StateMachineType}";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-DI-STATE-MACHINE-REGISTRATION", "owning-info-preserves-endpoint-specification-adoption")]
    public void Configure_OptionalInfoDoesNotPreventEndpointSpecificationAdoption(bool loggerThrows)
    {
        var actualMachine = new ProbeMachine();
        Assert.NotEmpty(actualMachine.Correlations);
        ISagaStateMachine<ProbeState> machine = DispatchProxy.Create<ISagaStateMachine<ProbeState>, MachineProxy>();
        var machineProxy = (MachineProxy)(object)machine;
        machineProxy.Actual = actualMachine;
        var observer = new ProbeObserver();
        ServiceProvider provider = new ServiceCollection()
            .AddSingleton(machine)
            .AddSingleton<IEventObserver<ProbeState>>(observer)
            .AddSingleton<IStateObserver<ProbeState>>(observer)
            .BuildServiceProvider();
        IRegistrationContext context = DispatchProxy.Create<IRegistrationContext, ContextProxy>();
        var contextProxy = (ContextProxy)(object)context;
        contextProxy.Provider = provider;
        IContainerSelector selector = DispatchProxy.Create<IContainerSelector, SelectorProxy>();
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointProxy>();
        var endpointProxy = (EndpointProxy)(object)endpoint;
        endpointProxy.Address = new Uri($"loopback://localhost/saga-diagnostic-{Guid.NewGuid():N}");
        var registration = new SagaStateMachineRegistration<ProbeMachine, ProbeState>(selector);
        ISagaConfigurator<ProbeState>? configured = null;
        int configureCalls = 0;
        registration.AddConfigureAction<ProbeState>((suppliedContext, configurator) =>
        {
            Assert.Same(context, suppliedContext);
            configureCalls++;
            configured = configurator;
        });
        var uniqueFailure = new IOException("unique saga endpoint Info diagnostic failure");
        var logger = new ProbeLogger(loggerThrows ? uniqueFailure : null);
        ILogContext? previous = LogContext.Current;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            Exception? operationFailure = Record.Exception(() => registration.Configure(endpoint, context));

            Assert.Equal(1, configureCalls);
            IReceiveEndpointSpecification specification = Assert.IsAssignableFrom<IReceiveEndpointSpecification>(configured);
            MachineProxy.Connection eventConnection = Assert.Single(machineProxy.EventConnections);
            MachineProxy.Connection stateConnection = Assert.Single(machineProxy.StateConnections);
            Assert.Same(observer, eventConnection.Observer);
            Assert.Same(observer, stateConnection.Observer);
            Assert.NotNull(eventConnection.Handle);
            Assert.NotNull(stateConnection.Handle);
            ProbeLogger.Entry entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal(Template, entry.Template);
            Assert.Equal(endpointProxy.Address.GetEndpointName(), entry.Values["Endpoint"]);
            Assert.Equal(TypeCache<ProbeState>.ShortName, entry.Values["SagaType"]);
            Assert.Equal(TypeCache.GetShortName(machine.GetType()), entry.Values["StateMachineType"]);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            Assert.Equal(loggerThrows ? uniqueFailure : null, logger.ThrownFailure);
            if (operationFailure is not null && loggerThrows)
                Assert.Same(uniqueFailure, operationFailure);
            Assert.Equal(0, contextProxy.PushContextCalls);
            Assert.Equal(0, observer.ExecutionCalls);

            // The first business oracle follows real configuration and observer acquisition.
            Assert.Null(operationFailure);

            Assert.Same(specification, Assert.Single(endpointProxy.Specifications));
            Assert.Empty(specification.Validate());
            Assert.Equal(1, endpointProxy.StateMachineNotifications);
            Assert.Equal(1, endpointProxy.SagaNotifications);
            Assert.Equal(1, endpointProxy.MessageNotifications);
            Assert.Same(machine, endpointProxy.NotifiedMachine);
            Assert.Same(endpointProxy.StateMachineConfigurator, endpointProxy.SagaConfigurator);
            Assert.Equal(0, observer.ExecutionCalls);
        }
        finally
        {
            try
            {
                var failures = new List<Exception>();
                foreach (MachineProxy.Connection connection in machineProxy.EventConnections.Concat(machineProxy.StateConnections))
                {
                    try { connection.Handle.Dispose(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
                try { provider.Dispose(); }
                catch (Exception failure) { failures.Add(failure); }
                if (failures.Count == 1)
                    ExceptionDispatchInfo.Capture(failures[0]).Throw();
                if (failures.Count > 1)
                    throw new AggregateException("Saga diagnostic fixture cleanup failed.", failures);
            }
            finally { LogContext.Current = previous; }
        }
    }

    public sealed class ProbeState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed record ProbeCreated(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ProbeMachine : ViciOneServiceBusStateMachine<ProbeState>
    {
        public ProbeMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Created).TransitionTo(Active));
        }

        public IState Active { get; private set; } = null!;
        public IEvent<ProbeCreated> Created { get; private set; } = null!;
    }

    public class MachineProxy : DispatchProxy
    {
        public sealed record Connection(object Observer, IDisposable Handle);
        public ProbeMachine Actual { get; set; } = null!;
        public List<Connection> EventConnections { get; } = new();
        public List<Connection> StateConnections { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
                throw new InvalidOperationException("Missing public machine method.");
            object? result;
            try { result = targetMethod.Invoke(Actual, args); }
            catch (TargetInvocationException failure) when (failure.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(failure.InnerException).Throw();
                throw;
            }
            if (targetMethod.Name == nameof(IStateMachine<ProbeState>.ConnectEventObserver))
            {
                Assert.NotNull(args);
                EventConnections.Add(new Connection(Assert.IsAssignableFrom<IEventObserver<ProbeState>>(args[^1]),
                    Assert.IsAssignableFrom<IDisposable>(result)));
            }
            else if (targetMethod.Name == nameof(IStateMachine<ProbeState>.ConnectStateObserver))
            {
                Assert.NotNull(args);
                StateConnections.Add(new Connection(Assert.IsAssignableFrom<IStateObserver<ProbeState>>(args[0]),
                    Assert.IsAssignableFrom<IDisposable>(result)));
            }
            return result;
        }
    }

    public class ContextProxy : DispatchProxy, ISetScopedConsumeContext
    {
        public IServiceProvider Provider { get; set; } = null!;
        public int PushContextCalls { get; private set; }

        public IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context)
        {
            PushContextCalls++;
            throw new InvalidOperationException("Configuration-only fixture must not enter a consume scope.");
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IServiceProvider.GetService) && args is [Type type])
                return Provider.GetService(type);
            throw new InvalidOperationException($"Unexpected registration context call: {targetMethod}");
        }
    }

    public class SelectorProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name is nameof(IContainerSelector.GetDefinition) or nameof(IContainerSelector.GetEndpointDefinition))
                return null;
            throw new InvalidOperationException($"Unexpected selector call: {targetMethod}");
        }
    }

    public class EndpointProxy : DispatchProxy
    {
        public Uri Address { get; set; } = null!;
        public List<IReceiveEndpointSpecification> Specifications { get; } = new();
        public int StateMachineNotifications { get; private set; }
        public int SagaNotifications { get; private set; }
        public int MessageNotifications { get; private set; }
        public object? NotifiedMachine { get; private set; }
        public object? StateMachineConfigurator { get; private set; }
        public object? SagaConfigurator { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_InputAddress")
                return Address;
            if (targetMethod?.Name == nameof(IReceiveEndpointConfigurator.AddEndpointSpecification) && args is [IReceiveEndpointSpecification specification])
            {
                Specifications.Add(specification);
                return null;
            }
            if (targetMethod?.Name == nameof(ISagaConfigurationObserver.StateMachineSagaConfigured))
            {
                Assert.Equal(typeof(ProbeState), Assert.Single(targetMethod.GetGenericArguments()));
                Assert.NotNull(args);
                StateMachineNotifications++;
                StateMachineConfigurator = args[0];
                NotifiedMachine = args[1];
                return null;
            }
            if (targetMethod?.Name == nameof(ISagaConfigurationObserver.SagaConfigured))
            {
                Assert.Equal(typeof(ProbeState), Assert.Single(targetMethod.GetGenericArguments()));
                Assert.NotNull(args);
                SagaNotifications++;
                SagaConfigurator = args[0];
                return null;
            }
            if (targetMethod?.Name == nameof(ISagaConfigurationObserver.SagaMessageConfigured))
            {
                Assert.Equal(new[] { typeof(ProbeState), typeof(ProbeCreated) }, targetMethod.GetGenericArguments());
                Assert.NotNull(args);
                Assert.IsAssignableFrom<ISagaMessageConfigurator<ProbeState, ProbeCreated>>(args[0]);
                MessageNotifications++;
                return null;
            }
            throw new InvalidOperationException($"Unexpected endpoint configuration call: {targetMethod}");
        }
    }

    sealed class ProbeObserver : IEventObserver<ProbeState>, IStateObserver<ProbeState>
    {
        public int ExecutionCalls { get; private set; }
        Task UnexpectedAsync()
        {
            ExecutionCalls++;
            throw new InvalidOperationException("This fixture configures and validates, but never executes a saga.");
        }
        public Task PreExecuteAsync(IBehaviorContext<ProbeState> context) => UnexpectedAsync();
        public Task PreExecuteAsync<T>(IBehaviorContext<ProbeState, T> context) where T : class => UnexpectedAsync();
        public Task PostExecuteAsync(IBehaviorContext<ProbeState> context) => UnexpectedAsync();
        public Task PostExecuteAsync<T>(IBehaviorContext<ProbeState, T> context) where T : class => UnexpectedAsync();
        public Task ExecuteFaultAsync(IBehaviorContext<ProbeState> context, Exception exception) => UnexpectedAsync();
        public Task ExecuteFaultAsync<T>(IBehaviorContext<ProbeState, T> context, Exception exception) where T : class => UnexpectedAsync();
        public Task StateChangedAsync(IBehaviorContext<ProbeState> context, IState currentState, IState? previousState) => UnexpectedAsync();
    }

    sealed class ProbeLogger(IOException? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string Template, IReadOnlyDictionary<string, object?> Values);
        public List<Entry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IOException? ThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> values)
                return;
            Dictionary<string, object?> fields = values.ToDictionary(item => item.Key, item => item.Value);
            if (!fields.TryGetValue("{OriginalFormat}", out object? template) || !Equals(template, Template))
                return;
            Entries.Add(new Entry(logLevel, Template, fields));
            if (failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }
}
