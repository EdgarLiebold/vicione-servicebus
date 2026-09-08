namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class ValidationMessage : IValidationContract
{
    public bool IsValid { get; set; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>();
}
