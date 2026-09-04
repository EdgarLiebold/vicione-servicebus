using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a job type saga map implementation.
/// </summary>
public class JobTypeSagaMap :
    SagaClassMap<JobTypeSaga>
{
    readonly bool _optimistic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="optimistic">The optimistic value.</param>
    public JobTypeSagaMap(bool optimistic)
    {
        _optimistic = optimistic;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="model">The model value.</param>
    protected override void Configure(EntityTypeBuilder<JobTypeSaga> entity, ModelBuilder model)
    {
        entity.OptOutOfEntityFrameworkConventions();

        entity.Property(x => x.CurrentState);

        entity.Ignore(x => x.Version);

        if (_optimistic)
        {
            entity.Property(x => x.RowVersion)
                .IsRowVersion();
        }
        else
            entity.Ignore(x => x.RowVersion);

        entity.Property(x => x.ActiveJobCount);
        entity.Property(x => x.ConcurrentJobLimit);
        entity.Property(x => x.GlobalConcurrentJobLimit);
        entity.Property(x => x.Name);

        entity.Property(x => x.OverrideJobLimit);
        entity.Property(x => x.OverrideLimitExpiration);

        entity.Property(x => x.ActiveJobs)
            .HasJsonConversion();

        entity.Property(x => x.Instances)
            .HasJsonConversion();

        entity.Property(x => x.Properties)
            .HasJsonConversion();
    }
}
