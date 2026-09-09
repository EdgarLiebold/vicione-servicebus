namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides transport metadata and delivery requirements for a published message.</summary>
public interface PublishContext :
    SendContext
{
    /// <summary>Gets or sets whether publishing must fail when no subscriber can receive the message.</summary>
    bool Mandatory { get; set; }
}


/// <summary>Provides the typed message and transport metadata for a publish operation.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface PublishContext<out TMessage> :
    SendContext<TMessage>,
    PublishContext
    where TMessage : class
{
}
