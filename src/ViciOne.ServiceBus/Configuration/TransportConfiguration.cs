using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates transport configuration.</summary>
public class TransportConfiguration :
    ITransportConfiguration,
    ITransportConfigurator
{
    readonly ITransportConfiguration _parent;
    int? _concurrentMessageLimit;
    int? _prefetchCount;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="parent">The parent.</param>
    public TransportConfiguration(ITransportConfiguration parent)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));

        _parent = parent;
    }

    /// <summary>Initializes a new instance.</summary>
    public TransportConfiguration()
    {
        _parent = new DefaultTransportConfiguration();
    }

    /// <summary>Gets the configurator.</summary>
    public ITransportConfigurator Configurator => this;

    /// <summary>Gets or sets the prefetch count.</summary>
    public int PrefetchCount
    {
        get => _prefetchCount ?? _parent.PrefetchCount;
        set => _prefetchCount = value;
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit ?? _parent.ConcurrentMessageLimit;
        set => _concurrentMessageLimit = value;
    }

    /// <summary>Gets concurrent message limit.</summary>
    /// <returns>The concurrent message limit.</returns>
    public int GetConcurrentMessageLimit()
    {
        return ConcurrentMessageLimit ?? PrefetchCount;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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
