using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for async element list.
/// </summary>
/// <typeparam name="TElement">The t element type.</typeparam>
public interface IAsyncElementList<out TElement>
    where TElement : class, IAsyncListElement
{
    /// <summary>
    /// Gets the count value.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the save mode value.
    /// </summary>
    TestContextSaveMode SaveMode { get; }

    /// <summary>
    /// Gets the maximum saved elements value.
    /// </summary>
    int MaximumSavedElements { get; }

    /// <summary>
    /// Performs the snapshot operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IReadOnlyList<TElement> Snapshot();

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IEnumerable<TElement> Select(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);
}
