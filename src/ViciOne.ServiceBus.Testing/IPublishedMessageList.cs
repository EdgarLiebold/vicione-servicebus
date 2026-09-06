using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by published message list.</summary>
public interface IPublishedMessageList :
    IAsyncElementList<IPublishedMessage>
{
    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<IPublishedMessage<T>> Select<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<IPublishedMessage<T>> Select<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IPublishedMessage> SelectAsync(Action<PublishedMessageFilter> apply, CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<IPublishedMessage<T>> SelectAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Selects any matching value.</summary>
    /// <param name="apply">The apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(Action<PublishedMessageFilter>? apply = default, CancellationToken cancellationToken = default);

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
    Task<bool> AnyAsync<T>(FilterDelegate<IPublishedMessage<T>> filter, CancellationToken cancellationToken = default)
        where T : class;
}
