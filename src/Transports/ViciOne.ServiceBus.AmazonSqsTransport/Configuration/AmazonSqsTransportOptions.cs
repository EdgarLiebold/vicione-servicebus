// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public class AmazonSqsTransportOptions
{
    public string? Region { get; set; }
    public string? Scope { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}
