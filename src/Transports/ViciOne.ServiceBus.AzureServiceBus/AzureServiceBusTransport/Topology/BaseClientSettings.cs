using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a base client settings implementation.
/// </summary>
public abstract class BaseClientSettings :
    ClientSettings
{
    readonly IServiceBusEndpointConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="configurator">The configurator value.</param>
    protected BaseClientSettings(IServiceBusEndpointConfiguration configuration, IServiceBusEndpointEntityConfigurator configurator)
    {
        _configuration = configuration;
        Configurator = configurator;

        MaxAutoRenewDuration = Defaults.MaxAutoRenewDuration;
        SessionIdleTimeout = Defaults.SessionIdleTimeout;
    }

    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    public IServiceBusEndpointEntityConfigurator Configurator { get; }

    /// <summary>
    /// Gets the requires session value.
    /// </summary>
    public abstract bool RequiresSession { get; }
    /// <summary>
    /// Gets the max concurrent sessions value.
    /// </summary>
    public abstract int MaxConcurrentSessions { get; }
    /// <summary>
    /// Gets the max concurrent calls per session value.
    /// </summary>
    public abstract int MaxConcurrentCallsPerSession { get; }

    /// <summary>
    /// Gets or sets the session idle timeout value.
    /// </summary>
    public TimeSpan? SessionIdleTimeout { get; set; }

    /// <summary>
    /// Gets the max concurrent calls value.
    /// </summary>
    public int MaxConcurrentCalls => Math.Max(_configuration.Transport.GetConcurrentMessageLimit(), 1);
    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int PrefetchCount => _configuration.Transport.PrefetchCount;

    /// <summary>
    /// Gets or sets the max auto renew duration value.
    /// </summary>
    public TimeSpan MaxAutoRenewDuration { get; set; }

    /// <summary>
    /// Gets the path value.
    /// </summary>
    public abstract string Path { get; }

    /// <summary>
    /// Gets or sets the name value.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets input address.
    /// </summary>
    /// <param name="serviceUri">The service uri value.</param>
    /// <param name="path">The path value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetInputAddress(Uri serviceUri, string path)
    {
        var builder = new UriBuilder(serviceUri) { Path = path };

        builder.Query += string.Join("&", GetQueryStringOptions());

        return builder.Uri;
    }

    /// <summary>
    /// Gets query string options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected abstract IEnumerable<string> GetQueryStringOptions();
}
