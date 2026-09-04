using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>
/// Defines the contract for dynamo db saga repository configurator.
/// </summary>
public interface IDynamoDbSagaRepositoryConfigurator
{
    /// <summary>
    /// Gets or sets the table name value.
    /// </summary>
    string TableName { set; }
    /// <summary>
    /// Gets or sets the expiration value.
    /// </summary>
    TimeSpan? Expiration { set; }
    /// <summary>
    /// Gets or sets the time provider value.
    /// </summary>
    TimeProvider TimeProvider { set; }
    /// <summary>
    /// Gets or sets the consistent read value.
    /// </summary>
    bool ConsistentRead { set; }
    /// <summary>
    /// Gets or sets the is empty string value enabled value.
    /// </summary>
    bool IsEmptyStringValueEnabled { set; }
    /// <summary>
    /// Gets or sets the retrieve date time in utc value.
    /// </summary>
    bool RetrieveDateTimeInUtc { set; }
    /// <summary>
    /// Gets or sets the conversion value.
    /// </summary>
    DynamoDBEntryConversion Conversion { set; }

    /// <summary>
    /// Factory method to get the DynamoDb context
    /// </summary>
    /// <param name="contextFactory"></param>
    void ContextFactory(Func<IDynamoDBContext> contextFactory);

    /// <summary>
    /// Use the container to build the DynamoDb context
    /// </summary>
    /// <param name="contextFactory"></param>
    void ContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory);
}


/// <summary>
/// Defines the contract for dynamo db saga repository configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IDynamoDbSagaRepositoryConfigurator<TSaga> :
    IDynamoDbSagaRepositoryConfigurator
    where TSaga : class, ISagaVersion
{
}
