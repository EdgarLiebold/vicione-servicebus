using System;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Defines read-only access to RabbitMQ client-side publish-batch settings.</summary>
public interface BatchSettings
{
    /// <summary>Indicates whether client-side publish batching is enabled.</summary>
    bool Enabled { get; }

    /// <summary>The maximum number of messages to include in a batch.</summary>
    int MessageLimit { get; }

    /// <summary>Gets the approximate combined message-body limit in bytes.</summary>
    int SizeLimit { get; }

    /// <summary>Gets the maximum time to collect messages before publishing a partial batch.</summary>
    TimeSpan Timeout { get; }
}
