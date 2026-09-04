using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dynamo db saga repository configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DynamoDbSagaRepositoryConfigurator<TSaga> :
    IDynamoDbSagaRepositoryConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISagaVersion
{
    Func<IServiceProvider, IDynamoDBContext>? _contextFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public DynamoDbSagaRepositoryConfigurator()
    {
        TimeProvider = TimeProvider.System;
        ConsistentRead = true;
        IsEmptyStringValueEnabled = true;
        RetrieveDateTimeInUtc = true;
        Conversion = DynamoDBEntryConversion.V2;
    }

    /// <summary>
    /// Gets or sets the table name value.
    /// </summary>
    public string TableName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the expiration value.
    /// </summary>
    public TimeSpan? Expiration { get; set; }
    /// <summary>
    /// Gets or sets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; set; }
    /// <summary>
    /// Gets or sets the consistent read value.
    /// </summary>
    public bool ConsistentRead { get; set; }
    /// <summary>
    /// Gets or sets the is empty string value enabled value.
    /// </summary>
    public bool IsEmptyStringValueEnabled { get; set; }
    /// <summary>
    /// Gets or sets the retrieve date time in utc value.
    /// </summary>
    public bool RetrieveDateTimeInUtc { get; set; }
    /// <summary>
    /// Gets or sets the conversion value.
    /// </summary>
    public DynamoDBEntryConversion Conversion { get; set; }

    /// <summary>
    /// Performs the context factory operation.
    /// </summary>
    /// <param name="contextFactory">The context factory value.</param>
    public void ContextFactory(Func<IDynamoDBContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        _contextFactory = _ => contextFactory();
    }

    /// <summary>
    /// Performs the context factory operation.
    /// </summary>
    /// <param name="contextFactory">The context factory value.</param>
    public void ContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
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
