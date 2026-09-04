using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

public class AmazonSqsHostConfigurator :
    IAmazonSqsHostConfigurator
{
    readonly ConfigurationHostSettings _settings;

    public AmazonSqsHostConfigurator(Uri address)
    {
        var hostAddress = new AmazonSqsHostAddress(address);

        var regionEndpoint = RegionEndpoint.GetBySystemName(hostAddress.Host);

        _settings = new ConfigurationHostSettings
        {
            Scope = hostAddress.Scope,
            Region = regionEndpoint
        };

    }

    public AmazonSqsHostSettings Settings => _settings.Freeze();

    public void Scope(string scope, bool scopeTopics)
    {
        _settings.Scope = scope;

        if (scopeTopics)
            EnableScopedTopics();
    }

    public void EnableScopedTopics()
    {
        _settings.ScopeTopics = true;
    }

    public void Credentials(AWSCredentials credentials)
    {
        _settings.SetCredentials(credentials);
    }

    public void ClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        _settings.SetClientFactories(sqsClientFactory, snsClientFactory);
    }

    public void ClientContextCache(AmazonSqsClientContextCacheOptions options)
    {
        _settings.ClientContextCacheOptions = options;
    }

    public void AllowTransportHeader(AllowTransportHeader? allowTransportHeader)
    {
        _settings.AllowTransportHeader = allowTransportHeader;
    }

}
