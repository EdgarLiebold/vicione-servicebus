using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by async element list.</summary>
/// <typeparam name="TElement">The element type.</typeparam>
public interface IAsyncElementList<out TElement>
    where TElement : class, IAsyncListElement
{
    /// <summary>Gets the count.</summary>
    int Count { get; }

    /// <summary>Gets the save mode.</summary>
    TestContextSaveMode SaveMode { get; }

    /// <summary>Gets the maximum saved elements.</summary>
    int MaximumSavedElements { get; }

    /// <summary>Captures the current state.</summary>
    /// <returns>The read only list produced by the operation.</returns>
    IReadOnlyList<TElement> Snapshot();

    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<TElement> Select(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

    /// <summary>Selects any matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);
}
