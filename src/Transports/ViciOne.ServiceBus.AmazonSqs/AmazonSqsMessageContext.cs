using System.Collections.Generic;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message context.
/// </summary>
public interface AmazonSqsMessageContext
{
    /// <summary>
    /// Gets the transport message value.
    /// </summary>
    Message TransportMessage { get; }

    /// <summary>
    /// Gets the attributes value.
    /// </summary>
    Dictionary<string, MessageAttributeValue> Attributes { get; }
}
