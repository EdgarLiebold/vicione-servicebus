using System;
using ViciOne.ServiceBus.MessageData.Configuration;
using ViciOne.ServiceBus.MessageData.Conventions;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the repository and transforms used by message-data properties.</summary>
public static class MessageDataConfiguratorExtensions
{
    /// <summary>Enables storage and loading for message contracts that contain message-data properties.</summary>
    /// <param name="configurator">The bus configurator that owns the repository.</param>
    /// <param name="repository">The repository used by both send and consume transforms.</param>
    /// <param name="policy">The immutable policy owned by this bus, or the default policy when omitted.</param>
    public static void UseMessageData(this IBusFactoryConfigurator configurator, IMessageDataRepository repository, MessageDataPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(repository);

        MessageDataPolicy runtimePolicy = policy ?? MessageDataPolicy.Default;

        bool consumeAdded = configurator.ConsumeTopology.TryAddConvention(new MessageDataConsumeTopologyConvention(repository));
        bool sendAdded = configurator.SendTopology.TryAddConvention(new MessageDataSendTopologyConvention(repository, runtimePolicy));
        if (!consumeAdded || !sendAdded)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message data", "unknown", "Message data is already configured for this bus owner.", "Correct the named configuration before starting the host"));

        _ = new ActivityMessageDataConfigurationObserver(configurator, repository, false);
    }

    /// <summary>Selects and configures the single message-data repository owned by a bus.</summary>
    /// <param name="configurator">The bus configurator that owns the repository.</param>
    /// <param name="selector">
    /// The repository selector.
    /// The callback that selects a repository, for example by calling
    /// <see cref="MessageDataRepositorySelectorExtensions.UseFileSystem" />.
    /// </param>
    /// <param name="policy">The immutable policy owned by this bus, or the default policy when omitted.</param>
    /// <returns>The configured message data.</returns>
    public static IMessageDataRepository UseMessageData(this IBusFactoryConfigurator configurator,
        Func<IMessageDataRepositorySelector, IMessageDataRepository> selector, MessageDataPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(selector);

        var repository = selector(new MessageDataRepositorySelector(configurator))
            ?? throw new InvalidOperationException("The message-data repository selector returned null.");

        UseMessageData(configurator, repository, policy);

        if (repository is IBusObserver observer)
            configurator.ConnectBusObserver(observer);

        return repository;
    }
}
