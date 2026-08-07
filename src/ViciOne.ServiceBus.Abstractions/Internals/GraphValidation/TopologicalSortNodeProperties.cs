// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.GraphValidation
{
    public interface ITopologicalSortNodeProperties
    {
        bool Visited { get; set; }
    }
}
