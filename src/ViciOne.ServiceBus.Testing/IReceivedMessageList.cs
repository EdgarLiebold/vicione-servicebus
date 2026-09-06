using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by received message list.</summary>
public interface IReceivedMessageList :
    IAsyncElementList<IReceivedMessage>
{
    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<IReceivedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<IReceivedMessage<T>> Select<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IReceivedMessage> SelectAsync(Action<ReceivedMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(Action<ReceivedMessageFilter>? apply = default, CancellationToken cancellationToken = default);

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects any matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync<T>(FilterDelegate<IReceivedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}


/// <summary>Defines the operations required by received message list.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReceivedMessageList<out T> :
    IAsyncElementList<IReceivedMessage<T>>
    where T : class
{
    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<IReceivedMessage<T>> Select(CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IReceivedMessage<T>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>Selects any matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
