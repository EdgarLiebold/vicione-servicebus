using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to routing slip request faulted.</summary>
public class RoutingSlipRequestFaultedException :
    RoutingSlipException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="faulted">The faulted.</param>
    public RoutingSlipRequestFaultedException(RoutingSlipFaulted faulted)
        : base("The routing slip request faulted")
    {
        Faulted = faulted;
    }

    /// <summary>Gets the faulted.</summary>
    public RoutingSlipFaulted Faulted { get; }
}
