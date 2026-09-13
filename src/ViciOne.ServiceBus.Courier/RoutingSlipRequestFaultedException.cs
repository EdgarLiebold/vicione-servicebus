using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that a requested routing slip reached its terminal fault state.</summary>
public sealed class RoutingSlipRequestFaultedException :
    RoutingSlipException
{
    /// <summary>Creates a request failure from the routing slip's terminal fault event.</summary>
    /// <param name="faulted">The terminal routing-slip fault event.</param>
    public RoutingSlipRequestFaultedException(IRoutingSlipFaulted faulted)
        : base("The routing slip request faulted")
    {
        Faulted = faulted ?? throw new ArgumentNullException(nameof(faulted));
    }

    /// <summary>Gets the terminal routing-slip fault event.</summary>
    public IRoutingSlipFaulted Faulted { get; }
}
