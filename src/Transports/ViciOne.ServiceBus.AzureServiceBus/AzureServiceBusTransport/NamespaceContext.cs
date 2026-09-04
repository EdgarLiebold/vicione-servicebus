namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// A service bus namespace which has the appropriate messaging factories available
/// </summary>
public interface NamespaceContext :
    PipeContext
{
    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    ConnectionContext ConnectionContext { get; }
}
