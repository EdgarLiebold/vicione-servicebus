namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for state machine interface type.
/// </summary>
public interface IStateMachineInterfaceType
{
    /// <summary>
    /// Gets connector.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    ISagaMessageConnector<T> GetConnector<T>()
        where T : class, ISaga;
}
