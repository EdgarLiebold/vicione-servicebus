namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates a message context for one contract.</summary>
internal interface IMessageFactory<out TMessage>
    where TMessage : class
{
    InitializeContext<TMessage> Create(InitializeContext context);
}


/// <summary>Creates an untyped message instance.</summary>
internal interface IMessageFactory
{
    object Create();
}
