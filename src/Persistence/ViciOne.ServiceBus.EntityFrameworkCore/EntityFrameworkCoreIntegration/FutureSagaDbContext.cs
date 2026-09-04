using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a future saga db context implementation.
/// </summary>
public class FutureSagaDbContext :
    SagaDbContext
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public FutureSagaDbContext(DbContextOptions<FutureSagaDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    protected FutureSagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the configurations value.
    /// </summary>
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new FutureStateMap(false); }
    }
}
