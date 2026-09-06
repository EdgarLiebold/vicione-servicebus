using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>EF Core context containing the row-versioned future-state mapping.</summary>
public class OptimisticFutureSagaDbContext :
    FutureSagaDbContext
{
    /// <summary>Initializes the row-versioned future-saga DbContext.</summary>
    /// <param name="options">The options for this context type.</param>
    public OptimisticFutureSagaDbContext(DbContextOptions<OptimisticFutureSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the row-versioned future-state mapping.</summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(true); }
    }
}
