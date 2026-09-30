namespace ViciOne.ServiceBus.Serialization;

// Internal forwarding serializers declare their preserved envelope types during body creation.
internal interface IForwardedMessageTypeContext
{
    void AcceptForwardedMessageTypes(string[]? messageTypes);
}
