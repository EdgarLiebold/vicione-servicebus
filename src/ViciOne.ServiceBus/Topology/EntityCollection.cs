using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides an entity collection implementation.
/// </summary>
/// <typeparam name="TEntity">The t entity type.</typeparam>
/// <typeparam name="THandle">The t handle type.</typeparam>
public class EntityCollection<TEntity, THandle> :
    IEnumerable<TEntity>
    where TEntity : THandle
    where THandle : EntityHandle
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityComparer">The entity comparer value.</param>
    public EntityCollection(IEqualityComparer<TEntity> entityComparer)
    {
        EntityIds = new Dictionary<long, TEntity>();
        Entities = new Dictionary<TEntity, TEntity>(entityComparer);
    }

    /// <summary>
    /// Gets the entities value.
    /// </summary>
    protected IDictionary<TEntity, TEntity> Entities { get; }

    /// <summary>
    /// Gets the entity ids value.
    /// </summary>
    protected IDictionary<long, TEntity> EntityIds { get; }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<TEntity> GetEnumerator()
    {
        return Entities.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Gets or add.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual THandle GetOrAdd(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // if it's exactly the same exchange
        if (Entities.TryGetValue(entity, out var existingEntity))
            return existingEntity;

        EntityIds.Add(entity.Id, entity);
        Entities.Add(entity, entity);

        return entity;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="entityHandle">The entity handle value.</param>
    /// <returns>The result of the operation.</returns>
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
