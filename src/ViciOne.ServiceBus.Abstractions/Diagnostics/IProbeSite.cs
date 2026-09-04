namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// To support the introspection of code, this interface is used to gain
/// information about the bus.
/// </summary>
public interface IProbeSite
{
    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    void Probe(ProbeContext context);
}
