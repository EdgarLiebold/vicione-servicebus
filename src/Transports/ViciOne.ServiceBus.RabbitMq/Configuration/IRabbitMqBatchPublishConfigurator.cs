using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ client-side publish batching.</summary>
public interface IRabbitMqBatchPublishConfigurator
{
    /// <summary>Enables client-side batching to reduce broker round trips.</summary>
    bool Enabled { set; }

    /// <summary>The maximum number of messages to include in a batch.</summary>
    int MessageLimit { set; }

    /// <summary>The approximate maximum combined message-body size of a batch, in bytes.</summary>
    int SizeLimit { set; }

    /// <summary>The maximum time to collect messages before publishing a partial batch.</summary>
    TimeSpan Timeout { set; }
}
