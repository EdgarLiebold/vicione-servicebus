using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a transport configuration implementation.
/// </summary>
public class TransportConfiguration :
    ITransportConfiguration,
    ITransportConfigurator
{
    readonly ITransportConfiguration _parent;
    int? _concurrentMessageLimit;
    int? _prefetchCount;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="parent">The parent value.</param>
    public TransportConfiguration(ITransportConfiguration parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        _parent = parent;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TransportConfiguration()
    {
        _parent = new DefaultTransportConfiguration();
    }

    /// <summary>
    /// Gets the configurator value.
    /// </summary>
    public ITransportConfigurator Configurator => this;

    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int PrefetchCount
    {
        get => _prefetchCount ?? _parent.PrefetchCount;
        set => _prefetchCount = value;
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit ?? _parent.ConcurrentMessageLimit;
        set => _concurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets concurrent message limit.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public int GetConcurrentMessageLimit()
    {
        return ConcurrentMessageLimit ?? PrefetchCount;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (PrefetchCount < ConcurrentMessageLimit)
            yield return this.Warning("ConcurrentMessageLimit", "Should be <= PrefetchCount");
        if (ConcurrentMessageLimit <= 0)
            yield return this.Failure("ConcurrentMessageLimit", "Must be > 0");
        if (PrefetchCount < 0)
            yield return this.Failure("PrefetchCount", "Must be >= 0");
    }


    class DefaultTransportConfiguration :
        ITransportConfiguration
    {
        public DefaultTransportConfiguration()
        {
            PrefetchCount = Math.Max(Environment.ProcessorCount * 2, 16);
        }

        public ITransportConfigurator Configurator => throw new InvalidOperationException("The default transport configuration cannot be configured");
        public int PrefetchCount { get; }
        public int? ConcurrentMessageLimit => default;

        public int GetConcurrentMessageLimit()
        {
            return ConcurrentMessageLimit ?? PrefetchCount;
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }
}
