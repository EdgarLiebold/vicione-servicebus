using System;
using System.Threading;
using System.Threading.Tasks;

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
    public TimestampVariable()
    {
        _timestamp = TimeProvider.System.GetUtcNow();
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
        cancellationToken.ThrowIfCancellationRequested();
        var timestampContext = context.GetOrAddPayload<TimestampContext>(() => new Context(_timestamp));

        return Task.FromResult(timestampContext.Timestamp);
    }

    /// <summary>Returns the timestamp captured by the variable.</summary>
    /// <param name="variable">The timestamp variable.</param>
    /// <returns>The captured timestamp.</returns>
    public static implicit operator DateTimeOffset(TimestampVariable variable)
    {
        return variable._timestamp;
    }


    interface TimestampContext
    {
        DateTimeOffset Timestamp { get; }
    }


    sealed class Context :
        TimestampContext
    {
        public Context(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }
}
