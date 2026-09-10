using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Exposes retained observations and bounded asynchronous queries for future matching elements.</summary>
/// <typeparam name="TElement">The observed element type.</typeparam>
public interface IAsyncElementList<out TElement>
    where TElement : class, IAsyncListElement
{
    /// <summary>Gets the number of observations currently retained.</summary>
    int Count { get; }

    /// <summary>Gets the policy that controls historical observation retention.</summary>
    TestContextSaveMode SaveMode { get; }

    /// <summary>Gets the maximum number of observations retained in bounded mode.</summary>
    int MaximumSavedElements { get; }

    /// <summary>Creates a stable copy of the observations retained at the time of the call.</summary>
    /// <returns>The retained observations in arrival order.</returns>
    IReadOnlyList<TElement> Snapshot();

    /// <summary>Asynchronously enumerates retained and subsequently observed elements that satisfy a predicate.</summary>
    /// <param name="filter">The predicate applied to each observation.</param>
    /// <param name="cancellationToken">The token used to stop enumeration.</param>
    /// <returns>Matching observations until timeout, test completion, or cancellation.</returns>
    IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

    /// <summary>Waits for a retained or subsequently observed element that satisfies a predicate.</summary>
    /// <param name="filter">The predicate applied to each observation.</param>
    /// <param name="cancellationToken">The token used to cancel the wait.</param>
    /// <returns><see langword="true"/> when a match is observed; otherwise, <see langword="false"/> after timeout or test completion.</returns>
    Task<bool> AnyAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);
}
