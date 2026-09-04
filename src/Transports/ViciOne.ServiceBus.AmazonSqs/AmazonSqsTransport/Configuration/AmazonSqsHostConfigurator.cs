using System;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs host configurator implementation.
/// </summary>
public class AmazonSqsHostConfigurator :
    IAmazonSqsHostConfigurator
{
    readonly ConfigurationHostSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
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

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public AmazonSqsHostSettings Settings => _settings.Freeze();

    /// <summary>
    /// Performs the scope operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="scopeTopics">The scope topics value.</param>
    public void Scope(string scope, bool scopeTopics)
    {
        _settings.Scope = scope;

        if (scopeTopics)
            EnableScopedTopics();
    }

    /// <summary>
    /// Performs the enable scoped topics operation.
    /// </summary>
    public void EnableScopedTopics()
    {
        _settings.ScopeTopics = true;
    }

    /// <summary>
    /// Performs the credentials operation.
    /// </summary>
    /// <param name="credentials">The credentials value.</param>
    public void Credentials(AWSCredentials credentials)
    {
        _settings.SetCredentials(credentials);
    }

    /// <summary>
    /// Performs the client factories operation.
    /// </summary>
    /// <param name="sqsClientFactory">The sqs client factory value.</param>
    /// <param name="snsClientFactory">The sns client factory value.</param>
    public void ClientFactories(Func<IAmazonSQS> sqsClientFactory, Func<IAmazonSimpleNotificationService> snsClientFactory)
    {
        _settings.SetClientFactories(sqsClientFactory, snsClientFactory);
    }

    /// <summary>
    /// Performs the client context cache operation.
    /// </summary>
    /// <param name="options">The options value.</param>
    public void ClientContextCache(AmazonSqsClientContextCacheOptions options)
    {
        _settings.ClientContextCacheOptions = options;
    }

    /// <summary>
    /// Performs the allow transport header operation.
    /// </summary>
    /// <param name="allowTransportHeader">The allow transport header value.</param>
    public void AllowTransportHeader(AllowTransportHeader? allowTransportHeader)
    {
        _settings.AllowTransportHeader = allowTransportHeader;
    }

}
