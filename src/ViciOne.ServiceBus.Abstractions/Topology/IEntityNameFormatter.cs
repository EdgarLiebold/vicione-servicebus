// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// Used to build entity names for the publish topology
    /// </summary>
    public interface IEntityNameFormatter
    {
        /// <summary>
        /// Formats the entity name for the given message type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string FormatEntityName<T>();
    }
}
