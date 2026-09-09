using System;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Applies an EF Core mapping for one saga entity type.</summary>
public interface ISagaClassMap
{
    /// <summary>Gets the saga entity type configured by this map.</summary>
    Type SagaType { get; }
    /// <summary>Adds the saga mapping to an EF Core model.</summary>
    /// <param name="model">The model builder to configure.</param>
    void Configure(ModelBuilder model);
}

/// <summary>Identifies an EF Core saga map for a specific saga type.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ISagaClassMap<TSaga> :
    ISagaClassMap
    where TSaga : class, ISaga
{
}
