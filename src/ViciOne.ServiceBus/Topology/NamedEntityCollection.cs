using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Indexes topology entities by name in addition to structural equality and identifier.</summary>
/// <typeparam name="TEntity">The named topology entity type.</typeparam>
/// <typeparam name="THandle">The entity-handle contract exposed to callers.</typeparam>
public class NamedEntityCollection<TEntity, THandle> :
    EntityCollection<TEntity, THandle>
    where TEntity : THandle
    where THandle : EntityHandle
{
    readonly IDictionary<TEntity, TEntity> _entityNames;

    /// <summary>Creates an empty collection with separate structural and name equality rules.</summary>
    /// <param name="entityComparer">The comparer for complete entity definitions.</param>
    /// <param name="nameComparer">The comparer for broker entity names.</param>
    public NamedEntityCollection(IEqualityComparer<TEntity> entityComparer, IEqualityComparer<TEntity> nameComparer)
        : base(entityComparer)
    {
        ArgumentNullException.ThrowIfNull(nameComparer);

        _entityNames = new Dictionary<TEntity, TEntity>(nameComparer);
    }

    /// <summary>Returns an equivalent named entity or adds a new name and definition.</summary>
    /// <param name="entity">The named entity definition to resolve.</param>
    /// <returns>The existing equivalent handle, or <paramref name="entity" /> when added.</returns>
    public override THandle GetOrAdd(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_entityNames.TryGetValue(entity, out var existingEntity))
        {
            // Matching names may reuse a handle only when all entity settings are equivalent.
            if (Entities.TryGetValue(entity, out existingEntity))
                return existingEntity;

            throw new ArgumentException(
                $"The {TypeCache<TEntity>.ShortName} settings differ from the existing entity with the same name.",
                nameof(entity));
        }

        if (EntityIds.ContainsKey(entity.Id))
            throw new ArgumentException($"The entity identifier {entity.Id} is already in use.", nameof(entity));

        EntityIds.Add(entity.Id, entity);
        Entities.Add(entity, entity);
        _entityNames.Add(entity, entity);

        return entity;
    }
}
