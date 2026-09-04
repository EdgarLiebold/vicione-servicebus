using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to routing slip request faulted.
/// </summary>
public class RoutingSlipRequestFaultedException :
    RoutingSlipException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="faulted">The faulted value.</param>
    public RoutingSlipRequestFaultedException(RoutingSlipFaulted faulted)
        : base("The routing slip request faulted")
    {
        Faulted = faulted;
    }

    /// <summary>
    /// Gets the faulted value.
    /// </summary>
    public RoutingSlipFaulted Faulted { get; }
}
