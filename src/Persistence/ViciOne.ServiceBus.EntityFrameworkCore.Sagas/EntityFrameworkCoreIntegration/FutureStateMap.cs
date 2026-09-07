using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Maps persisted future state, including JSON-owned collections and optional row-version concurrency.</summary>
public class FutureStateMap :
    SagaClassMap<FutureState>
{
    readonly bool _optimistic;

    /// <summary>Initializes a future-state mapping with the selected concurrency model.</summary>
    /// <param name="optimistic"><see langword="true"/> to map <c>RowVersion</c>; otherwise it is ignored.</param>
    public FutureStateMap(bool optimistic)
    {
        _optimistic = optimistic;
    }

    /// <summary>Configures future-state scalar, JSON, and concurrency properties.</summary>
    /// <param name="entity">The future-state entity builder.</param>
    /// <param name="model">The containing EF Core model builder.</param>
    protected override void Configure(EntityTypeBuilder<FutureState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState);

        if (_optimistic)
        {
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        }
        else
            entity.Ignore(x => x.RowVersion);

        entity.Property(x => x.Created);
        entity.Property(x => x.Completed);
        entity.Property(x => x.Faulted);

        entity.Property(x => x.Location);

        entity.Property(x => x.Command)
            .HasJsonConversion();
        entity.Property(x => x.Pending)
            .HasJsonConversion();
        entity.Property(x => x.Subscriptions)
            .HasJsonConversion();
        entity.Property(x => x.Variables)
            .HasJsonConversion();
        entity.Property(x => x.Results)
            .HasJsonConversion();
        entity.Property(x => x.Faults)
            .HasJsonConversion();
    }
}
