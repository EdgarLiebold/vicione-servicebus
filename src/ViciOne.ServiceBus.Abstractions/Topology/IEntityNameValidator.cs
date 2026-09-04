namespace ViciOne.ServiceBus;

public interface IEntityNameValidator
{
    bool IsValidEntityName(string name);

    void ThrowIfInvalidEntityName(string name);
}
