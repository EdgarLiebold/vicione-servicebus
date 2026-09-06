using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Builds immutable Amazon SQS and Amazon SNS host settings.</summary>
public class AmazonSqsHostConfigurator :
    IAmazonSqsHostConfigurator
{
    readonly ConfigurationHostSettings _settings;

    /// <summary>Initializes host settings from an Amazon SQS host address.</summary>
    /// <param name="address">The host address containing the AWS region and optional scope.</param>
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

    /// <summary>Gets an immutable snapshot of the configured host settings.</summary>
    public AmazonSqsHostSettings Settings => _settings.Freeze();

    /// <summary>Sets the entity-name scope and optionally applies it to topics.</summary>
    /// <param name="scope">The prefix applied to scoped entity names.</param>
    /// <param name="scopeTopics">Whether topic names also receive the scope prefix.</param>
    public void Scope(string scope, bool scopeTopics)
    {
        _settings.Scope = scope;

        if (scopeTopics)
            EnableScopedTopics();
    }

    /// <summary>Applies the configured scope prefix to topic names as well as queue names.</summary>
    public void EnableScopedTopics()
    {
        _settings.ScopeTopics = true;
    }

    /// <summary>Sets the AWS credentials used to create Amazon SQS and Amazon SNS clients.</summary>
    /// <param name="credentials">The AWS credentials.</param>
    public void Credentials(AWSCredentials credentials)
    {
        _settings.SetCredentials(credentials);
    }

    /// <summary>Sets custom factories for Amazon SQS and Amazon SNS clients.</summary>
    /// <param name="sqsClientFactory">The Amazon SQS client factory.</param>
    /// <param name="snsClientFactory">The Amazon SNS client factory.</param>
    public void ClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        _settings.SetClientFactories(sqsClientFactory, snsClientFactory);
    }

    /// <summary>Configures queue and topic metadata caching.</summary>
    /// <param name="options">The cache lifetime and capacity options.</param>
    public void ClientContextCache(AmazonSqsClientContextCacheOptions options)
    {
        _settings.ClientContextCacheOptions = options;
    }

    /// <summary>Sets the predicate used to include or reject outbound transport headers.</summary>
    /// <param name="allowTransportHeader">The header predicate, or <see langword="null"/> to use the default policy.</param>
    public void AllowTransportHeader(AllowTransportHeader? allowTransportHeader)
    {
        _settings.AllowTransportHeader = allowTransportHeader;
    }

}
