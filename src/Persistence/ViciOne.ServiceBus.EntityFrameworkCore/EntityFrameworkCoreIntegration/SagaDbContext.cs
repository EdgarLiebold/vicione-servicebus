using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a saga db context implementation.
/// </summary>
public abstract class SagaDbContext :
    DbContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    protected SagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the configurations value.
    /// </summary>
    protected abstract IEnumerable<ISagaClassMap> Configurations { get; }

    /// <summary>
    /// Performs the on model creating operation.
    /// </summary>
    /// <param name="modelBuilder">The model builder value.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var configuration in Configurations)
            configuration.Configure(modelBuilder);
    }
}
