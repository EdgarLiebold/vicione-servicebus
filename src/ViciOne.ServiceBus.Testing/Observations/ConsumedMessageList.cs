using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Records message-consumption attempts for stable snapshots and bounded asynchronous queries.</summary>
public sealed class ConsumedMessageList :
    AsyncElementList<IConsumedMessage>,
    IConsumedMessageList
{
    /// <summary>Creates a consumption list that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    public ConsumedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Creates a consumption list.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    /// <param name="timeProvider">The clock used for query timeouts and observation timestamps.</param>
    public ConsumedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<IConsumedMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class
    {
        return Snapshot().OfType<IConsumedMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<IConsumedMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);
        var messageFilter = new ConsumedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Snapshot().Where(messageFilter.Any).OfType<IConsumedMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IConsumedMessage> SelectAsync(Action<ConsumedMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default)
    {
        var messageFilter = new ConsumedMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new ConsumedMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<IConsumedMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new ConsumedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<IConsumedMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(Action<ConsumedMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default)
    {
        var messageFilter = new ConsumedMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new ConsumedMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new ConsumedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Records a successful consumption.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="context">The successfully consumed message context.</param>
    public void Add<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        Add(new ConsumedMessage<TMessage>(context, null, TimeProvider));
    }

    /// <summary>Records a failed consumption.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="context">The consume context that faulted.</param>
    /// <param name="exception">The exception raised by the consume pipeline.</param>
    public void Add<TMessage>(ConsumeContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        Add(new ConsumedMessage<TMessage>(context, exception, TimeProvider));
    }
}


/// <summary>Records consumption attempts for one message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class ConsumedMessageList<TMessage> :
    AsyncElementList<IConsumedMessage<TMessage>>,
    IConsumedMessageList<TMessage>
    where TMessage : class
{
    /// <summary>Creates a consumption list that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    public ConsumedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Creates a consumption list.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    /// <param name="timeProvider">The clock used for query timeouts and observation timestamps.</param>
    public ConsumedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <summary>Records a successful consumption.</summary>
    /// <param name="context">The successfully consumed message context.</param>
    public void Add(ConsumeContext<TMessage> context)
    {
        Add(new ConsumedMessage<TMessage>(context, null, TimeProvider));
    }

    /// <summary>Records a failed consumption.</summary>
    /// <param name="context">The consume context that faulted.</param>
    /// <param name="exception">The exception raised by the consume pipeline.</param>
    public void Add(ConsumeContext<TMessage> context, Exception exception)
    {
        Add(new ConsumedMessage<TMessage>(context, exception, TimeProvider));
    }
}
