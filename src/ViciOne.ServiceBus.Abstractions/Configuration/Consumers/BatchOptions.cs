using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines batch size, timing, grouping, delivery concurrency, and endpoint capacity settings.
/// </summary>
public sealed class BatchOptions :
    IOptions,
    IConfigureReceiveEndpoint,
    ISpecification
{
    /// <summary>Represents a callback that configures receive-endpoint capacity for effective batch limits.</summary>
    /// <param name="name">The endpoint definition name, when one is available.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    public delegate void ConfigurationCallback(string? name, IReceiveEndpointConfigurator configurator);
    ConfigurationCallback _configurationCallback;

    /// <summary>Creates options with one concurrent batch, ten messages, and a one-second first-message timeout.</summary>
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

    /// <summary>The maximum number of completed batches delivered concurrently.</summary>
    public int ConcurrencyLimit { get; set; }

    /// <summary>The maximum time to wait before delivering a partial batch.</summary>
    public TimeSpan TimeLimit { get; set; }

    /// <summary>The message arrival from which <see cref="TimeLimit" /> is measured.</summary>
    public BatchTimeLimitStart TimeLimitStart { get; set; }

    internal object? GroupKeyProvider { get; private set; }

    /// <summary>Applies endpoint capacity settings derived from these batch limits.</summary>
    /// <param name="name">The endpoint definition name, when one is available.</param>
    /// <param name="configurator">The receive endpoint to configure.</param>
    public void Configure(string? name, IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

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
        if (ConcurrencyLimit > 0 && MessageLimit > 0 && (long)ConcurrencyLimit * MessageLimit > int.MaxValue)
            yield return this.Failure("Batch", "ConcurrencyLimit", "ConcurrencyLimit multiplied by MessageLimit must fit in Int32");
        if (!Enum.IsDefined(TimeLimitStart))
            yield return this.Failure("Batch", "TimeLimitStart", "Must be a defined BatchTimeLimitStart value");
    }

    /// <summary>Replaces the endpoint-capacity configuration callback.</summary>
    /// <param name="callback">The callback invoked when the receive endpoint is configured.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetConfigurationCallback(ConfigurationCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        _configurationCallback = callback;
        return this;
    }

    void DefaultConfigurationCallback(string? name, IReceiveEndpointConfigurator configurator)
    {
        var messageCapacity = checked(ConcurrencyLimit * MessageLimit);

        configurator.PrefetchCount = Math.Max(messageCapacity, configurator.PrefetchCount);

        if (configurator.ConcurrentMessageLimit < messageCapacity)
            configurator.ConcurrentMessageLimit = messageCapacity;
    }

    /// <summary>Sets the maximum number of messages in a single batch.</summary>
    /// <param name="limit">The maximum message count.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetMessageLimit(int limit)
    {
        MessageLimit = limit;
        return this;
    }

    /// <summary>Sets the maximum number of completed batches delivered concurrently.</summary>
    /// <param name="limit">The maximum concurrent batch count.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetConcurrencyLimit(int limit)
    {
        ConcurrencyLimit = limit;
        return this;
    }

    /// <summary>Sets the maximum time to wait before delivering a partial batch.</summary>
    /// <param name="limit">The maximum collection interval.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetTimeLimit(TimeSpan limit)
    {
        TimeLimit = limit;
        return this;
    }

    /// <summary>Sets the starting point for the <see cref="TimeLimit" />.</summary>
    /// <param name="timeLimitStart">The message arrival that starts or restarts the interval.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetTimeLimitStart(BatchTimeLimitStart timeLimitStart)
    {
        TimeLimitStart = timeLimitStart;
        return this;
    }

    /// <summary>Sets the maximum collection interval from optional duration components.</summary>
    /// <param name="milliseconds">The millisecond component.</param>
    /// <param name="seconds">The second component.</param>
    /// <param name="minutes">The minute component.</param>
    /// <param name="hours">The hour component.</param>
    /// <param name="days">The day component.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions SetTimeLimit(int? milliseconds = default, int? seconds = default, int? minutes = default, int? hours = default,
        int? days = default)
    {
        var timeSpan = new TimeSpan(days ?? 0, hours ?? 0, minutes ?? 0, seconds ?? 0, milliseconds ?? 0);
        if (timeSpan <= TimeSpan.Zero)
            throw new ArgumentException("The timeout must be > 0");

        TimeLimit = timeSpan;
        return this;
    }

    /// <summary>Groups messages by an optional value-type key.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <typeparam name="TKey">The value-type grouping key.</typeparam>
    /// <param name="provider">The selector that returns a grouping key or <see langword="null" /> for the ungrouped stream.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions GroupBy<TMessage, TKey>(Func<ConsumeContext<TMessage>, TKey?> provider)
        where TMessage : class
        where TKey : struct
    {
        ArgumentNullException.ThrowIfNull(provider);

        GroupKeyProvider = new ValueTypeGroupKeyProvider<TMessage, TKey>(provider);

        return this;
    }

    /// <summary>Groups messages by an optional reference-type key.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <typeparam name="TKey">The reference-type grouping key.</typeparam>
    /// <param name="provider">The selector that returns a grouping key or <see langword="null" /> for the ungrouped stream.</param>
    /// <returns>This options instance.</returns>
    public BatchOptions GroupBy<TMessage, TKey>(Func<ConsumeContext<TMessage>, TKey> provider)
        where TMessage : class
        where TKey : class
    {
        ArgumentNullException.ThrowIfNull(provider);

        GroupKeyProvider = new GroupKeyProvider<TMessage, TKey>(provider);

        return this;
    }
}
