// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    /// <summary>
    /// For saga repositories that use an incrementing version
    /// </summary>
    public interface ISagaVersion :
        ISaga
    {
        int Version { get; set; }
    }
}
