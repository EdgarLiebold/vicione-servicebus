using System.Collections.Generic;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus;

public interface AmazonSqsMessageContext
{
    Message TransportMessage { get; }

    Dictionary<string, MessageAttributeValue> Attributes { get; }
}
