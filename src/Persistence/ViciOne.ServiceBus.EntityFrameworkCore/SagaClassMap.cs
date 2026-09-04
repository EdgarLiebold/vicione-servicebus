using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a saga class map implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public abstract class SagaClassMap<TSaga> :
    ISagaClassMap<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Gets the saga type value.
    /// </summary>
    public Type SagaType => typeof(TSaga);

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="model">The model value.</param>
    public virtual void Configure(ModelBuilder model)
    {
        EntityTypeBuilder<TSaga> entity = model.Entity<TSaga>();

        var key = entity.HasKey(p => p.CorrelationId);

        entity.Property(p => p.CorrelationId)
            .ValueGeneratedNever();

        Configure(entity, model);
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="model">The model value.</param>
    protected virtual void Configure(EntityTypeBuilder<TSaga> entity, ModelBuilder model)
    {
    }

    /// <summary>
    /// Override to configure the primary CorrelationId key, to add things like clustering
    /// </summary>
    /// <param name="keyBuilder"></param>
    /// <returns></returns>
    protected virtual KeyBuilder ConfigureCorrelationIdKey(KeyBuilder keyBuilder)
    {
        return keyBuilder;
    }
}
