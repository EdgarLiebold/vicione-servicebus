namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for event correlation builder.
/// </summary>
public interface IEventCorrelationBuilder
{
    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    EventCorrelation Build();
}
