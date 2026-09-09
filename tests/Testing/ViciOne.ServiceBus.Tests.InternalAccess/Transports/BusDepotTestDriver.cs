using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Transports;

public static class BusDepotTestDriver
{
    public static void CreateWithNullInstances()
    {
        _ = new BusDepot(null!, NullLogger<BusDepot>.Instance);
    }

    public static void CreateWithNullLogger()
    {
        _ = new BusDepot(Array.Empty<IBusInstance>(), null!);
    }

    public static void CreateWithNullInstance()
    {
        _ = new BusDepot(new IBusInstance[] { null! }, NullLogger<BusDepot>.Instance);
    }

    public static void CreateWithDuplicateInstanceType()
    {
        _ = new BusDepot(
            new IBusInstance[] { new TestBusInstance(), new TestBusInstance() },
            NullLogger<BusDepot>.Instance);
    }

    public static Task StartEmptyAsync(CancellationToken cancellationToken)
    {
        var depot = new BusDepot(Array.Empty<IBusInstance>(), NullLogger<BusDepot>.Instance);
        return depot.StartAsync(cancellationToken);
    }

    public static Task StopEmptyAsync(CancellationToken cancellationToken)
    {
        var depot = new BusDepot(Array.Empty<IBusInstance>(), NullLogger<BusDepot>.Instance);
        return depot.StopAsync(cancellationToken);
    }

    private sealed class TestBusInstance : IBusInstance
    {
        public string Name => "test";

        public Type InstanceType => typeof(IBus);

        public IBus Bus => null!;

        public IBusControl BusControl => null!;

        public IHostConfiguration HostConfiguration => null!;

        public void Connect<TRider>(IRiderControl riderControl)
            where TRider : IRider
        {
            throw new NotSupportedException();
        }

        public TRider GetRider<TRider>()
            where TRider : IRider
        {
            throw new NotSupportedException();
        }

        public IHostReceiveEndpointHandle ConnectReceiveEndpoint(
            IEndpointDefinition definition,
            IEndpointNameFormatter endpointNameFormatter,
            Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
        {
            throw new NotSupportedException();
        }

        public IHostReceiveEndpointHandle ConnectReceiveEndpoint(
            string queueName,
            Action<IBusRegistrationContext, IReceiveEndpointConfigurator>? configure = null)
        {
            throw new NotSupportedException();
        }
    }
}
