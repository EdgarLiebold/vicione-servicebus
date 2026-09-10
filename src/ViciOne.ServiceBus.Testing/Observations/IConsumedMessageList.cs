using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes retained message-consumption observations and queries for future consumptions.</summary>
public interface IConsumedMessageList :
    IAsyncElementList<IConsumedMessage>
{
    /// <summary>Creates a stable snapshot of consumptions for a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <returns>The retained matching consumptions in arrival order.</returns>
    IReadOnlyList<IConsumedMessage<TMessage>> Snapshot<TMessage>()
        where TMessage : class;

    /// <summary>Creates a stable snapshot of consumptions for a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <returns>The retained matching consumptions in arrival order.</returns>
    IReadOnlyList<IConsumedMessage<TMessage>> Snapshot<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter)
        where TMessage : class;

    /// <summary>Asynchronously enumerates consumptions accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching consumptions until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IConsumedMessage> SelectAsync(Action<ConsumedMessageFilter>? configureFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>Asynchronously enumerates consumptions of a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching consumptions until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Asynchronously enumerates consumptions of a message contract that satisfy a predicate.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching consumptions until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a consumption accepted by an include/exclude filter.</summary>
    /// <param name="configureFilter">An optional callback that configures the include and exclude predicates.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync(Action<ConsumedMessageFilter>? configureFilter = null, CancellationToken cancellationToken = default);

    /// <summary>Waits for a consumption of a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Waits for a consumption of a message contract that satisfies a predicate.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="filter">The predicate applied to matching message contracts.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter, CancellationToken cancellationToken = default)
        where TMessage : class;
}

/// <summary>Exposes observations for a single consumed message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IConsumedMessageList<out TMessage> :
    IAsyncElementList<IConsumedMessage<TMessage>>
    where TMessage : class
{
    /// <summary>Asynchronously enumerates consumptions of the configured message contract.</summary>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Observed consumptions until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<IConsumedMessage<TMessage>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>Waits for a consumption of the configured message contract.</summary>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a message is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
