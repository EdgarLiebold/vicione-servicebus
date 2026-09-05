using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData.Configuration;
using ViciOne.ServiceBus.MessageData.Conventions;

#nullable enable

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for message data configurator.
/// </summary>
public static class MessageDataConfiguratorExtensions
{
    /// <summary>
    /// Enable the loading of message data for the any message type that includes a MessageData property.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="repository"></param>
    /// <param name="policy">The immutable policy owned by this bus, or the default policy when omitted.</param>
    public static void UseMessageData(this IBusFactoryConfigurator configurator, IMessageDataRepository repository, MessageDataPolicy? policy = null)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        MessageDataPolicy runtimePolicy = policy ?? MessageDataPolicy.Default;

        bool consumeAdded = configurator.ConsumeTopology.TryAddConvention(new MessageDataConsumeTopologyConvention(repository));
        bool sendAdded = configurator.SendTopology.TryAddConvention(new MessageDataSendTopologyConvention(repository, runtimePolicy));
        if (!consumeAdded || !sendAdded)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message data", "unknown", "Message data is already configured for this bus owner.", "Correct the named configuration before starting the host"));

        // Courier does not use ConsumeContext, so it needs to be special
        _ = new CourierMessageDataConfigurationObserver(configurator, repository, false);
    }

    /// <summary>
    /// Enable the loading of message data for the any message type that includes a MessageData property.
    /// </summary>
    /// <param name="configurator">The bus factory configurator.</param>
    /// <param name="selector">
    /// The repository selector.
    /// See extension methods, e.g. <see cref="MessageDataRepositorySelectorExtensions.FileSystem" />.
    /// </param>
    /// <param name="policy">The immutable policy owned by this bus, or the default policy when omitted.</param>
    public static IMessageDataRepository UseMessageData(this IBusFactoryConfigurator configurator,
        Func<IMessageDataRepositorySelector, IMessageDataRepository> selector, MessageDataPolicy? policy = null)
    {
        if (configurator is null)
            throw new ArgumentNullException(nameof(configurator));

        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        var repository = selector(new MessageDataRepositorySelector(configurator));

        UseMessageData(configurator, repository, policy);

        if (repository is IBusObserver observer)
            configurator.ConnectBusObserver(observer);

        return repository;
    }
}
