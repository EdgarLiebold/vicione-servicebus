namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class ContactMessage : IContactContract
{
    public string Email { get; set; } = string.Empty;
}
