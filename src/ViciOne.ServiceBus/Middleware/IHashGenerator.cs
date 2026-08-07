// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    /// <summary>
    /// Generates a hash of the input data for partitioning purposes
    /// </summary>
    public interface IHashGenerator
    {
        uint Hash(byte[] data);
    }
}
