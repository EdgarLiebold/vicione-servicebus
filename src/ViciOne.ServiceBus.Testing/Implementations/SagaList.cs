using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Stores a list of saga values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SagaList<T> :
    AsyncElementList<ISagaInstance<T>>,
    ISagaList<T>
    where T : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public SagaList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public SagaList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return Select(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>Determines whether the current collection contains the supplied value.</summary>
    /// <param name="sagaId">The saga id.</param>
    /// <returns>The t produced by the operation.</returns>
    public T? Contains(Guid sagaId)
    {
        return Select(x => x.Saga.CorrelationId == sagaId).Select(x => x.Saga).FirstOrDefault();
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <summary>Selects the matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any outcome.</returns>
    public Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Add(SagaConsumeContext<T> context)
    {
        Add(new SagaInstance<T>(context.Saga));
    }
}
