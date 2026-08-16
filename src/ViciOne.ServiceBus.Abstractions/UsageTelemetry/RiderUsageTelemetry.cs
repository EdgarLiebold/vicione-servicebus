namespace ViciOne.ServiceBus.UsageTelemetry;

using System.Collections.Generic;


public class RiderUsageTelemetry
{
    public string? RiderType { get; set; }
    public List<EndpointUsageTelemetry>? Endpoints { get; set; }
}
