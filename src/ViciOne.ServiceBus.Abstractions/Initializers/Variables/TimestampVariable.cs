using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Variables;

/// <summary>
/// Used to set timestamp(s) in a message, which is the same regardless of how many times it is
/// used within the same initialize message context
/// </summary>
public class TimestampVariable :
    IInitializerVariable<DateTimeOffset>
{
    readonly DateTimeOffset _timestamp;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TimestampVariable()
    {
        _timestamp = TimeProvider.System.GetUtcNow();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timestamp">The timestamp value.</param>
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

    /// <summary>
    /// Converts a value to <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="variable">The variable value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator DateTimeOffset(TimestampVariable variable)
    {
        return variable._timestamp;
    }


    interface TimestampContext
    {
        DateTimeOffset Timestamp { get; }
    }


    class Context :
        TimestampContext
    {
        public Context(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }
}
