using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Maps job-attempt saga state, its job relationship, uniqueness constraint, and optional row version.</summary>
public class JobAttemptSagaMap :
    SagaClassMap<JobAttemptSaga>
{
    readonly bool _optimistic;

    /// <summary>Initializes a job-attempt saga mapping with the selected concurrency model.</summary>
    /// <param name="useOptimisticConcurrency"><see langword="true"/> to map <c>RowVersion</c>; otherwise it is ignored.</param>
    public JobAttemptSagaMap(bool useOptimisticConcurrency)
    {
        _optimistic = useOptimisticConcurrency;
    }

    /// <summary>Configures job-attempt properties and the cascading job relationship.</summary>
    /// <param name="entity">The job-attempt saga entity builder.</param>
    /// <param name="model">The containing EF Core model builder.</param>
    protected override void Configure(EntityTypeBuilder<JobAttemptSaga> entity, ModelBuilder model)
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

        entity.Property(x => x.JobId);
        entity.HasOne<JobSaga>().WithMany()
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.RetryAttempt);

        entity.HasIndex(x => new
        {
            x.JobId,
            x.RetryAttempt
        }).IsUnique().HasFilter(null);

        entity.Property(x => x.ServiceAddress);

        entity.Property(x => x.InstanceAddress);

        entity.Property(x => x.Started);

        entity.Property(x => x.Faulted);

        entity.Property(x => x.StatusCheckTokenId);
    }
}
