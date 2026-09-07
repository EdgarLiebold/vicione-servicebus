namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by state machine interface type.</summary>
public interface IStateMachineInterfaceType
{
    /// <summary>Gets connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The connector.</returns>
    ISagaMessageConnector<T> GetConnector<T>()
        where T : class, ISaga;
}
