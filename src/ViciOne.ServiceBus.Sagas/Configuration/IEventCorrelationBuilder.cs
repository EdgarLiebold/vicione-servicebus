namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds event correlation components.</summary>
public interface IEventCorrelationBuilder
{
    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    IEventCorrelation Build();
}
