// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Collections.Generic;
using Amazon.SQS.Model;


public interface AmazonSqsMessageContext
{
    Message TransportMessage { get; }

    Dictionary<string, MessageAttributeValue> Attributes { get; }
}
