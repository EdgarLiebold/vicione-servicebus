using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes retained message-publication observations and queries for future publications.</summary>
public interface IPublishedMessageList :
    IAsyncElementList<IPublishedMessage>
{
    /// <summary>Creates a stable snapshot of publications for a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <returns>The retained matching publications in arrival order.</returns>
    IReadOnlyList<IPublishedMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class;

    /// <summary>Creates a stable snapshot of publications for a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <returns>The retained matching publications in arrival order.</returns>
    IReadOnlyList<IPublishedMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter)
        where TMessage : class;

    /// <summary>Asynchronously enumerates publications accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching publications until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asynchronously enumerates publications of a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching publications until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IPublishedMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Asynchronously enumerates publications of a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching publications until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IPublishedMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a publication accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync(Action<PublishedMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default);

    /// <summary>Waits for a publication of a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a publication of a message contract that satisfies a predicate.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class;
}
