// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Diagnostics;


public interface MetricsContext
{
    void Populate(ref TagList tagList);
}
