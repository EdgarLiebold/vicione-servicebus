namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public interface IPersonContract
{
    int Id { get; init; }

    string Name { get; set; }

    IContactContract Contact { get; }
}
