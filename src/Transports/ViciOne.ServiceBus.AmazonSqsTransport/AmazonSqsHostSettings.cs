namespace ViciOne.ServiceBus;

using System;
using Amazon;
using Amazon.Runtime;
using AmazonSqsTransport;
using Transports;


/// <summary>
/// Immutable settings for an Amazon SQS host produced by the typed host configurator.
/// </summary>
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

    /// <summary>
    /// The AmazonSQS region to connect
    /// </summary>
    public RegionEndpoint Region { get; }

    public AllowTransportHeader? AllowTransportHeader { get; }

    /// <summary>
    /// If true, topics are named "{Scope}_{topicName}" when publishing messages
    /// </summary>
    public bool ScopeTopics { get; }

    public Uri HostAddress { get; }

    public AmazonSqsClientContextCacheOptions ClientContextCacheOptions { get; }

    internal AWSCredentials? Credentials { get; }

    public IConnection CreateConnection() => _connectionFactory();

    public override string ToString() => new UriBuilder
    {
        Scheme = "https",
        Host = Region.SystemName
    }.Uri.ToString();
}
