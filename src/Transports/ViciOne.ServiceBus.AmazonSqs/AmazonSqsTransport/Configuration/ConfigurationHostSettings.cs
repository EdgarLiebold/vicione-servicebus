using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

internal sealed class ConfigurationHostSettings
{
    AllowTransportHeader? _allowTransportHeader;
    Func<IConnection>? _connectionFactory;
    AWSCredentials? _credentials;
    bool _frozen;
    RegionEndpoint? _region;
    AmazonSqsClientContextCacheOptions _clientContextCacheOptions = new();
    AmazonSqsHostSettings? _snapshot;
    string? _scope;
    bool _scopeTopics;

    internal AWSCredentials? Credentials
    {
        get => _credentials;
        set
        {
            ThrowIfFrozen();
            _credentials = value;
        }
    }

    internal string? Scope
    {
        get => _scope;
        set
        {
            ThrowIfFrozen();
            _scope = value;
        }
    }

    public RegionEndpoint? Region
    {
        get => _region;
        internal set
        {
            ThrowIfFrozen();
            _region = value;
        }
    }

    public AllowTransportHeader? AllowTransportHeader
    {
        get => _allowTransportHeader;
        internal set
        {
            ThrowIfFrozen();
            _allowTransportHeader = value;
        }
    }

    public bool ScopeTopics
    {
        get => _scopeTopics;
        internal set
        {
            ThrowIfFrozen();
            _scopeTopics = value;
        }
    }

    internal AmazonSqsClientContextCacheOptions ClientContextCacheOptions
    {
        get => _clientContextCacheOptions;
        set
        {
            ThrowIfFrozen();
            _clientContextCacheOptions = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    internal AmazonSqsHostSettings Freeze()
    {
        if (_snapshot != null)
            return _snapshot;

        Uri hostAddress = FormatHostAddress();
        AWSCredentials? credentials = _credentials;
        RegionEndpoint region = _region!;
        Func<IConnection> connectionFactory = _connectionFactory ?? (() => new Connection(credentials, region));
        _frozen = true;
        return _snapshot = new AmazonSqsHostSettings(
            region,
            _allowTransportHeader,
            _scopeTopics,
            hostAddress,
            _clientContextCacheOptions,
            connectionFactory,
            credentials);
    }

    internal void SetClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        ThrowIfFrozen();
        ArgumentNullException.ThrowIfNull(sqsClientFactory);
        ArgumentNullException.ThrowIfNull(snsClientFactory);

        if (_credentials != null)
            throw new InvalidOperationException("Explicit AWS credentials and custom client factories are mutually exclusive.");

        _connectionFactory = () => new Connection(sqsClientFactory, snsClientFactory);
    }

    internal void SetCredentials(AWSCredentials credentials)
    {
        ThrowIfFrozen();
        ArgumentNullException.ThrowIfNull(credentials);

        if (_connectionFactory != null)
            throw new InvalidOperationException("Custom client factories and explicit AWS credentials are mutually exclusive.");

        _credentials = credentials;
    }

    Uri FormatHostAddress()
    {
        if (Region?.SystemName == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Amazon SQS", "unknown", "The Region must be specified", "Correct the named configuration before starting the host"));

        return new AmazonSqsHostAddress(Region.SystemName, Scope);
    }

    public override string ToString()
    {
        if (Region?.SystemName == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Amazon SQS", "unknown", "The Region must be specified", "Correct the named configuration before starting the host"));

        return new UriBuilder
        {
            Scheme = "https",
            Host = Region.SystemName
        }.Uri.ToString();
    }

    void ThrowIfFrozen()
    {
        if (_frozen)
            throw new InvalidOperationException("Amazon SQS host settings are immutable after they are assigned to a host.");
    }
}
