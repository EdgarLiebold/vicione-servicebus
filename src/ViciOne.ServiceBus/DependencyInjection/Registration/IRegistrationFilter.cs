using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Defines the contract for registration filter.
/// </summary>
public interface IRegistrationFilter
{
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IConsumerRegistration registration);
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(ISagaRegistration registration);
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IExecuteActivityRegistration registration);
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IActivityRegistration registration);
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IFutureRegistration registration);
    /// <summary>
    /// Performs the matches operation.
    /// </summary>
    /// <param name="registration">The registration value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Matches(IEndpointRegistration registration);
}
