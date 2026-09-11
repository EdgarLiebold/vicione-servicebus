namespace ViciOne.ServiceBus.Configuration;

/// <summary>Associates repository selection with its owning bus configurator.</summary>
internal sealed class MessageDataRepositorySelector :
    IMessageDataRepositorySelector
{
    /// <summary>Creates a selector for one bus configuration.</summary>
    /// <param name="configurator">The bus configurator that will own the selected repository.</param>
    public MessageDataRepositorySelector(IBusFactoryConfigurator configurator)
    {
        Configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    /// <inheritdoc />
    public IBusFactoryConfigurator Configurator { get; }
}
