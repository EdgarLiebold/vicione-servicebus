using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

public interface ISagaList<out T> :
    IAsyncElementList<ISagaInstance<T>>
    where T : class, ISaga
{
    IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    T? Contains(Guid sagaId);

    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default);
}
