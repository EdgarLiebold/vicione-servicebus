using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for saga list.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ISagaList<out T> :
    IAsyncElementList<ISagaInstance<T>>
    where T : class, ISaga
{
    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the contains operation.
    /// </summary>
    /// <param name="sagaId">The saga id value.</param>
    /// <returns>The result of the operation.</returns>
    T? Contains(Guid sagaId);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);
}
