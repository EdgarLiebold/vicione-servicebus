using System;
using System.Runtime.ExceptionServices;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Owns the Amazon SQS and Amazon SNS clients used by one transport connection.</summary>
public class Connection :
    IConnection
{
    /// <summary>Creates Amazon SQS and Amazon SNS clients for a region.</summary>
    /// <param name="credentials">Explicit AWS credentials, or <see langword="null"/> to use the SDK credential chain.</param>
    /// <param name="regionEndpoint">The AWS region, or <see langword="null"/> to use US East (N. Virginia) when no client configuration is supplied.</param>
    /// <param name="amazonSqsConfig">An optional Amazon SQS client configuration.</param>
    /// <param name="amazonSnsConfig">An optional Amazon SNS client configuration.</param>
    public Connection(AWSCredentials? credentials, RegionEndpoint? regionEndpoint = null, AmazonSQSConfig? amazonSqsConfig = null,
        AmazonSimpleNotificationServiceConfig? amazonSnsConfig = null)
        : this(
            () => CreateSqsClient(credentials, regionEndpoint, amazonSqsConfig),
            () => CreateSnsClient(credentials, regionEndpoint, amazonSnsConfig))
    {
    }

    internal Connection(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        ArgumentNullException.ThrowIfNull(sqsClientFactory);
        ArgumentNullException.ThrowIfNull(snsClientFactory);

        (SqsClient, SnsClient) = CreateClientPair(sqsClientFactory, snsClientFactory);
    }

    static (IAmazonSQS SqsClient, IAmazonSimpleNotificationService SnsClient) CreateClientPair(
        Func<IAmazonSQS> sqsClientFactory,
        Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        IAmazonSQS sqsClient = sqsClientFactory() ?? throw new InvalidOperationException("The SQS client factory returned null.");
        try
        {
            IAmazonSimpleNotificationService snsClient = snsClientFactory()
                ?? throw new InvalidOperationException("The SNS client factory returned null.");
            return (sqsClient, snsClient);
        }
        catch (Exception primaryException)
        {
            try
            {
                sqsClient.Dispose();
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Creating the Amazon SNS client and disposing the partially created SQS client failed.",
                    primaryException,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(primaryException).Throw();
            throw;
        }
    }

    static IAmazonSQS CreateSqsClient(AWSCredentials? credentials, RegionEndpoint? regionEndpoint, AmazonSQSConfig? config)
    {
        config ??= new AmazonSQSConfig { RegionEndpoint = regionEndpoint ?? RegionEndpoint.USEast1 };
        return credentials == null
            ? new AmazonSQSClient(config)
            : new AmazonSQSClient(credentials, config);
    }

    static IAmazonSimpleNotificationService CreateSnsClient(
        AWSCredentials? credentials,
        RegionEndpoint? regionEndpoint,
        AmazonSimpleNotificationServiceConfig? config)
    {
        config ??= new AmazonSimpleNotificationServiceConfig { RegionEndpoint = regionEndpoint ?? RegionEndpoint.USEast1 };
        return credentials == null
            ? new AmazonSimpleNotificationServiceClient(config)
            : new AmazonSimpleNotificationServiceClient(credentials, config);
    }

    /// <summary>Gets the Amazon SQS client.</summary>
    public IAmazonSQS SqsClient { get; }
    /// <summary>Gets the Amazon SNS client.</summary>
    public IAmazonSimpleNotificationService SnsClient { get; }

    /// <summary>Disposes both Amazon clients and preserves any failures from either disposal.</summary>
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
