using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Stores exact saga wrapper references with unique registered correlation identifiers.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
/// <remarks>Correlation identifiers must remain unchanged while registered. Queries snapshot membership, not mutable state values.</remarks>
public class IndexedSagaDictionary<TSaga>
    where TSaga : class, ISaga
{
    readonly IndexedSagaProperty<TSaga, Guid> _indexById;
    readonly IStagedSagaIndex<TSaga>[] _indices;
    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1, 1);
    readonly object _lock = new object();
    readonly HashSet<SagaInstance<TSaga>> _pendingAdds = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<SagaInstance<TSaga>> _pendingRemovals = new(ReferenceEqualityComparer.Instance);

    /// <summary>Creates the canonical interface-bound identifier index and supported attributed property indices.</summary>
    /// <remarks>Honors inherited property and implemented interface metadata once per getter; unsupported members raise a property-specific InvalidOperationException.</remarks>
    public IndexedSagaDictionary()
    {
        _indexById = new IndexedSagaProperty<TSaga, Guid>(state => state.CorrelationId);
        _indices = BuildIndices();
    }

    /// <summary>Gets the registered wrapper for an identifier, or null when no wrapper is retained.</summary>
    /// <param name="sagaId">The registered correlation identifier.</param>
    /// <remarks>Rejects an identifier changed on the retained state; removal still supports reference-based cleanup.</remarks>
    public SagaInstance<TSaga>? this[Guid sagaId]
    {
        get
        {
            SagaInstance<TSaga>? instance;
            lock (_lock)
                instance = _indexById[sagaId];

            if (instance != null)
                ValidateRegisteredCorrelation(instance, sagaId);
            return instance;
        }
    }

    /// <summary>Gets the number of exact wrappers registered under unique correlation identifiers.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _indexById.RegistrationCount;
        }
    }

    /// <summary>Acquires the dictionary's exclusive operation lease.</summary>
    /// <param name="cancellationToken">The token that cancels waiting for that lease.</param>
    /// <returns>A task completing with one lease requiring a matching Release.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>Releases one previously acquired dictionary operation lease.</summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>Captures all required index keys before publishing a new exact wrapper reference.</summary>
    /// <param name="instance">The required wrapper whose state remains referenced without copying.</param>
    /// <remarks>Repeated registered references are idempotent; duplicate identifiers and pending or invalidated references are rejected.</remarks>
    public void Add(SagaInstance<TSaga> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_lock)
        {
            if (_indexById.Contains(instance))
                return;
            if (instance.IsRemoved || _pendingRemovals.Contains(instance))
                throw new InvalidOperationException("An invalidated or removing saga wrapper cannot be registered.");
            if (!_pendingAdds.Add(instance))
                throw new InvalidOperationException("A saga registration is already in progress for this wrapper.");
        }

        try
        {
            SagaIndexRegistration[] registrations = _indices.Select(index => index.Capture(instance)).ToArray();
            Guid correlationId = (Guid)registrations[0].Key!;
            ValidateAdmissionCorrelation(instance, correlationId);
            lock (_lock)
            {
                if (instance.IsRemoved || _pendingRemovals.Contains(instance))
                    throw new InvalidOperationException("An invalidated or removing saga wrapper cannot be registered.");
                if (_indexById[correlationId] != null)
                    throw new InvalidOperationException($"Saga {correlationId} is already registered in the in-memory repository.");

                int applied = 0;
                try
                {
                    for (; applied < registrations.Length; applied++)
                        registrations[applied].Apply();
                }
                catch (Exception failure)
                {
                    List<Exception>? cleanupFailures = null;
                    for (int index = applied - 1; index >= 0; index--)
                    {
                        try
                        {
                            registrations[index].Rollback();
                        }
                        catch (Exception cleanupFailure)
                        {
                            (cleanupFailures ??= new List<Exception>()).Add(cleanupFailure);
                        }
                    }
                    if (cleanupFailures != null)
                    {
                        cleanupFailures.Insert(0, failure);
                        throw new AggregateException(cleanupFailures);
                    }
                    throw;
                }
            }
        }
        finally
        {
            lock (_lock)
                _pendingAdds.Remove(instance);
        }
    }

    /// <summary>Removes a required registered reference by its captured keys, then invalidates that wrapper.</summary>
    /// <param name="instance">The required wrapper, or an unregistered reference to leave unchanged.</param>
    /// <remarks>Does not remove or invalidate another equal state or a replacement. Invalidation runs outside the dictionary lock.</remarks>
    public void Remove(SagaInstance<TSaga> instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_lock)
        {
            if (!_indexById.Contains(instance))
                return;

            _pendingRemovals.Add(instance);
            foreach (IStagedSagaIndex<TSaga> index in _indices)
                index.Remove(instance);
        }
        try
        {
            instance.Remove();
        }
        finally
        {
            lock (_lock)
                _pendingRemovals.Remove(instance);
        }
    }

    /// <summary>Evaluates a required query against actual states in a registered membership snapshot.</summary>
    /// <param name="query">The required query returning a non-null predicate.</param>
    /// <returns>The materialized matching wrapper references, excluding later additions.</returns>
    /// <remarks>Predicates run outside owner locks; retired snapshot references remain in the result. Mutable secondary key buckets cannot determine complete query results.</remarks>
    public IEnumerable<SagaInstance<TSaga>> Where(ISagaQuery<TSaga> query)
    {
        return MatchSnapshot(query).Select(entry => entry.Key).ToArray();
    }

    /// <summary>Transforms original membership outside owner locks and materializes the result values.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The required transformation applied to exact state references.</param>
    /// <returns>The materialized transformation values without copying saga states.</returns>
    public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
    {
        ArgumentNullException.ThrowIfNull(transformer);
        KeyValuePair<SagaInstance<TSaga>, Guid>[] membership = SnapshotMembership();
        ValidateCorrelations(membership);
        TResult[] values = membership.Select(entry => transformer(entry.Key.Instance)).ToArray();
        ValidateCorrelations(membership);
        return values;
    }

    KeyValuePair<SagaInstance<TSaga>, Guid>[] SnapshotMembership()
    {
        lock (_lock)
            return _indexById.SnapshotRegistrations();
    }

    /// <summary>Materializes the original registered identifiers of matching snapshot members.</summary>
    internal List<Guid> GetMatchingCorrelationIds(ISagaQuery<TSaga> query)
    {
        return MatchSnapshot(query).Select(entry => entry.Value).ToList();
    }

    KeyValuePair<SagaInstance<TSaga>, Guid>[] MatchSnapshot(ISagaQuery<TSaga> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Func<TSaga, bool> filter = query.GetFilter() ?? throw new InvalidOperationException("The saga query returned a null filter.");
        KeyValuePair<SagaInstance<TSaga>, Guid>[] membership = SnapshotMembership();
        ValidateCorrelations(membership);
        KeyValuePair<SagaInstance<TSaga>, Guid>[] matching = membership.Where(entry => filter(entry.Key.Instance)).ToArray();
        ValidateCorrelations(membership);
        return matching;
    }

    IStagedSagaIndex<TSaga>[] BuildIndices()
    {
        var indices = new List<IStagedSagaIndex<TSaga>> { _indexById };
        var getters = new HashSet<(Module Module, int Token, Type? DeclaringType)>();
        MethodInfo correlationGetter = GetImplementedGetter(typeof(ISaga).GetProperty(nameof(ISaga.CorrelationId))!.GetMethod!);
        getters.Add((correlationGetter.Module, correlationGetter.MetadataToken, correlationGetter.DeclaringType));
        IEnumerable<PropertyInfo> indexProperties = typeof(TSaga).GetProperties()
            .Concat(typeof(TSaga).GetInterfaces().SelectMany(contract => contract.GetProperties()))
            .Where(property => Attribute.IsDefined(property, typeof(IndexedAttribute), inherit: true));

        foreach (var property in indexProperties)
        {
            Type keyType = property.PropertyType;
            if (property.GetMethod == null || property.GetMethod.IsStatic || property.GetIndexParameters().Length != 0
                || keyType.IsByRef || keyType.IsByRefLike || keyType.IsPointer || keyType.IsFunctionPointer || keyType.ContainsGenericParameters)
                throw new InvalidOperationException($"Indexed saga property '{property.DeclaringType?.FullName}.{property.Name}' must be a readable instance non-indexer property with a supported key type.");

            MethodInfo getter = GetImplementedGetter(property.GetMethod);
            if (!getters.Add((getter.Module, getter.MetadataToken, getter.DeclaringType)))
                continue;

            var propertyType = typeof(IndexedSagaProperty<,>).MakeGenericType(typeof(TSaga), property.PropertyType);

            var index = Activator.CreateInstance(propertyType, property) as IStagedSagaIndex<TSaga>
                ?? throw new InvalidOperationException($"Could not create a saga index for '{property.Name}'.");
            indices.Add(index);
        }
        return indices.ToArray();
    }

    static MethodInfo GetImplementedGetter(MethodInfo getter)
    {
        if (!typeof(TSaga).IsInterface && getter.DeclaringType is { IsInterface: true } contract)
        {
            InterfaceMapping mapping = typeof(TSaga).GetInterfaceMap(contract);
            int methodIndex = Array.IndexOf(mapping.InterfaceMethods, getter);
            if (methodIndex >= 0)
                return mapping.TargetMethods[methodIndex];
        }
        return getter;
    }

    void ValidateCorrelations(KeyValuePair<SagaInstance<TSaga>, Guid>[] membership)
    {
        foreach (KeyValuePair<SagaInstance<TSaga>, Guid> entry in membership)
            ValidateRegisteredCorrelation(entry.Key, entry.Value);
    }

    void ValidateRegisteredCorrelation(SagaInstance<TSaga> instance, Guid registeredId)
    {
        Guid currentId = instance.Instance.CorrelationId;
        if (currentId == registeredId)
            return;

        lock (_lock)
        {
            if (!_indexById.Contains(instance))
                return;
        }
        throw new InvalidOperationException($"Saga {registeredId} changed its correlation identifier while registered.");
    }

    static void ValidateAdmissionCorrelation(SagaInstance<TSaga> instance, Guid capturedId)
    {
        if (instance.Instance.CorrelationId != capturedId)
            throw new InvalidOperationException($"Saga {capturedId} changed its correlation identifier during registration.");
    }
}
