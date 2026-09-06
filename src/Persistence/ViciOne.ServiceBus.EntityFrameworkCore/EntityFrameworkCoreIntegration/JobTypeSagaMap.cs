using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Maps job-type concurrency limits and JSON-owned active-job state with optional row-version concurrency.</summary>
public class JobTypeSagaMap :
    SagaClassMap<JobTypeSaga>
{
    readonly bool _optimistic;

    /// <summary>Initializes a job-type saga mapping with the selected concurrency model.</summary>
    /// <param name="optimistic"><see langword="true"/> to map <c>RowVersion</c>; otherwise it is ignored.</param>
    public JobTypeSagaMap(bool optimistic)
    {
        _optimistic = optimistic;
    }

    /// <summary>Configures job-type limits, JSON collections, and concurrency properties.</summary>
    /// <param name="entity">The job-type saga entity builder.</param>
    /// <param name="model">The containing EF Core model builder.</param>
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
