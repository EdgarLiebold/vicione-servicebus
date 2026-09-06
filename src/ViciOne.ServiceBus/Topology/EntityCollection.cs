using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Stores a collection of entity values.</summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="THandle">The handle type.</typeparam>
public class EntityCollection<TEntity, THandle> :
    IEnumerable<TEntity>
    where TEntity : THandle
    where THandle : EntityHandle
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityComparer">The entity comparer.</param>
    public EntityCollection(IEqualityComparer<TEntity> entityComparer)
    {
        EntityIds = new Dictionary<long, TEntity>();
        Entities = new Dictionary<TEntity, TEntity>(entityComparer);
    }

    /// <summary>Gets the entities.</summary>
    protected IDictionary<TEntity, TEntity> Entities { get; }

    /// <summary>Gets the entity ids.</summary>
    protected IDictionary<long, TEntity> EntityIds { get; }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<TEntity> GetEnumerator()
    {
        return Entities.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Gets or add.</summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The or add.</returns>
    public virtual THandle GetOrAdd(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // An equivalent entity reuses its existing handle.
        if (Entities.TryGetValue(entity, out var existingEntity))
            return existingEntity;

        EntityIds.Add(entity.Id, entity);
        Entities.Add(entity, entity);

        return entity;
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="entityHandle">The entity handle.</param>
    /// <returns>The requested value.</returns>
    public virtual TEntity Get(THandle entityHandle)
    {
        if (entityHandle == null)
            throw new ArgumentNullException(nameof(entityHandle));

        if (!EntityIds.TryGetValue(entityHandle.Id, out var existingEntity))
            throw new ArgumentException($"The existing entity was not found: {TypeCache<TEntity>.ShortName}");

        if (!existingEntity.Equals(entityHandle))
            throw new ArgumentException($"The existing entity did not match the argument entity: {TypeCache<TEntity>.ShortName}");

        return existingEntity;
    }
}
