using System;
using Amazon;
using Amazon.Runtime;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;
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

    /// <summary>
    /// Gets the allow transport header value.
    /// </summary>
    public AllowTransportHeader? AllowTransportHeader { get; }

    /// <summary>
    /// If true, topics are named "{Scope}_{topicName}" when publishing messages
    /// </summary>
    public bool ScopeTopics { get; }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress { get; }

    /// <summary>
    /// Gets the client context cache options value.
    /// </summary>
    public AmazonSqsClientContextCacheOptions ClientContextCacheOptions { get; }

    internal AWSCredentials? Credentials { get; }

    /// <summary>
    /// Creates connection.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IConnection CreateConnection() => _connectionFactory();

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString() => new UriBuilder
    {
        Scheme = "https",
        Host = Region.SystemName
    }.Uri.ToString();
}
