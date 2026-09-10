using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Records message-send attempts for stable snapshots and bounded asynchronous queries.</summary>
public sealed class SentMessageList :
    AsyncElementList<ISentMessage>,
    ISentMessageList
{
    /// <summary>Creates a send list that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Creates a send list.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    /// <param name="timeProvider">The clock used for query timeouts and observation timestamps.</param>
    public SentMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<ISentMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class
    {
        return Snapshot().OfType<ISentMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<ISentMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return Snapshot().Where(messageFilter.Any).OfType<ISentMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ISentMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<ISentMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ISentMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<ISentMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(Action<SentMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default)
    {
        var messageFilter = new SentMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new SentMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Records a successful send.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="context">The successfully sent message context.</param>
    public void Add<TMessage>(SendContext<TMessage> context)
        where TMessage : class
    {
        Add(new SentMessage<TMessage>(context, null, TimeProvider));
    }

    /// <summary>Records a failed send.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="context">The send context that faulted.</param>
    /// <param name="exception">The exception raised by the send pipeline.</param>
    public void Add<TMessage>(SendContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        Add(new SentMessage<TMessage>(context, exception, TimeProvider));
    }
}
