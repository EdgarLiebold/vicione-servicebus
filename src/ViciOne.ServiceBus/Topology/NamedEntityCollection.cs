using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a named entity collection implementation.
/// </summary>
/// <typeparam name="TEntity">The t entity type.</typeparam>
/// <typeparam name="THandle">The t handle type.</typeparam>
public class NamedEntityCollection<TEntity, THandle> :
    EntityCollection<TEntity, THandle>
    where TEntity : THandle
    where THandle : EntityHandle
{
    readonly IDictionary<TEntity, TEntity> _entityNames;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityComparer">The entity comparer value.</param>
    /// <param name="nameComparer">The name comparer value.</param>
    public NamedEntityCollection(IEqualityComparer<TEntity> entityComparer, IEqualityComparer<TEntity> nameComparer)
        : base(entityComparer)
    {
        _entityNames = new Dictionary<TEntity, TEntity>(nameComparer);
    }

    /// <summary>
    /// Gets or add.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the operation.</returns>
    public override THandle GetOrAdd(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_entityNames.TryGetValue(entity, out var existingEntity))
        {
            // if it's exactly the same exchange
            if (Entities.TryGetValue(entity, out existingEntity))
                return existingEntity;

            throw new ArgumentException($"The {TypeCache<TEntity>.ShortName} entity settings did not match the existing entity");
        }

        EntityIds.Add(entity.Id, entity);
        Entities.Add(entity, entity);
        _entityNames.Add(entity, entity);

        return entity;
    }
}
