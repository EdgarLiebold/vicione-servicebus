namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.DeployTopologyContracts;

public interface OrderSubmitted : OrderEvent;

public interface OrderEvent
{
    Guid OrderId { get; }
}

public interface PackageShipped : PackageEvent;

[ExcludeFromTopology]
public interface PackageEvent;

public interface CustomerEvent
{
    Guid CustomerId { get; }
}
