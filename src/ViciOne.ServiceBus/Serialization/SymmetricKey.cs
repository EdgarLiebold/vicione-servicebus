// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    public interface SymmetricKey
    {
        byte[] Key { get; }

        byte[] IV { get; }
    }
}
