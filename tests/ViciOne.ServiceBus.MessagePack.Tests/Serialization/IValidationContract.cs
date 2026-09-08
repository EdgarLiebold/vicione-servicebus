namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public interface IValidationContract
{
    bool IsValid { get; }

    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }
}
