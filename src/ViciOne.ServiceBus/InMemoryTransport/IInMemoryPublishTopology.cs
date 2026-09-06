namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by in memory publish topology.</summary>
public interface IInMemoryPublishTopology :
    IPublishTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new IInMemoryMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;
}
