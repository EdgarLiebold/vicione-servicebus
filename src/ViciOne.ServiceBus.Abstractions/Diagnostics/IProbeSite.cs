namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// To support the introspection of code, this interface is used to gain
/// information about the bus.
/// </summary>
public interface IProbeSite
{
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    void Probe(ProbeContext context);
}
