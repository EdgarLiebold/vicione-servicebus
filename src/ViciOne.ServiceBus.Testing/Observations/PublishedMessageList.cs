using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Records message-publication attempts for stable snapshots and bounded asynchronous queries.</summary>
public sealed class PublishedMessageList :
    AsyncElementList<IPublishedMessage>,
    IPublishedMessageList
{
    /// <summary>Creates a publication list that uses the system clock.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Creates a publication list.</summary>
    /// <param name="timeout">The maximum time a query waits for another observation.</param>
    /// <param name="testCompleted">The token that ends pending queries.</param>
    /// <param name="timeProvider">The clock used for query timeouts and observation timestamps.</param>
    public PublishedMessageList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<IPublishedMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class
    {
        return Snapshot().OfType<IPublishedMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IReadOnlyList<IPublishedMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return Snapshot().Where(messageFilter.Any).OfType<IPublishedMessage<TMessage>>().ToArray();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IPublishedMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<IPublishedMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IPublishedMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return SelectAsync(message => messageFilter.Any(message), cancellationToken)
            .OfTypeAsync<IPublishedMessage<TMessage>>(cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(Action<PublishedMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default)
    {
        var messageFilter = new PublishedMessageFilter();
        configureFilter?.Invoke(messageFilter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add<TMessage>();

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var messageFilter = new PublishedMessageFilter();
        messageFilter.Includes.Add(filter);

        return AnyAsync(message => messageFilter.Any(message), cancellationToken);
    }

    /// <summary>Records a successful publication.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="context">The successfully published message context.</param>
    public void Add<TMessage>(PublishContext<TMessage> context)
        where TMessage : class
    {
        Add(new PublishedMessage<TMessage>(context, null, TimeProvider));
    }

    /// <summary>Records a failed publication.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="context">The publish context that faulted.</param>
    /// <param name="exception">The exception raised by the publish pipeline.</param>
    public void Add<TMessage>(PublishContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        Add(new PublishedMessage<TMessage>(context, exception, TimeProvider));
    }
}
