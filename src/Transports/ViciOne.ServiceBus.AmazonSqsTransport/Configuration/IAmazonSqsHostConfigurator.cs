using System;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public interface IAmazonSqsHostConfigurator
{
    /// <summary>
    /// Sets the credentials for the connection to AmazonSQS/AmazonSNS
    /// See <see href="https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-config-creds.html">Configure AWS credentials</see> for the ways to supply them.
    /// </summary>
    /// <param name="credentials"></param>
    void Credentials(AWSCredentials credentials);

    /// <summary>
    /// Set scope for AmazonSQS. Will be used as a prefix for queue/topic name
    /// </summary>
    /// <param name="scope"></param>
    /// <param name="scopeTopics">If true, topics will be scoped to the host scope</param>
    void Scope(string scope, bool scopeTopics = false);

    /// <summary>
    /// Enable the scoping of topics to use the host scope (specified via the <see cref="Scope"/> method.
    /// </summary>
    void EnableScopedTopics();

    /// <summary>
    /// Supplies factories for advanced client configuration, including test-owned local emulators.
    /// Each reconnect obtains a fresh pair of clients, and the transport owns their disposal.
    /// </summary>
    /// <param name="sqsClientFactory">Creates a fresh SQS client.</param>
    /// <param name="snsClientFactory">Creates a fresh SNS client.</param>
    void ClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory);

    /// <summary>
    /// Configures the immutable queue and topic client-context cache for this host.
    /// </summary>
    /// <param name="options">Validated per-host cache options.</param>
    void ClientContextCache(AmazonSqsClientContextCacheOptions options);

    /// <summary>
    /// Specifies a method used to determine if a header should be copied to the transport message
    /// </summary>
    /// <param name="allowTransportHeader"></param>
    void AllowTransportHeader(AllowTransportHeader? allowTransportHeader);
}
