using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for published message list.
/// </summary>
public interface IPublishedMessageList :
    IAsyncElementList<IPublishedMessage>
{
    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IPublishedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IPublishedMessage<T>> Select<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(Action<PublishedMessageFilter>? apply = default, CancellationToken cancellationToken = default);

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
    Task<bool> AnyAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}
