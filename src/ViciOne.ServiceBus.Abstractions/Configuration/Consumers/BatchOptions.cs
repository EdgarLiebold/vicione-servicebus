using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Batch options are applied to a <see cref="Batch{T}" /> consumer to configure
/// the size and time limits for each batch.
/// </summary>
public sealed class BatchOptions :
    IOptions,
    IConfigureReceiveEndpoint,
    ISpecification
{
    /// <summary>Override the default receive endpoint configuration done by the batch options.</summary>
    /// <param name="name">The name.</param>
    /// <param name="configurator">The configurator to update.</param>
    public delegate void ConfigurationCallback(string? name, IReceiveEndpointConfigurator configurator);


    ConfigurationCallback _configurationCallback;

    /// <summary>Initializes a new instance.</summary>
    public BatchOptions()
    {
        ConcurrencyLimit = 1;
        MessageLimit = 10;
        TimeLimit = TimeSpan.FromSeconds(1);
        TimeLimitStart = BatchTimeLimitStart.FromFirst;

        _configurationCallback = DefaultConfigurationCallback;
    }

    /// <summary>The maximum number of messages in a single batch.</summary>
    public int MessageLimit { get; set; }

    /// <summary>The number of batches which can be executed concurrently.</summary>
    public int ConcurrencyLimit { get; set; }

    /// <summary>The maximum time to wait before delivering a partial batch.</summary>
    public TimeSpan TimeLimit { get; set; }

    /// <summary>The starting point for the <see cref="TimeLimit" />.</summary>
    public BatchTimeLimitStart TimeLimitStart { get; set; }

    /// <summary>The property to group by.</summary>
    public object? GroupKeyProvider { get; private set; }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="configurator">The configurator to update.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        _configurationCallback(name, configurator);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (TimeLimit <= TimeSpan.Zero)
            yield return this.Failure("Batch", "TimeLimit", "Must be > TimeSpan.Zero");
        if (MessageLimit <= 0)
            yield return this.Failure("Batch", "MessageLimit", "Must be > 0");
        if (ConcurrencyLimit <= 0)
            yield return this.Failure("Batch", "ConcurrencyLimit", "Must be > 0");
        if (!Enum.IsDefined(TimeLimitStart))
            yield return this.Failure("Batch", "TimeLimitStart", "Must be a defined BatchTimeLimitStart value");
    }

    /// <summary>Sets configuration callback.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetConfigurationCallback(ConfigurationCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        _configurationCallback = callback;
        return this;
    }

    void DefaultConfigurationCallback(string? name, IReceiveEndpointConfigurator configurator)
    {
        var messageCapacity = ConcurrencyLimit * MessageLimit;

        configurator.PrefetchCount = Math.Max(messageCapacity, configurator.PrefetchCount);

        if (configurator.ConcurrentMessageLimit < messageCapacity)
            configurator.ConcurrentMessageLimit = messageCapacity;
    }

    /// <summary>Sets the maximum number of messages in a single batch.</summary>
    /// <param name="limit">The message limit.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetMessageLimit(int limit)
    {
        MessageLimit = limit;
        return this;
    }

    /// <summary>Sets the number of batches which can be executed concurrently.</summary>
    /// <param name="limit">The message limit.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetConcurrencyLimit(int limit)
    {
        ConcurrencyLimit = limit;
        return this;
    }

    /// <summary>Sets the maximum time to wait before delivering a partial batch.</summary>
    /// <param name="limit">The message limit.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetTimeLimit(TimeSpan limit)
    {
        TimeLimit = limit;
        return this;
    }

    /// <summary>Sets the starting point for the <see cref="TimeLimit" />.</summary>
    /// <param name="timeLimitStart">The starting point.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetTimeLimitStart(BatchTimeLimitStart timeLimitStart)
    {
        TimeLimitStart = timeLimitStart;
        return this;
    }

    /// <summary>Sets the maximum time to wait before delivering a partial batch.</summary>
    /// <param name="ms">The ms.</param>
    /// <param name="s">The <c>s</c> value.</param>
    /// <param name="m">The <c>m</c> value.</param>
    /// <param name="h">The <c>h</c> value.</param>
    /// <param name="d">The <c>d</c> value.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions SetTimeLimit(int? ms = default, int? s = default, int? m = default, int? h = default, int? d = default)
    {
        var timeSpan = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);
        if (timeSpan <= TimeSpan.Zero)
            throw new ArgumentException("The timeout must be > 0");

        TimeLimit = timeSpan;
        return this;
    }

    /// <summary>Groups values using the supplied key selector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions GroupBy<T, TProperty>(Func<ConsumeContext<T>, TProperty?> provider)
        where T : class
        where TProperty : struct
    {
        GroupKeyProvider = new ValueTypeGroupKeyProvider<T, TProperty>(provider);

        return this;
    }

    /// <summary>Groups values using the supplied key selector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The batch options produced by the operation.</returns>
    public BatchOptions GroupBy<T, TProperty>(Func<ConsumeContext<T>, TProperty> provider)
        where T : class
        where TProperty : class
    {
        GroupKeyProvider = new GroupKeyProvider<T, TProperty>(provider);

        return this;
    }
}
