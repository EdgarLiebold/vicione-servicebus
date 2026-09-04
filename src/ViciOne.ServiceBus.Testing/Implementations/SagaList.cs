using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a saga list implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SagaList<T> :
    AsyncElementList<ISagaInstance<T>>,
    ISagaList<T>
    where T : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public SagaList(TimeSpan timeout, CancellationToken testCompleted = default)
        : base(timeout, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public SagaList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeout, testCompleted, timeProvider)
    {
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ISagaInstance<T>> Select(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return Select(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>
    /// Performs the contains operation.
    /// </summary>
    /// <param name="sagaId">The saga id value.</param>
    /// <returns>The result of the operation.</returns>
    public T? Contains(Guid sagaId)
    {
        return Select(x => x.Saga.CorrelationId == sagaId).Select(x => x.Saga).FirstOrDefault();
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IAsyncEnumerable<ISagaInstance<T>> SelectAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<bool> AnyAsync(FilterDelegate<T> filter, CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Add(SagaConsumeContext<T> context)
    {
        Add(new SagaInstance<T>(context.Saga));
    }
}
