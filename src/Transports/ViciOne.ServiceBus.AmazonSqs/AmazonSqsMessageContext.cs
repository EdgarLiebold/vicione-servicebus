using System.Collections.Generic;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Exposes Amazon SQS-native state for a received message.</summary>
public interface AmazonSqsMessageContext
{
    /// <summary>Gets the underlying AWS SDK message.</summary>
    Message TransportMessage { get; }

    /// <summary>Gets the native message attributes.</summary>
    Dictionary<string, MessageAttributeValue> Attributes { get; }
}
