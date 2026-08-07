// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.UsageTelemetry;

using System.Collections.Generic;


public class RiderUsageTelemetry
{
    public string? RiderType { get; set; }
    public List<EndpointUsageTelemetry>? Endpoints { get; set; }
}
