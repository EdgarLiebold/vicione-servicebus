using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Stores a collection of named entity values.</summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="THandle">The handle type.</typeparam>
public class NamedEntityCollection<TEntity, THandle> :
    EntityCollection<TEntity, THandle>
    where TEntity : THandle
    where THandle : EntityHandle
{
    readonly IDictionary<TEntity, TEntity> _entityNames;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityComparer">The entity comparer.</param>
    /// <param name="nameComparer">The name comparer.</param>
    public NamedEntityCollection(IEqualityComparer<TEntity> entityComparer, IEqualityComparer<TEntity> nameComparer)
        : base(entityComparer)
    {
        _entityNames = new Dictionary<TEntity, TEntity>(nameComparer);
    }

    /// <summary>Gets or add.</summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The or add.</returns>
    public override THandle GetOrAdd(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_entityNames.TryGetValue(entity, out var existingEntity))
        {
            // Matching names may reuse a handle only when all entity settings are equivalent.
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
