namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for session id formatter.
/// </summary>
public interface ISessionIdFormatter
{
    /// <summary>
    /// Performs the format session id operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    string? FormatSessionId<T>(SendContext<T> context)
        where T : class;
}
