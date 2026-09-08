namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class PersonMessage : IPersonContract
{
    public int Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public IContactContract Contact { get; set; } = new ContactMessage();
}
