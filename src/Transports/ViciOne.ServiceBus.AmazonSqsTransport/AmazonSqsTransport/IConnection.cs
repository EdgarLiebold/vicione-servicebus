using System;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface IConnection :
    IDisposable
{
    IAmazonSQS SqsClient { get; }

    IAmazonSimpleNotificationService SnsClient { get; }
}
