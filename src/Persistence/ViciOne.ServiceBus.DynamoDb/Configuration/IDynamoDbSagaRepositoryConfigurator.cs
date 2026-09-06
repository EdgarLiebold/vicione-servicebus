using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Configures Amazon DynamoDB persistence for versioned sagas.</summary>
public interface IDynamoDbSagaRepositoryConfigurator
{
    /// <summary>Sets the Amazon DynamoDB table that stores saga documents.</summary>
    string TableName { set; }
    /// <summary>Sets the optional lifetime written to the saga document's time-to-live attribute.</summary>
    TimeSpan? Expiration { set; }
    /// <summary>Sets the time source used to calculate the expiration epoch.</summary>
    TimeProvider TimeProvider { set; }
    /// <summary>Sets whether saga loads use strongly consistent reads.</summary>
    bool ConsistentRead { set; }
    /// <summary>Sets whether the AWS object-persistence model accepts empty string values.</summary>
    bool IsEmptyStringValueEnabled { set; }
    /// <summary>Sets whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</summary>
    bool RetrieveDateTimeInUtc { set; }
    /// <summary>Sets the immutable AWS SDK entry-conversion version used for persistence.</summary>
    DynamoDBEntryConversion Conversion { set; }

    /// <summary>Supplies a factory that creates the Amazon DynamoDB persistence context.</summary>
    /// <param name="contextFactory">The context factory invoked by each repository context.</param>
    void ContextFactory(Func<IDynamoDBContext> contextFactory);

    /// <summary>Supplies a factory that resolves the Amazon DynamoDB persistence context from dependency injection.</summary>
    /// <param name="contextFactory">The service-provider-aware context factory.</param>
    void ContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory);
}


/// <summary>Configures Amazon DynamoDB persistence for one versioned saga type.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IDynamoDbSagaRepositoryConfigurator<TSaga> :
    IDynamoDbSagaRepositoryConfigurator
    where TSaga : class, ISagaVersion
{
}
