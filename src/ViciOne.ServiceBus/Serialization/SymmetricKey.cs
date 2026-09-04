namespace ViciOne.ServiceBus.Serialization;

public interface SymmetricKey
{
    byte[] Key { get; }

    byte[] IV { get; }
}
