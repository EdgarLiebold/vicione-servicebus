using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Applies a declared set of saga mappings when EF Core builds the model.</summary>
public abstract class SagaDbContext :
    DbContext
{
    /// <summary>Initializes the saga DbContext with provider-specific EF Core options.</summary>
    /// <param name="options">The options configured by the derived DbContext type.</param>
    protected SagaDbContext(DbContextOptions options)
        : base(options ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    /// <summary>Gets the saga entity mappings included in this context.</summary>
    protected abstract IEnumerable<ISagaClassMap> Configurations { get; }

    /// <summary>Applies each configured saga mapping to the EF Core model.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var configuration in Configurations)
            configuration.Configure(modelBuilder);
    }
}
