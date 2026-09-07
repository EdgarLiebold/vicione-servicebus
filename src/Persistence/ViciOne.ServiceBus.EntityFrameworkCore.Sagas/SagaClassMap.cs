using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures the correlation identifier as the non-generated key for a saga entity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public abstract class SagaClassMap<TSaga> :
    ISagaClassMap<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Gets the saga entity type configured by this map.</summary>
    public Type SagaType => typeof(TSaga);

    /// <summary>Adds the saga entity, configures its correlation key, and applies derived mapping rules.</summary>
    /// <param name="model">The EF Core model builder to configure.</param>
    public virtual void Configure(ModelBuilder model)
    {
        ArgumentNullException.ThrowIfNull(model);

        EntityTypeBuilder<TSaga> entity = model.Entity<TSaga>();

        ConfigureCorrelationIdKey(entity.HasKey(p => p.CorrelationId));

        entity.Property(p => p.CorrelationId)
            .ValueGeneratedNever();

        Configure(entity, model);
    }

    /// <summary>Configures saga-specific properties and relationships.</summary>
    /// <param name="entity">The saga entity builder.</param>
    /// <param name="model">The containing EF Core model builder.</param>
    protected virtual void Configure(EntityTypeBuilder<TSaga> entity, ModelBuilder model)
    {
    }

    /// <summary>Customizes the primary correlation key, for example with provider-specific clustering.</summary>
    /// <param name="keyBuilder">The correlation-key builder.</param>
    protected virtual void ConfigureCorrelationIdKey(KeyBuilder keyBuilder)
    {
    }
}
