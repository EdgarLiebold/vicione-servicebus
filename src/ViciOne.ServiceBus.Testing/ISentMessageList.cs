using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for sent message list.
/// </summary>
public interface ISentMessageList :
    IAsyncElementList<ISentMessage>
{
    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<ISentMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<ISentMessage<T>> Select<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(Action<SentMessageFilter>? apply = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}
