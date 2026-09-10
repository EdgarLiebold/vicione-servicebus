using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes retained message-send observations and queries for future sends.</summary>
public interface ISentMessageList :
    IAsyncElementList<ISentMessage>
{
    /// <summary>Creates a stable snapshot of sends for a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <returns>The retained matching sends in arrival order.</returns>
    IReadOnlyList<ISentMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class;

    /// <summary>Creates a stable snapshot of sends for a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <returns>The retained matching sends in arrival order.</returns>
    IReadOnlyList<ISentMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter)
        where TMessage : class;

    /// <summary>Asynchronously enumerates sends accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching sends until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asynchronously enumerates sends of a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching sends until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<ISentMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Asynchronously enumerates sends of a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching sends until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<ISentMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a send accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync(Action<SentMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default);

    /// <summary>Waits for a send of a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a send of a message contract that satisfies a predicate.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class;
}
