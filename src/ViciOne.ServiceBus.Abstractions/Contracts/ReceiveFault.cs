namespace ViciOne.ServiceBus;

/// <summary>Describes a fault produced when an incoming envelope cannot be deserialized or consumed.</summary>
public interface ReceiveFault :
    Fault
{
    /// <summary>Gets the content type declared by the incoming envelope, when supplied.</summary>
    string? ContentType { get; }
}
