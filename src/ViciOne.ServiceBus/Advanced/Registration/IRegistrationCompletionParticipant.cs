namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Completes registrations owned by an optional capability package before the container is built.</summary>
public interface IRegistrationCompletionParticipant
{
    /// <summary>Gets the order.</summary>
    int Order { get; }

    /// <summary>Completes registrations contributed by the capability package.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void Complete(IRegistrationConfigurator configurator);
}
