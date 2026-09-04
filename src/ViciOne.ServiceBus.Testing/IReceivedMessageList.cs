using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for received message list.
/// </summary>
public interface IReceivedMessageList :
    IAsyncElementList<IReceivedMessage>
{
    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="apply">The apply value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default);

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
    Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}


/// <summary>
/// Defines the contract for received message list.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IReceivedMessageList<out T> :
    IAsyncElementList<IReceivedMessage<T>>
    where T : class
{
    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
