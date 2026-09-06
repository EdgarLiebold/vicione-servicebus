using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by sent message list.</summary>
public interface ISentMessageList :
    IAsyncElementList<ISentMessage>
{
    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<ISentMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<ISentMessage<T>> Select<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<ISentMessage> SelectAsync(Action<SentMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<ISentMessage<T>> SelectAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(Action<SentMessageFilter>? apply = default, CancellationToken cancellationToken = default);

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
    Task<bool> AnyAsync<T>(FilterDelegate<ISentMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}
