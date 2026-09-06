using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>EF Core context containing the pessimistic-concurrency future-state mapping.</summary>
public class FutureSagaDbContext :
    SagaDbContext
{
    /// <summary>Initializes the default future-saga DbContext.</summary>
    /// <param name="options">The options for this context type.</param>
    public FutureSagaDbContext(DbContextOptions<FutureSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>Initializes a derived future-saga DbContext.</summary>
    /// <param name="options">Options supplied by a derived context type.</param>
    protected FutureSagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>Gets the non-row-version future-state mapping.</summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(false); }
    }
}
