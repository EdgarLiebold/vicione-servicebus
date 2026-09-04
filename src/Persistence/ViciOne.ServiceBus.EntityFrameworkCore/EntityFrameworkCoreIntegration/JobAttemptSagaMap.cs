using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a job attempt saga map implementation.
/// </summary>
public class JobAttemptSagaMap :
    SagaClassMap<JobAttemptSaga>
{
    readonly bool _optimistic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="optimistic">The optimistic value.</param>
    public JobAttemptSagaMap(bool optimistic)
    {
        _optimistic = optimistic;
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="model">The model value.</param>
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
