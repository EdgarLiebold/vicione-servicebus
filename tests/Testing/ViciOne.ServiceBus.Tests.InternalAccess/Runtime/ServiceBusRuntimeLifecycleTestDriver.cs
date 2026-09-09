using System.Reflection;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Runtime;

public sealed class ServiceBusRuntimeLifecycleTestDriver
{
    private static readonly Uri Address = new("loopback://runtime-lifecycle/bus");
    private readonly RecordingBusObserver _observer = new();
    private readonly CompletingStopHostHandle _hostHandle = new(Address);
    private readonly TaskCompletionSource _hostStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ServiceBusRuntimeLifecycleTestDriver()
    {
        if (LogContext.Current == null)
            LogContext.ConfigureCurrentLogContext();

        IHost host = DispatchProxy.Create<IHost, HostProxy>();
        HostProxy hostProxy = (HostProxy)(object)host;
        hostProxy.Address = Address;
        hostProxy.HostHandle = _hostHandle;
        hostProxy.Started = _hostStarted;
        hostProxy.Topology = PassiveProxy.Create<IBusTopology>();

        IReceiveEndpointConfiguration configuration =
            DispatchProxy.Create<IReceiveEndpointConfiguration, EndpointConfigurationProxy>();
        EndpointConfigurationProxy configurationProxy = (EndpointConfigurationProxy)(object)configuration;
        configurationProxy.InputAddress = Address;
        configurationProxy.ConsumePipe = PassiveProxy.Create<IConsumePipe>();
        configurationProxy.ReceiveEndpoint = PassiveProxy.Create<IReceiveEndpoint>();

        Bus = new ServiceBusRuntime(host, _observer, configuration, TimeProvider.System);
    }

    public IBusControl Bus { get; }

    public Task HostStarted => _hostStarted.Task;

    public int HostStopCount => _hostHandle.StopCount;

    public int PostStartCount => _observer.PostStartCount;

    public int StartFaultedCount => _observer.StartFaultedCount;

    public class HostProxy : DispatchProxy
    {
        public Uri Address { get; set; } = null!;
        public IHostHandle HostHandle { get; set; } = null!;
        public TaskCompletionSource Started { get; set; } = null!;
        public IBusTopology Topology { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_Address" => Address,
                "get_Topology" => Topology,
                nameof(IHost.Start) => Start(),
                _ => throw new InvalidOperationException($"Unexpected host member: {targetMethod.Name}."),
            };
        }

        private IHostHandle Start()
        {
            Started.TrySetResult();
            return HostHandle;
        }
    }

    public class EndpointConfigurationProxy : DispatchProxy
    {
        public IConsumePipe ConsumePipe { get; set; } = null!;
        public Uri InputAddress { get; set; } = null!;
        public IReceiveEndpoint ReceiveEndpoint { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name switch
            {
                "get_ConsumePipe" => ConsumePipe,
                "get_InputAddress" => InputAddress,
                "get_ReceiveEndpoint" => ReceiveEndpoint,
                _ => throw new InvalidOperationException(
                    $"Unexpected receive-endpoint configuration member: {targetMethod.Name}."),
            };
        }
    }

    public class PassiveProxy : DispatchProxy
    {
        public static T Create<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected passive dependency member: {targetMethod?.Name}.");
    }

    private sealed class CompletingStopHostHandle(Uri address) : IHostHandle
    {
        private readonly TaskCompletionSource<HostReady> _ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _stopCount;

        public Task<HostReady> Ready => _ready.Task;

        public int StopCount => Volatile.Read(ref _stopCount);

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _stopCount);
            _ready.TrySetResult(new HostReadyEvent(address, [], []));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBusObserver : IBusObserver
    {
        private int _postStartCount;
        private int _startFaultedCount;

        public int PostStartCount => Volatile.Read(ref _postStartCount);

        public int StartFaultedCount => Volatile.Read(ref _startFaultedCount);

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
        {
            Interlocked.Increment(ref _postStartCount);
            return Task.CompletedTask;
        }

        public Task StartFaultedAsync(IBus bus, Exception exception)
        {
            Interlocked.Increment(ref _startFaultedCount);
            return Task.CompletedTask;
        }

        public Task PreStopAsync(IBus bus) => Task.CompletedTask;

        public Task PostStopAsync(IBus bus) => Task.CompletedTask;

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;
    }
}
