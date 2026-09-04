using System;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for connection.
/// </summary>
public interface IConnection :
    IDisposable
{
    /// <summary>
    /// Gets the sqs client value.
    /// </summary>
    IAmazonSQS SqsClient { get; }

    /// <summary>
    /// Gets the sns client value.
    /// </summary>
    IAmazonSimpleNotificationService SnsClient { get; }
}
