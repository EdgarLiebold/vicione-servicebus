using System;
using Amazon;
using Amazon.Runtime;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Immutable settings for an Amazon SQS host produced by the typed host configurator.</summary>
public sealed class AmazonSqsHostSettings
{
    readonly Func<IConnection> _connectionFactory;

    internal AmazonSqsHostSettings(
        RegionEndpoint region,
        AllowTransportHeader? allowTransportHeader,
        bool scopeTopics,
        Uri hostAddress,
        AmazonSqsClientContextCacheOptions clientContextCacheOptions,
        Func<IConnection> connectionFactory,
        AWSCredentials? credentials)
    {
        Region = region ?? throw new ArgumentNullException(nameof(region));
        HostAddress = hostAddress ?? throw new ArgumentNullException(nameof(hostAddress));
        ClientContextCacheOptions = clientContextCacheOptions ?? throw new ArgumentNullException(nameof(clientContextCacheOptions));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        AllowTransportHeader = allowTransportHeader;
        ScopeTopics = scopeTopics;
        Credentials = credentials;
    }

    /// <summary>Gets the AWS region used by Amazon SQS and Amazon SNS clients.</summary>
    public RegionEndpoint Region { get; }

    /// <summary>Gets the optional predicate that permits transport-specific headers.</summary>
    public AllowTransportHeader? AllowTransportHeader { get; }

    /// <summary>Gets whether Amazon SNS topic names are prefixed with the host scope.</summary>
    public bool ScopeTopics { get; }

    /// <summary>Gets the canonical Amazon SQS transport host address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Gets the queue and topic cache limits.</summary>
    public AmazonSqsClientContextCacheOptions ClientContextCacheOptions { get; }

    internal AWSCredentials? Credentials { get; }

    /// <summary>Creates the configured AWS client connection.</summary>
    /// <returns>A connection containing Amazon SQS and Amazon SNS clients.</returns>
    public IConnection CreateConnection() => _connectionFactory();

    /// <summary>Returns the HTTPS endpoint for the configured AWS region.</summary>
    /// <returns>The region endpoint URI.</returns>
    public override string ToString() => new UriBuilder
    {
        Scheme = "https",
        Host = Region.SystemName
    }.Uri.ToString();
}
