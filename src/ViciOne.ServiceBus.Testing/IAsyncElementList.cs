namespace ViciOne.ServiceBus.Testing
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;


    public interface IAsyncElementList<out TElement>
        where TElement : class, IAsyncListElement
    {
        int Count { get; }

        TestContextSaveMode SaveMode { get; }

        int MaximumSavedElements { get; }

        IReadOnlyList<TElement> Snapshot();

        IEnumerable<TElement> Select(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

        IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);

        Task<bool> Any(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default);
    }
}
