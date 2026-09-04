using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides an optimistic future saga db context implementation.
/// </summary>
public class OptimisticFutureSagaDbContext :
    FutureSagaDbContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public OptimisticFutureSagaDbContext(DbContextOptions<OptimisticFutureSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the configurations value.
    /// </summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(true); }
    }
}
