namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Completes registrations owned by an optional capability package before the container is built.
/// </summary>
public interface IRegistrationCompletionParticipant
{
    /// <summary>
    /// Gets the ordering key used when multiple capability packages complete their registrations.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Completes registrations contributed by the capability package.
    /// </summary>
    /// <param name="configurator">The active registration configurator.</param>
    void Complete(IRegistrationConfigurator configurator);
}
