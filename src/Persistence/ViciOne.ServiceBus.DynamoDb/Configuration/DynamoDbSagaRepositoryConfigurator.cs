using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Collects and validates Amazon DynamoDB persistence settings for one versioned saga type.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DynamoDbSagaRepositoryConfigurator<TSaga> :
    IDynamoDbSagaRepositoryConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISagaVersion
{
    Func<IServiceProvider, IDynamoDBContext>? _contextFactory;

    /// <summary>Creates a configurator with strongly consistent reads, current conversion rules, UTC retrieval, and system time.</summary>
    public DynamoDbSagaRepositoryConfigurator()
    {
        TimeProvider = TimeProvider.System;
        ConsistentRead = true;
        IsEmptyStringValueEnabled = true;
        RetrieveDateTimeInUtc = true;
        Conversion = DynamoDBEntryConversion.V2;
    }

    /// <summary>Gets or sets the Amazon DynamoDB table that stores saga documents.</summary>
    public string TableName { get; set; } = null!;
    /// <summary>Gets or sets the optional lifetime written to the saga document's time-to-live attribute.</summary>
    public TimeSpan? Expiration { get; set; }
    /// <summary>Gets or sets the time source used to calculate the expiration epoch.</summary>
    public TimeProvider TimeProvider { get; set; }
    /// <summary>Gets or sets whether saga loads use strongly consistent reads.</summary>
    public bool ConsistentRead { get; set; }
    /// <summary>Gets or sets whether the AWS object-persistence model accepts empty string values.</summary>
    public bool IsEmptyStringValueEnabled { get; set; }
    /// <summary>Gets or sets whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</summary>
    public bool RetrieveDateTimeInUtc { get; set; }
    /// <summary>Gets or sets the immutable AWS SDK entry-conversion version used for persistence.</summary>
    public DynamoDBEntryConversion Conversion { get; set; }

    /// <summary>Supplies a factory that creates the Amazon DynamoDB persistence context.</summary>
    /// <param name="contextFactory">The context factory invoked by each repository context.</param>
    public void ContextFactory(Func<IDynamoDBContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        _contextFactory = _ => contextFactory();
    }

    /// <summary>Supplies a service-provider-aware factory that creates the Amazon DynamoDB persistence context.</summary>
    /// <param name="contextFactory">The factory that resolves the context from the registration service provider.</param>
    public void ContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_contextFactory == null)
            yield return this.Failure("ContextFactory", "must be specified");
        if (!DynamoDbSagaRepositoryOptions<TSaga>.IsValidTableName(TableName))
            yield return this.Failure("TableName", DynamoDbSagaRepositoryOptions<TSaga>.TableNameValidationMessage);
        if (Expiration < TimeSpan.FromSeconds(30))
            yield return this.Failure("Expiration", "If specified, must be at least 30 seconds");
        if (TimeProvider == null)
            yield return this.Failure("TimeProvider", "must be specified");
        if (!ReferenceEquals(Conversion, DynamoDBEntryConversion.V1) && !ReferenceEquals(Conversion, DynamoDBEntryConversion.V2))
            yield return this.Failure("Conversion", "must be the immutable DynamoDBEntryConversion.V1 or V2 instance");
    }

    /// <summary>Registers the validated Amazon DynamoDB repository services for the saga type.</summary>
    /// <param name="configurator">The saga repository registration to update.</param>
    public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        Func<IServiceProvider, IDynamoDBContext> contextFactory = _contextFactory
            ?? throw new InvalidOperationException("The DynamoDB context factory must be configured before registration.");

        configurator.TryAddSingleton(provider => new DynamoDbContextFactory<TSaga>(() => contextFactory(provider)));
        configurator.TryAddSingleton(new DynamoDbSagaRepositoryOptions<TSaga>(TableName, Expiration, TimeProvider, ConsistentRead,
            IsEmptyStringValueEnabled, RetrieveDateTimeInUtc, Conversion));
        configurator.RegisterLoadSagaRepository<TSaga, DynamoDbSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
            DynamoDbSagaRepositoryContextFactory<TSaga>>();
    }
}
