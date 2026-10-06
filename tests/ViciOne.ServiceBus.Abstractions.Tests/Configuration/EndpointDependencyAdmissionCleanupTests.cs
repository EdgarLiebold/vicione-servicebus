using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class EndpointDependencyAdmissionCleanupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEPENDENCY", "public-first-admission-cleanup-and-real-lifecycle")]
    public async Task AddDependency_FirstAdmissionFailureRetiresTheAcquiredObserverAsync(bool failAdmission)
    {
        var state = new AdmissionState(failAdmission);
        IReceiveEndpointConfigurator connector = CreateEndpoint(state, "connector", out EndpointConfiguratorProxy connectorProxy);
        IReceiveEndpointConfigurator dependency = CreateEndpoint(state, "dependency", out EndpointConfiguratorProxy dependencyProxy);

        try
        {
            Exception? failure = Record.Exception(() =>
            {
                connector.AddDependency(dependency);
            });

            Assert.Equal(1, dependencyProxy.ConnectCalls);
            Assert.Equal(1, connectorProxy.DependencyCalls);
            Assert.NotNull(connectorProxy.CapturedDependency);
            if (failAdmission)
            {
                Assert.Same(state.AdmissionFailure, failure);
                Assert.Equal(1, connectorProxy.AdmissionThrows);
                Assert.Null(connectorProxy.StoredDependency);
                Assert.Equal(0, connectorProxy.ConnectCalls);
                Assert.Equal(0, dependencyProxy.DependentCalls);
                Assert.Null(dependencyProxy.StoredDependent);
                Assert.Equal(["dependency.Connect", "connector.AddDependency"], state.Calls);
                ReturnedRegistration registration = Assert.Single(state.Registrations);

                // Observe the genuine registration before any callback or fixture fallback cleanup.
                Assert.Equal(0, dependencyProxy.Observers.Count);
                Assert.Equal(1, registration.ReleaseAttempts);
            }
            else
            {
                Assert.Null(failure);
                Assert.Equal(0, connectorProxy.AdmissionThrows);
                Assert.Same(connectorProxy.CapturedDependency, connectorProxy.StoredDependency);
                Assert.NotNull(dependencyProxy.StoredDependent);
                Assert.Equal(1, connectorProxy.ConnectCalls);
                Assert.Equal(1, dependencyProxy.DependentCalls);
                Assert.Equal(2, state.Registrations.Count);
                Assert.Equal(1, connectorProxy.Observers.Count);
                Assert.Equal(1, dependencyProxy.Observers.Count);
                Assert.All(state.Registrations, registration => Assert.Equal(0, registration.ReleaseAttempts));
                Assert.Equal(["dependency.Connect", "connector.AddDependency", "connector.Connect", "dependency.AddDependent"], state.Calls);

                ReturnedRegistration dependencyRegistration = Assert.Single(state.Registrations, registration => registration.Name == "dependency");
                ReturnedRegistration connectorRegistration = Assert.Single(state.Registrations, registration => registration.Name == "connector");
                var ready = new ReadyEvent();
                Task readyCallback = dependencyRegistration.Observer.ReadyAsync(ready);
                state.CallbackTasks.Add(readyCallback);
                await readyCallback.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                await connectorProxy.CapturedDependency.Ready.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(0, dependencyProxy.Observers.Count);
                Assert.Equal(1, dependencyRegistration.ReleaseAttempts);

                var completed = new CompletedEvent();
                Task completedCallback = connectorRegistration.Observer.CompletedAsync(completed);
                state.CallbackTasks.Add(completedCallback);
                await completedCallback.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                await dependencyProxy.StoredDependent.Completed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Equal(0, connectorProxy.Observers.Count);
                Assert.Equal(1, connectorRegistration.ReleaseAttempts);
            }
        }
        finally
        {
            await CleanupAsync(state);
        }
    }

    private static IReceiveEndpointConfigurator CreateEndpoint(AdmissionState state, string name, out EndpointConfiguratorProxy proxy)
    {
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointConfiguratorProxy>();
        proxy = (EndpointConfiguratorProxy)(object)endpoint;
        proxy.State = state;
        proxy.Name = name;
        return endpoint;
    }

    private static async Task CleanupAsync(AdmissionState state)
    {
        var errors = new List<Exception>();
        foreach (ReturnedRegistration registration in state.Registrations)
        {
            try
            {
                registration.Inner.Disconnect();
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        // Captured public helpers still own their actual promises, even after observer disconnection.
        foreach (ReturnedRegistration registration in state.Registrations)
        {
            try
            {
                Task callback = registration.Observer.FaultedAsync(new FaultedEvent(state.CleanupTerminal));
                state.CallbackTasks.Add(callback);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        foreach (Task task in state.CallbackTasks.Concat(state.OwnedTasks))
        {
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await task.WaitAsync(budget.Token);
            }
            catch (Exception exception) when (task.IsCompleted && ReferenceEquals(exception, state.CleanupTerminal))
            {
                // Only the unique deliberately delivered terminal endpoint event is expected here.
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count != 0)
            throw new AggregateException(errors);
    }

    private class EndpointConfiguratorProxy : DispatchProxy
    {
        private AdmissionState? _state;
        private string? _name;
        private readonly Connectable<IReceiveEndpointObserver> _observers = new();
        private IReceiveEndpointDependency? _capturedDependency;
        private IReceiveEndpointDependency? _storedDependency;
        private IReceiveEndpointDependent? _storedDependent;
        private int _connectCalls;
        private int _dependencyCalls;
        private int _dependentCalls;
        private int _admissionThrows;

        public AdmissionState State { get => _state ?? throw new InvalidOperationException("Unconfigured proxy"); set => _state = value; }
        public string Name { get => _name ?? throw new InvalidOperationException("Unconfigured proxy"); set => _name = value; }
        public Connectable<IReceiveEndpointObserver> Observers => _observers;
        public IReceiveEndpointDependency? CapturedDependency => _capturedDependency;
        public IReceiveEndpointDependency? StoredDependency => _storedDependency;
        public IReceiveEndpointDependent? StoredDependent => _storedDependent;
        public int ConnectCalls => _connectCalls;
        public int DependencyCalls => _dependencyCalls;
        public int DependentCalls => _dependentCalls;
        public int AdmissionThrows => _admissionThrows;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case nameof(IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver):
                {
                    var observer = (IReceiveEndpointObserver)args![0]!;
                    State.Calls.Add($"{Name}.Connect");
                    _connectCalls++;
                    ConnectHandle inner = _observers.Connect(observer);
                    var registration = new ReturnedRegistration(Name, observer, inner);
                    State.Registrations.Add(registration);
                    return registration;
                }
                case nameof(IReceiveEndpointDependencyConnector.AddDependency):
                    State.Calls.Add($"{Name}.AddDependency");
                    _dependencyCalls++;
                    _capturedDependency = (IReceiveEndpointDependency)args![0]!;
                    State.OwnedTasks.Add(_capturedDependency.Ready);
                    if (State.FailAdmission && Name == "connector")
                    {
                        _admissionThrows++;
                        throw State.AdmissionFailure;
                    }
                    _storedDependency = _capturedDependency;
                    return null;
                case nameof(IReceiveEndpointDependentConnector.AddDependent):
                    State.Calls.Add($"{Name}.AddDependent");
                    _dependentCalls++;
                    _storedDependent = (IReceiveEndpointDependent)args![0]!;
                    State.OwnedTasks.Add(_storedDependent.Completed);
                    return null;
                default:
                    throw new NotSupportedException($"Unexpected endpoint invocation: {targetMethod?.Name}");
            }
        }
    }

    private sealed class AdmissionState(bool failAdmission)
    {
        public bool FailAdmission { get; } = failAdmission;
        public InvalidOperationException AdmissionFailure { get; } = new("first dependency admission failed before storage");
        public InvalidOperationException CleanupTerminal { get; } = new("fixture terminal endpoint cleanup");
        public List<string> Calls { get; } = [];
        public List<ReturnedRegistration> Registrations { get; } = [];
        public List<Task> OwnedTasks { get; } = [];
        public List<Task> CallbackTasks { get; } = [];
    }

    private sealed class ReturnedRegistration(string name, IReceiveEndpointObserver observer, ConnectHandle inner) : ConnectHandle
    {
        public string Name { get; } = name;
        public IReceiveEndpointObserver Observer { get; } = observer;
        public ConnectHandle Inner { get; } = inner;
        public int ReleaseAttempts { get; private set; }
        public void Dispose() => Disconnect();

        public void Disconnect()
        {
            ReleaseAttempts++;
            Inner.Disconnect();
        }
    }

    private sealed class ReadyEvent : ReceiveEndpointReady
    {
        public bool IsStarted => true;
        public Uri InputAddress { get; } = new("loopback://dependency");
        public IReceiveEndpoint ReceiveEndpoint { get; } = DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    private sealed class CompletedEvent : ReceiveEndpointCompleted
    {
        public long DeliveryCount => 3;
        public int MaxConcurrentDeliveryCount => 2;
        public Uri InputAddress { get; } = new("loopback://connector");
        public IReceiveEndpoint ReceiveEndpoint { get; } = DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    private sealed class FaultedEvent(Exception exception) : ReceiveEndpointFaulted
    {
        public Exception Exception { get; } = exception;
        public bool IsTerminal => true;
        public Uri InputAddress { get; } = new("loopback://dependency-cleanup");
        public IReceiveEndpoint ReceiveEndpoint { get; } = DispatchProxy.Create<IReceiveEndpoint, RejectInvocationProxy>();
    }

    public class RejectInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected receive endpoint invocation: {targetMethod?.Name}");
    }
}
