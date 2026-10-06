using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ActivityObserverRegistrationCleanupTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-CONFIGURATION-OBSERVATION", "public-returned-observer-handle-all-child-retirement")]
    public void ConnectActivityObserver_ReturnedHandleRetiresAllAcquiredRegistrations(
        bool endpointOverload, bool disposeOwner, bool cleanupThrows)
    {
        var configurationObservers = new ActivityConfigurationObservable();
        var executeObservers = new Connectable<IActivityObserver>();
        var compensateObservers = new Connectable<IActivityObserver>();
        var observer = new RecordingActivityObserver();
        var cleanupFailure = new IOException("activity-observer-child-release-primary");
        var acquired = new List<ReturnedHandle>();
        var acquisitionOrder = new List<string>();
        var recordedObservers = new List<IActivityObserver>();

        object? ConnectConfiguration(MethodInfo method, object?[]? arguments)
        {
            if (method.Name != nameof(IActivityConfigurationObserverConnector.ConnectActivityConfigurationObserver)
                || arguments is not [IActivityConfigurationObserver actual])
                throw new NotSupportedException(method.Name);

            acquisitionOrder.Add("configuration");
            ConnectHandle inner = configurationObservers.Connect(actual);
            var handle = new ReturnedHandle(inner, null);
            acquired.Add(handle);
            return handle;
        }

        object? ConnectChild(MethodInfo method, object?[]? arguments,
            Connectable<IActivityObserver> connections, string name, bool throwOnRelease)
        {
            if (method.Name != nameof(IActivityObserverConnector.ConnectActivityObserver)
                || arguments is not [IActivityObserver actual])
                throw new NotSupportedException(method.Name);

            acquisitionOrder.Add(name);
            recordedObservers.Add(actual);
            ConnectHandle inner = connections.Connect(actual);
            var handle = new ReturnedHandle(inner, throwOnRelease ? cleanupFailure : null);
            acquired.Add(handle);
            return handle;
        }

        try
        {
            ConnectHandle owner = endpointOverload
                ? CreateFacade<IReceiveEndpointConfigurator>(ConnectConfiguration).ConnectActivityObserver(observer)
                : CreateFacade<IBusFactoryConfigurator>(ConnectConfiguration).ConnectActivityObserver(observer);

            Assert.Equal(1, configurationObservers.Count);
            Assert.Equal(0, executeObservers.Count);
            Assert.Equal(0, compensateObservers.Count);
            Assert.Single(acquired);

            var execute = CreateFacade<IExecuteActivityPipeConfigurator<ProbeActivity, ProbeArguments>>(
                (method, arguments) => ConnectChild(method, arguments, executeObservers, "execute", cleanupThrows));
            var compensate = CreateFacade<ICompensateActivityPipeConfigurator<ProbeActivity, ProbeLog>>(
                (method, arguments) => ConnectChild(method, arguments, compensateObservers, "compensate", false));

            configurationObservers.ExecuteActivityConfigured(execute);
            configurationObservers.CompensateActivityConfigured(compensate);

            Assert.Equal(["configuration", "execute", "compensate"], acquisitionOrder);
            Assert.Equal(3, acquired.Count);
            Assert.Equal(2, recordedObservers.Count);
            Assert.All(recordedObservers, actual => Assert.Same(observer, actual));
            Assert.Same(observer, Assert.Single(executeObservers.Connected));
            Assert.Same(observer, Assert.Single(compensateObservers.Connected));
            Assert.All(acquired, handle => Assert.Equal(0, handle.ReleaseAttempts));

            Exception? failure = Record.Exception(() =>
            {
                if (disposeOwner)
                    owner.Dispose();
                else
                    owner.Disconnect();
            });

            if (cleanupThrows)
            {
                Assert.Same(cleanupFailure, failure);
                Assert.Equal(1, acquired[1].ThrowCount);
                Assert.Same(cleanupFailure, acquired[1].ThrownFailure);
            }
            else
            {
                Assert.Null(failure);
                Assert.Equal(0, acquired[1].ThrowCount);
            }

            Assert.Equal(0, configurationObservers.Count);
            Assert.Equal(0, executeObservers.Count);
            AssertSelectedRelease(acquired[0], disposeOwner);
            AssertSelectedRelease(acquired[1], disposeOwner);

            // First causal boundary uses the second real registration before fixture fallback.
            Assert.Equal(0, compensateObservers.Count);
            AssertSelectedRelease(acquired[2], disposeOwner);
            Assert.Equal(0, observer.NotificationCalls);

            // Removal of the configuration observer must prevent further child acquisition.
            configurationObservers.ExecuteActivityConfigured(execute);
            configurationObservers.CompensateActivityConfigured(compensate);
            Assert.Equal(3, acquired.Count);
            Assert.Equal(["configuration", "execute", "compensate"], acquisitionOrder);
        }
        finally
        {
            var failures = new List<Exception>();
            foreach (ReturnedHandle handle in acquired)
            {
                try
                {
                    // Bypass only the injected wrapper and retire each actual registration.
                    handle.Inner.Disconnect();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException("Activity observer fixture cleanup failed.", failures);
        }
    }

    private static void AssertSelectedRelease(ReturnedHandle handle, bool disposeOwner)
    {
        Assert.Equal(disposeOwner ? 1 : 0, handle.DisposeCalls);
        Assert.Equal(disposeOwner ? 0 : 1, handle.DisconnectCalls);
        Assert.Equal(1, handle.ReleaseAttempts);
    }

    private static T CreateFacade<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        T facade = DispatchProxy.Create<T, PublicConfiguratorProxy>();
        ((PublicConfiguratorProxy)(object)facade).Invocation = invoke;
        return facade;
    }

    public class PublicConfiguratorProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Invocation { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Invocation(targetMethod ?? throw new InvalidOperationException("Missing public SPI method."), args);
    }

    private sealed class ReturnedHandle(ConnectHandle inner, IOException? failure) : ConnectHandle
    {
        public ConnectHandle Inner { get; } = inner;
        public int DisconnectCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public int ReleaseAttempts { get; private set; }
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }

        public void Disconnect()
        {
            DisconnectCalls++;
            ReleaseAttempts++;
            Inner.Disconnect();
            ThrowIfSelected();
        }

        public void Dispose()
        {
            DisposeCalls++;
            ReleaseAttempts++;
            Inner.Dispose();
            ThrowIfSelected();
        }

        private void ThrowIfSelected()
        {
            if (failure is not null && ReleaseAttempts == 1)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }

    private sealed class RecordingActivityObserver : IActivityObserver
    {
        public int NotificationCalls { get; private set; }

        public Task PreExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
            where TActivity : class where TArguments : class => UnexpectedNotificationAsync();
        public Task PostExecuteAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context)
            where TActivity : class where TArguments : class => UnexpectedNotificationAsync();
        public Task ExecuteFaultAsync<TActivity, TArguments>(ExecuteActivityContext<TActivity, TArguments> context, Exception exception)
            where TActivity : class where TArguments : class => UnexpectedNotificationAsync();
        public Task PreCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
            where TActivity : class where TLog : class => UnexpectedNotificationAsync();
        public Task PostCompensateAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context)
            where TActivity : class where TLog : class => UnexpectedNotificationAsync();
        public Task CompensateFailAsync<TActivity, TLog>(CompensateActivityContext<TActivity, TLog> context, Exception exception)
            where TActivity : class where TLog : class => UnexpectedNotificationAsync();

        private Task UnexpectedNotificationAsync()
        {
            NotificationCalls++;
            throw new InvalidOperationException("Registration cleanup must not invoke activity business callbacks.");
        }
    }

    public sealed record ProbeArguments;
    public sealed record ProbeLog;
    public sealed class ProbeActivity { }
}
