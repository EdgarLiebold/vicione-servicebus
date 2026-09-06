using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides common Azure Service Bus entity and processor settings for queue and subscription clients.</summary>
public abstract class BaseClientSettings :
    ClientSettings
{
    readonly IServiceBusEndpointConfiguration _configuration;

    /// <summary>Creates client settings from an endpoint configuration and entity configurator.</summary>
    /// <param name="configuration">The endpoint configuration supplying transport concurrency and prefetch settings.</param>
    /// <param name="configurator">The provider entity configuration.</param>
    protected BaseClientSettings(IServiceBusEndpointConfiguration configuration, IServiceBusEndpointEntityConfigurator configurator)
    {
        _configuration = configuration;
        Configurator = configurator;

        MaxAutoRenewDuration = Defaults.MaxAutoRenewDuration;
        SessionIdleTimeout = Defaults.SessionIdleTimeout;
    }

    /// <summary>Gets the provider entity configuration.</summary>
    public IServiceBusEndpointEntityConfigurator Configurator { get; }

    /// <summary>Gets whether the entity requires sessions.</summary>
    public abstract bool RequiresSession { get; }
    /// <summary>Gets the maximum number of sessions processed concurrently.</summary>
    public abstract int MaxConcurrentSessions { get; }
    /// <summary>Gets the maximum number of concurrent message callbacks per session.</summary>
    public abstract int MaxConcurrentCallsPerSession { get; }

    /// <summary>Gets or sets how long a session processor waits for another message before releasing the session.</summary>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>Gets the endpoint's effective concurrent message limit, with a minimum of one.</summary>
    public int MaxConcurrentCalls => Math.Max(_configuration.Transport.GetConcurrentMessageLimit(), 1);
    /// <summary>Gets the number of messages prefetched by the processor.</summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;

    /// <summary>Gets or sets how long the processor automatically renews message or session locks.</summary>
    public TimeSpan MaxAutoRenewDuration { get; set; }

    /// <summary>Gets the namespace-relative queue or subscription path.</summary>
    public abstract string Path { get; }

    /// <summary>Gets or sets the processor entity name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Builds an input address including non-default processor options.</summary>
    /// <param name="serviceUri">The Azure Service Bus namespace address.</param>
    /// <param name="path">The namespace-relative entity path.</param>
    /// <returns>The absolute endpoint input address.</returns>
    public Uri GetInputAddress(Uri serviceUri, string path)
    {
        var builder = new UriBuilder(serviceUri) { Path = path };

        builder.Query += string.Join("&", GetQueryStringOptions());

        return builder.Uri;
    }

    /// <summary>Formats non-default processor options for the endpoint query string.</summary>
    /// <returns>The encoded option fragments.</returns>
    protected abstract IEnumerable<string> GetQueryStringOptions();
}
