using System;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures AWS credentials, entity scoping, clients, caching, and header filtering for an Amazon SQS host.</summary>
public interface IAmazonSqsHostConfigurator
{
    /// <summary>
    /// Sets explicit credentials for Amazon SQS and Amazon SNS clients.
    /// See <see href="https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-config-creds.html">Configure AWS credentials</see> for the ways to supply them.
    /// </summary>
    /// <param name="credentials">The AWS credentials.</param>
    void Credentials(AWSCredentials credentials);

    /// <summary>Sets the entity-name prefix and optionally applies it to topics as well as queues.</summary>
    /// <param name="scope">The entity-name prefix.</param>
    /// <param name="scopeTopics">Whether topic names also receive the prefix.</param>
    void Scope(string scope, bool scopeTopics = false);

    /// <summary>Applies the configured scope prefix to Amazon SNS topic names.</summary>
    void EnableScopedTopics();

    /// <summary>
    /// Supplies factories for advanced client configuration, including test-owned local emulators.
    /// Each reconnect obtains a fresh pair of clients, and the transport owns their disposal.
    /// </summary>
    /// <param name="sqsClientFactory">Creates a fresh Amazon SQS client.</param>
    /// <param name="snsClientFactory">Creates a fresh Amazon SNS client.</param>
    void ClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory);

    /// <summary>Configures the immutable queue and topic client-context cache for this host.</summary>
    /// <param name="options">Validated per-host cache options.</param>
    void ClientContextCache(AmazonSqsClientContextCacheOptions options);

    /// <summary>Sets the predicate used to include or reject outbound transport headers.</summary>
    /// <param name="allowTransportHeader">The header predicate, or <see langword="null"/> to allow every convertible header.</param>
    void AllowTransportHeader(AllowTransportHeader? allowTransportHeader);
}
