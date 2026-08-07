// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

public interface AmazonSqsSendContext<out T> :
    AmazonSqsSendContext,
    SendContext<T>
    where T : class
{
}


public interface AmazonSqsSendContext :
    SendContext
{
    string? GroupId { set; }
    string? DeduplicationId { set; }
    int? DelaySeconds { set; }
}
