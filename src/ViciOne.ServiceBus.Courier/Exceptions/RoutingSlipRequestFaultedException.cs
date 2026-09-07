using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that a requested routing slip reached its terminal fault state.</summary>
public sealed class RoutingSlipRequestFaultedException :
    RoutingSlipException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="faulted">The terminal routing-slip fault event.</param>
    public RoutingSlipRequestFaultedException(RoutingSlipFaulted faulted)
        : base("The routing slip request faulted")
    {
        Faulted = faulted ?? throw new ArgumentNullException(nameof(faulted));
    }

    /// <summary>Gets the terminal routing-slip fault event.</summary>
    public RoutingSlipFaulted Faulted { get; }
}
