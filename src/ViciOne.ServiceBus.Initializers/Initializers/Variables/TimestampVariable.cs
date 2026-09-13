using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOne.ServiceBus.Initializers.Variables;

/// <summary>
/// Supplies one timestamp to every matching property within an initialization context.
/// A variable can therefore initialize related timestamp properties with the same instant.
/// </summary>
public sealed class TimestampVariable :
    IInitializerVariable<DateTimeOffset>
{
    readonly DateTimeOffset _timestamp;

    /// <summary>Creates a variable that captures the current UTC time.</summary>
    public TimestampVariable() :
        this(TimeProvider.System)
    {
    }

    /// <summary>Creates a variable that captures the current UTC time from a time source.</summary>
    /// <param name="timeProvider">The source that supplies the current UTC time.</param>
    public TimestampVariable(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timestamp = timeProvider.GetUtcNow();
    }

    /// <summary>Creates a variable that supplies the specified timestamp.</summary>
    /// <param name="timestamp">The timestamp supplied during initialization.</param>
    public TimestampVariable(DateTimeOffset timestamp)
    {
        _timestamp = timestamp;
    }

    Task<DateTimeOffset> IInitializerVariable<DateTimeOffset>.GetValueAsync<TMessage>(InitializeContext<TMessage> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var timestampContext = context.GetOrAddPayload<TimestampContext>(() => new TimestampContext(_timestamp));

        return Task.FromResult(timestampContext.Timestamp);
    }

    /// <summary>Returns the timestamp captured by the variable.</summary>
    /// <param name="variable">The timestamp variable.</param>
    /// <returns>The captured timestamp.</returns>
    public static implicit operator DateTimeOffset(TimestampVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return variable._timestamp;
    }

    sealed class TimestampContext(DateTimeOffset timestamp)
    {
        public DateTimeOffset Timestamp { get; } = timestamp;
    }
}
