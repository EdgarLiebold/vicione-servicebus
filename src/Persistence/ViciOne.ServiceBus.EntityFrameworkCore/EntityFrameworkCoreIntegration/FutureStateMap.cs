using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a future state map implementation.
/// </summary>
public class FutureStateMap :
    SagaClassMap<FutureState>
{
    readonly bool _optimistic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="optimistic">The optimistic value.</param>
    public FutureStateMap(bool optimistic)
    {
        _optimistic = optimistic;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="model">The model value.</param>
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
