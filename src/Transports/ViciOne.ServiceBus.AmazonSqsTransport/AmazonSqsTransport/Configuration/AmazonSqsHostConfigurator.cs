namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Transports;


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

        if (!string.IsNullOrEmpty(address.UserInfo))
            throw new AmazonSqsTransportConfigurationException("Credentials must not be embedded in an Amazon SQS host URI. Use the AWS SDK credential chain or Credentials(AWSCredentials).");
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

    public void AllowTransportHeader(AllowTransportHeader? allowTransportHeader)
    {
        _settings.AllowTransportHeader = allowTransportHeader;
    }

}
