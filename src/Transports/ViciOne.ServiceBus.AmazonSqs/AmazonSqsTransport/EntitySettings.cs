using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines common naming, lifetime, and tagging settings for an Amazon SQS queue or Amazon SNS topic.</summary>
public interface EntitySettings
{
    /// <summary>Gets the queue or topic name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the transport retains the entity when its endpoint stops.</summary>
    bool Durable { get; }

    /// <summary>Gets whether the transport deletes the queue or topic when the bus stops.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets the tags assigned to the queue or topic when it is created.</summary>
    IDictionary<string, string> Tags { get; }
}
