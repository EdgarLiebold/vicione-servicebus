using System;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Owns the Amazon SQS and Amazon SNS clients for one transport connection.</summary>
public interface IConnection :
    IDisposable
{
    /// <summary>Gets the Amazon SQS client.</summary>
    IAmazonSQS SqsClient { get; }

    /// <summary>Gets the Amazon SNS client.</summary>
    IAmazonSimpleNotificationService SnsClient { get; }
}
