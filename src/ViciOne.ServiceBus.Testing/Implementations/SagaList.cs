using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class SagaList<T> :
    AsyncElementList<ISagaInstance<T>>,
    ISagaList<T>
    where T : class, ISaga
{
    public SagaList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    public SagaList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    public IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return Select(x => filter(x.Saga), cancellationToken);
    }

    public T? Contains(Guid sagaId)
    {
        return Select(x => x.Saga.CorrelationId == sagaId).Select(x => x.Saga).FirstOrDefault();
    }

    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => filter(x.Saga), cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    public Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => filter(x.Saga), cancellationToken);
    }

    public void Add(SagaConsumeContext<T> context)
    {
        Add(new SagaInstance<T>(context.Saga));
    }
}
