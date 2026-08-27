namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using System.Runtime.ExceptionServices;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;


public class Connection :
    IConnection
{
    public Connection(AWSCredentials? credentials, RegionEndpoint? regionEndpoint = null, AmazonSQSConfig? amazonSqsConfig = null,
        AmazonSimpleNotificationServiceConfig? amazonSnsConfig = null)
    {
        amazonSqsConfig ??= new AmazonSQSConfig { RegionEndpoint = regionEndpoint ?? RegionEndpoint.USEast1 };
        amazonSnsConfig ??= new AmazonSimpleNotificationServiceConfig { RegionEndpoint = regionEndpoint ?? RegionEndpoint.USEast1 };

        SqsClient = credentials == null
            ? new AmazonSQSClient(amazonSqsConfig)
            : new AmazonSQSClient(credentials, amazonSqsConfig);

        SnsClient = credentials == null
            ? new AmazonSimpleNotificationServiceClient(amazonSnsConfig)
            : new AmazonSimpleNotificationServiceClient(credentials, amazonSnsConfig);
    }

    internal Connection(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        ArgumentNullException.ThrowIfNull(sqsClientFactory);
        ArgumentNullException.ThrowIfNull(snsClientFactory);

        SqsClient = sqsClientFactory() ?? throw new InvalidOperationException("The SQS client factory returned null.");
        try
        {
            SnsClient = snsClientFactory() ?? throw new InvalidOperationException("The SNS client factory returned null.");
        }
        catch
        {
            SqsClient.Dispose();
            throw;
        }
    }

    public IAmazonSQS SqsClient { get; }
    public IAmazonSimpleNotificationService SnsClient { get; }

    public void Dispose()
    {
        Exception? snsException = null;
        Exception? sqsException = null;

        try
        {
            SnsClient.Dispose();
        }
        catch (Exception exception)
        {
            snsException = exception;
        }

        try
        {
            SqsClient.Dispose();
        }
        catch (Exception exception)
        {
            sqsException = exception;
        }

        if (snsException != null && sqsException != null)
            throw new AggregateException("Disposing the Amazon SNS and SQS clients failed.", snsException, sqsException);

        if (snsException != null)
            ExceptionDispatchInfo.Capture(snsException).Throw();
        if (sqsException != null)
            ExceptionDispatchInfo.Capture(sqsException).Throw();
    }
}
