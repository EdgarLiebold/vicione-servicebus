namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Transports;


public class ConfigurationHostSettings :
    AmazonSqsHostSettings
{
    AllowTransportHeader? _allowTransportHeader;
    Func<IConnection>? _connectionFactory;
    AWSCredentials? _credentials;
    bool _frozen;
    RegionEndpoint? _region;
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

    public Uri HostAddress => FormatHostAddress();

    public IConnection CreateConnection()
    {
        Freeze();
        return (_connectionFactory ?? throw new InvalidOperationException("The host settings do not have a connection factory."))();
    }

    internal ConfigurationHostSettings Freeze()
    {
        if (_frozen)
            return this;

        _ = FormatHostAddress();
        AWSCredentials? credentials = _credentials;
        RegionEndpoint? region = _region;
        _connectionFactory ??= () => new Connection(credentials, region);
        _frozen = true;
        return this;
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
            throw new ConfigurationException("The Region must be specified");

        return new AmazonSqsHostAddress(Region.SystemName, Scope);
    }

    public override string ToString()
    {
        if (Region?.SystemName == null)
            throw new ConfigurationException("The Region must be specified");

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
