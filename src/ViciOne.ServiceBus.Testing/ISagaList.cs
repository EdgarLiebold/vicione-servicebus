using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by saga list.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ISagaList<out T> :
    IAsyncElementList<ISagaInstance<T>>
    where T : class, ISaga
{
    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    /// <summary>Determines whether the current collection contains the supplied value.</summary>
    /// <param name="sagaId">The saga id.</param>
    /// <returns>The t produced by the operation.</returns>
    T? Contains(Guid sagaId);

    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default);

    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    /// <summary>Selects any matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>Selects any matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);
}
