// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using Amazon.SimpleNotificationService;
using Amazon.SQS;


public interface IConnection :
    IDisposable
{
    IAmazonSQS SqsClient { get; }

    IAmazonSimpleNotificationService SnsClient { get; }
}
