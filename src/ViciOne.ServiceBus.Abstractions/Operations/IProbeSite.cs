namespace ViciOne.ServiceBus.Operations;

/// <summary>Exposes the current diagnostic structure of a service-bus component.</summary>
public interface IProbeSite
{
    /// <summary>Writes the component's current diagnostic values and child scopes.</summary>
    /// <param name="context">The destination for the diagnostic snapshot.</param>
    void Probe(ProbeContext context);
}
