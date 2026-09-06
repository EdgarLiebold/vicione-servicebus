namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Describes an Amazon SNS topic subscription targeting an Amazon SQS queue.</summary>
public interface QueueSubscription
{
    /// <summary>Gets the source Amazon SNS topic.</summary>
    Topic Source { get; }

    /// <summary>Gets the destination Amazon SQS queue.</summary>
    Queue Destination { get; }
}
