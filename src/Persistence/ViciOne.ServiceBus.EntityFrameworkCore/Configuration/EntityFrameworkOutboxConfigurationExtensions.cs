using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Registers EF Core inbox/outbox services, relational lock providers, and entity mappings.</summary>
public static class EntityFrameworkOutboxConfigurationExtensions
{
    /// <summary>
    /// Configures the Entity Framework Outbox on the bus, which can subsequently be used to configure
    /// the transactional outbox on a receive endpoint.
    /// </summary>
    /// <typeparam name="TDbContext">The EF Core context that stores the default bus outbox.</typeparam>
    /// <param name="configurator">The default-bus registration that receives the transactional store.</param>
    /// <param name="configure">An optional callback that configures persistence, cleanup, and delivery.</param>
    public static void ConfigureEntityFrameworkTransactionalStore<TDbContext>(this IBusRegistrationConfigurator configurator,
        Action<IEntityFrameworkOutboxConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var outboxConfigurator = new EntityFrameworkOutboxConfigurator<IBus, TDbContext>(configurator.Services);
        outboxConfigurator.Configure(configure);
    }

    /// <summary>
    /// Configures an Entity Framework outbox for a specific MultiBus instance. Bus and DbContext together form
    /// the durable outbox identity, allowing the same DbContext to host isolated outboxes for multiple buses.
    /// </summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <typeparam name="TDbContext">The EF Core context that stores this bus instance's outbox.</typeparam>
    /// <param name="configurator">The typed-bus registration that receives the transactional store.</param>
    /// <param name="configure">An optional callback that configures persistence, cleanup, and delivery.</param>
    public static void ConfigureEntityFrameworkTransactionalStore<TBus, TDbContext>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IEntityFrameworkOutboxConfigurator>? configure = null)
        where TBus : class, IBus
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var outboxConfigurator = new EntityFrameworkOutboxConfigurator<TBus, TDbContext>(configurator.Services);
        outboxConfigurator.Configure(configure);
    }

    /// <summary>Enables EF Core inbox deduplication and receive-side outbox persistence on an endpoint.</summary>
    /// <typeparam name="TDbContext">The EF Core context that stores inbox and receive-side outbox state.</typeparam>
    /// <param name="configurator">The receive endpoint on which the EF Core outbox is enabled.</param>
    /// <param name="context">The registration context used to resolve the DbContext and outbox services.</param>
    /// <param name="configure">An optional callback that configures receive-side outbox behavior.</param>
    public static void UseEntityFrameworkOutbox<TDbContext>(this IReceiveEndpointConfigurator configurator, IRegistrationContext context,
        Action<IOutboxOptionsConfigurator>? configure = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(context);

        var observer = new OutboxConsumePipeSpecificationObserver<TDbContext>(configurator, context);

        configure?.Invoke(observer);

        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }


    /// <summary>Selects SQL Server lock statements for the outbox.</summary>
    /// <param name="configurator">The outbox configuration on which SQL Server locking is selected.</param>
    /// <returns>The same outbox configurator.</returns>
    public static IEntityFrameworkOutboxConfigurator UseSqlServer(this IEntityFrameworkOutboxConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new SqlServerLockStatementProvider();

        return configurator;
    }

    /// <summary>Selects PostgreSQL lock statements and read-committed transactions for the outbox.</summary>
    /// <param name="configurator">The outbox configuration on which PostgreSQL locking is selected.</param>
    /// <returns>The same outbox configurator.</returns>
    public static IEntityFrameworkOutboxConfigurator UsePostgreSql(this IEntityFrameworkOutboxConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new PostgreSqlLockStatementProvider();
        configurator.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;

        return configurator;
    }

    /// <summary>Selects SQLite statements and serializable transactions for the outbox.</summary>
    /// <param name="configurator">The outbox configuration on which SQLite locking is selected.</param>
    /// <returns>The same outbox configurator.</returns>
    public static IEntityFrameworkOutboxConfigurator UseSqlite(this IEntityFrameworkOutboxConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new SqliteLockStatementProvider();
        configurator.IsolationLevel = System.Data.IsolationLevel.Serializable;

        return configurator;
    }

    /// <summary>
    /// Adds all three entities (<see cref="InboxState" />, <see cref="OutboxState" />, and <see cref="OutboxMessage" />)
    /// to the DbContext. If this method is used, the <see cref="AddInboxStateEntity" />, <see cref="AddOutboxStateEntity" />, and
    /// <see cref="AddOutboxMessageEntity" /> methods should not be used.
    /// </summary>
    /// <param name="modelBuilder">The model being configured.</param>
    /// <param name="callback">An optional callback applied to each outbox entity mapping.</param>
    public static void AddTransactionalOutboxEntities(this ModelBuilder modelBuilder, Action<EntityTypeBuilder>? callback = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.AddInboxStateEntity(callback);
        modelBuilder.AddOutboxStateEntity(callback);
        modelBuilder.AddOutboxMessageEntity(callback);
    }

    /// <summary>Adds the <see cref="InboxState" /> entity to the DbContext. If used, the <see cref="AddTransactionalOutboxEntities" /> method should not be used.</summary>
    /// <param name="modelBuilder">The model to which the inbox state mapping is added.</param>
    /// <param name="callback">An optional callback that further configures the inbox state mapping.</param>
    public static void AddInboxStateEntity(this ModelBuilder modelBuilder, Action<EntityTypeBuilder<InboxState>>? callback = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        EntityTypeBuilder<InboxState> inbox = modelBuilder.Entity<InboxState>();

        inbox.ConfigureInboxStateEntity();

        callback?.Invoke(inbox);
    }

    /// <summary>Configures the <see cref="InboxState" /> entity using an already created <see cref="ModelBuilder" />.</summary>
    /// <param name="inbox">The inbox state mapping to configure.</param>
    public static void ConfigureInboxStateEntity(this EntityTypeBuilder<InboxState> inbox)
    {
        ArgumentNullException.ThrowIfNull(inbox);
        inbox.OptOutOfEntityFrameworkConventions();

        inbox.Property(p => p.Id);
        inbox.HasKey(p => p.Id);

        inbox.Property(p => p.MessageId);
        inbox.Property(p => p.ConsumerId);

        inbox.HasAlternateKey(p => new
        {
            p.MessageId,
            p.ConsumerId
        });

        inbox.Property(p => p.LockId);

        inbox.Property(p => p.RowVersion).IsRowVersion();

        inbox.Property(p => p.Received);
        inbox.Property(p => p.ReceiveCount);
        inbox.Property(p => p.ExpirationTime);
        inbox.Property(p => p.Consumed);

        inbox.Property(p => p.Delivered);
        inbox.HasIndex(p => p.Delivered);

        inbox.Property(p => p.LastSequenceNumber);
    }

    /// <summary>Adds the <see cref="OutboxState" /> entity to the DbContext. If used, the <see cref="AddTransactionalOutboxEntities" /> method should not be used.</summary>
    /// <param name="modelBuilder">The model to which the outbox state mapping is added.</param>
    /// <param name="callback">An optional callback that further configures the outbox state mapping.</param>
    public static void AddOutboxStateEntity(this ModelBuilder modelBuilder, Action<EntityTypeBuilder<OutboxState>>? callback = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        EntityTypeBuilder<OutboxState> outbox = modelBuilder.Entity<OutboxState>();

        outbox.ConfigureOutboxStateEntity();

        callback?.Invoke(outbox);
    }

    /// <summary>Configures the <see cref="OutboxState" /> entity using an already created <see cref="ModelBuilder" />.</summary>
    /// <param name="outbox">The outbox state mapping to configure.</param>
    public static void ConfigureOutboxStateEntity(this EntityTypeBuilder<OutboxState> outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        outbox.OptOutOfEntityFrameworkConventions();

        outbox.Property(p => p.OutboxId);
        outbox.HasKey(p => p.OutboxId);

        outbox.Property(p => p.LockId);

        outbox.Property(p => p.RowVersion).IsRowVersion();

        outbox.Property(p => p.BusKey).HasMaxLength(256);
        outbox.Property(p => p.Created);
        outbox.Property(p => p.Status);
        outbox.Property(p => p.NextDeliveryTime);
        outbox.Property(p => p.DeliveryAttempts);
        outbox.Property(p => p.LastFailureKind);
        outbox.Property(p => p.LastFailureCode);
        outbox.Property(p => p.LastFailureTime);
        outbox.Property(p => p.LastExceptionType).HasMaxLength(512);
        outbox.Property(p => p.FailedSequenceNumber);
        outbox.Property(p => p.FailedMessageId);
        outbox.Property(p => p.Delivered);
        outbox.Property(p => p.LastSequenceNumber);

        outbox.HasIndex(p => new
        {
            p.BusKey,
            p.Status,
            p.NextDeliveryTime,
            p.Created
        });
    }

    /// <summary>Adds the <see cref="OutboxMessage" /> entity to the DbContext. If used, the <see cref="AddTransactionalOutboxEntities" /> method should not be used.</summary>
    /// <param name="modelBuilder">The model to which the outbox message mapping is added.</param>
    /// <param name="callback">An optional callback that further configures the outbox message mapping.</param>
    public static void AddOutboxMessageEntity(this ModelBuilder modelBuilder, Action<EntityTypeBuilder<OutboxMessage>>? callback = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        EntityTypeBuilder<OutboxMessage> outbox = modelBuilder.Entity<OutboxMessage>();

        outbox.ConfigureOutboxMessageEntity();

        callback?.Invoke(outbox);
    }

    /// <summary>Configures the <see cref="OutboxMessage" /> entity using an already created <see cref="ModelBuilder" />.</summary>
    /// <param name="outbox">The outbox message mapping to configure.</param>
    public static void ConfigureOutboxMessageEntity(this EntityTypeBuilder<OutboxMessage> outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        outbox.OptOutOfEntityFrameworkConventions();

        outbox.Property(p => p.SequenceNumber);
        outbox.HasKey(p => p.SequenceNumber);

        outbox.Property(p => p.MessageId);

        outbox.Property(p => p.ConversationId);
        outbox.Property(p => p.CorrelationId);
        outbox.Property(p => p.InitiatorId);
        outbox.Property(p => p.RequestId);

        outbox.Property(p => p.SourceAddress).HasMaxLength(256);
        outbox.Property(p => p.DestinationAddress).HasMaxLength(256);
        outbox.Property(p => p.ResponseAddress).HasMaxLength(256);
        outbox.Property(p => p.FaultAddress).HasMaxLength(256);

        outbox.Property(p => p.ExpirationTime);
        outbox.HasIndex(p => p.ExpirationTime);

        outbox.Property(p => p.EnqueueTime);
        outbox.HasIndex(p => p.EnqueueTime);

        outbox.Property(p => p.SentTime);

        outbox.Property(p => p.InboxMessageId);
        outbox.Property(p => p.InboxConsumerId);
        outbox.HasIndex(p => new
        {
            p.InboxMessageId,
            p.InboxConsumerId,
            p.SequenceNumber
        }).IsUnique();
        outbox.HasOne<InboxState>().WithMany().IsRequired(false)
            .HasForeignKey(p => new
            {
                p.InboxMessageId,
                p.InboxConsumerId
            }).HasPrincipalKey(p => new
            {
                p.MessageId,
                p.ConsumerId
            });

        outbox.Property(p => p.OutboxId).IsRequired(false);
        outbox.HasIndex(p => new
        {
            p.OutboxId,
            p.SequenceNumber,
        }).IsUnique();
        outbox.HasOne<OutboxState>().WithMany().IsRequired(false)
            .HasForeignKey(p => p.OutboxId);

        outbox.Property(p => p.Headers);

        outbox.Property(p => p.Properties);

        outbox.Property(p => p.ContentType)
            .HasMaxLength(256);
        outbox.Property(p => p.MessageType);

        outbox.Property(p => p.Body);
    }

    /// <summary>
    /// Clears convention-provided maximum lengths so each outbox mapping declares its storage limits explicitly.
    /// </summary>
    /// <param name="builder">The entity mapping whose convention-provided maximum lengths are cleared.</param>
    internal static void OptOutOfEntityFrameworkConventions(this EntityTypeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var properties in builder.Metadata.GetProperties())
            properties.SetMaxLength(null);
    }
}
