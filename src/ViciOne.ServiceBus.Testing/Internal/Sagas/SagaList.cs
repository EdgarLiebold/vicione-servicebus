using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records saga instances for synchronous snapshots and asynchronous observation.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class SagaList<TSaga> :
    AsyncElementList<ISagaInstance<TSaga>>,
    ISagaList<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a saga observation list that uses the system time provider.</summary>
    /// <param name="timeout">The maximum wait for a matching observation.</param>
    /// <param name="cancellationToken">The token that cancels pending observation.</param>
    public SagaList(TimeSpan timeout, CancellationToken cancellationToken = default)
        : base(timeout, cancellationToken)
    {
    }

    /// <summary>Creates a saga observation list.</summary>
    /// <param name="timeout">The maximum wait for a matching observation.</param>
    /// <param name="cancellationToken">The token that cancels pending observation.</param>
    /// <param name="timeProvider">The time provider used by observation timeouts.</param>
    public SagaList(TimeSpan timeout, CancellationToken cancellationToken, TimeProvider timeProvider)
        : base(timeout, cancellationToken, timeProvider)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<ISagaInstance<TSaga>> Snapshot(FilterDelegate<TSaga> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return Snapshot().Where(instance => filter(instance.Saga)).ToArray();
    }

    /// <inheritdoc />
    public TSaga? FindById(Guid sagaId)
    {
        return Snapshot().Where(x => x.Saga.CorrelationId == sagaId).Select(x => x.Saga).LastOrDefault();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(CancellationToken cancellationToken = default)
    {
        return SelectAsync(x => true, cancellationToken);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ISagaInstance<TSaga>> SelectAsync(FilterDelegate<TSaga> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SelectAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return AnyAsync(x => true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> AnyAsync(FilterDelegate<TSaga> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return AnyAsync(x => filter(x.Saga), cancellationToken);
    }

    /// <summary>Records the saga state from a consume context.</summary>
    /// <param name="context">The saga consume context.</param>
    public void Add(SagaConsumeContext<TSaga> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Add(new SagaInstance<TSaga>(context.Saga));
    }
}
