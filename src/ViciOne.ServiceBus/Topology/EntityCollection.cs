using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Indexes topology entities by structural equality and by their builder-assigned identifiers.</summary>
/// <typeparam name="TEntity">The topology entity type.</typeparam>
/// <typeparam name="THandle">The entity-handle contract exposed to callers.</typeparam>
public class EntityCollection<TEntity, THandle> :
    IEnumerable<TEntity>
    where TEntity : THandle
    where THandle : EntityHandle
{
    /// <summary>Creates an empty collection using the specified structural equality comparer.</summary>
    /// <param name="entityComparer">The comparer that determines whether two entity definitions are equivalent.</param>
    public EntityCollection(IEqualityComparer<TEntity> entityComparer)
    {
        ArgumentNullException.ThrowIfNull(entityComparer);

        EntityIds = new Dictionary<long, TEntity>();
        Entities = new Dictionary<TEntity, TEntity>(entityComparer);
    }

    /// <summary>Gets the structural entity index used by specialized collections.</summary>
    protected IDictionary<TEntity, TEntity> Entities { get; }

    /// <summary>Gets the identifier index used by specialized collections.</summary>
    protected IDictionary<long, TEntity> EntityIds { get; }

    /// <summary>Enumerates the distinct entity definitions in insertion order.</summary>
    /// <returns>An enumerator over the stored entities.</returns>
    public IEnumerator<TEntity> GetEnumerator()
    {
        return Entities.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Returns the handle for an equivalent entity or adds the supplied entity.</summary>
    /// <param name="entity">The entity definition to resolve.</param>
    /// <returns>The existing equivalent handle, or <paramref name="entity" /> when added.</returns>
    public virtual THandle GetOrAdd(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // An equivalent entity reuses its existing handle.
        if (Entities.TryGetValue(entity, out var existingEntity))
            return existingEntity;

        if (EntityIds.ContainsKey(entity.Id))
            throw new ArgumentException($"The entity identifier {entity.Id} is already in use.", nameof(entity));

        EntityIds.Add(entity.Id, entity);
        Entities.Add(entity, entity);

        return entity;
    }

    /// <summary>Resolves a handle to the structurally matching entity definition.</summary>
    /// <param name="entityHandle">The handle to resolve.</param>
    /// <returns>The entity stored for the handle.</returns>
    public virtual TEntity Get(THandle entityHandle)
    {
        ArgumentNullException.ThrowIfNull(entityHandle);

        if (!EntityIds.TryGetValue(entityHandle.Id, out var existingEntity))
            throw new ArgumentException($"No {TypeCache<TEntity>.ShortName} entity has identifier {entityHandle.Id}.", nameof(entityHandle));

        if (!existingEntity.Equals(entityHandle))
            throw new ArgumentException($"The {TypeCache<TEntity>.ShortName} handle does not match the stored entity.", nameof(entityHandle));

        return existingEntity;
    }
}
