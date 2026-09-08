using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Configures Amazon DynamoDB persistence for versioned sagas.</summary>
public interface IDynamoDbSagaRepositoryConfigurator
{
    /// <summary>Sets the Amazon DynamoDB table that stores saga documents.</summary>
    string TableName { set; }
    /// <summary>Sets the optional lifetime written to each saga document's time-to-live attribute.</summary>
    TimeSpan? TimeToLive { set; }
    /// <summary>Sets the time source used to calculate the expiration epoch.</summary>
    TimeProvider TimeProvider { set; }
    /// <summary>Sets whether saga loads use strongly consistent reads.</summary>
    bool ConsistentRead { set; }
    /// <summary>Sets whether the AWS object-persistence model accepts empty string values.</summary>
    bool AllowEmptyStrings { set; }
    /// <summary>Sets whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</summary>
    bool RetrieveDateTimeAsUtc { set; }
    /// <summary>Sets the immutable AWS SDK entry-conversion version used for persistence.</summary>
    DynamoDBEntryConversion EntryConversion { set; }

    /// <summary>Supplies a factory that creates each operation-scoped Amazon DynamoDB persistence context.</summary>
    /// <param name="contextFactory">The factory whose returned context is owned and disposed by the repository.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contextFactory"/> is <see langword="null"/>.</exception>
    void UseContextFactory(Func<IDynamoDBContext> contextFactory);

    /// <summary>Supplies a service-provider-aware factory that creates each operation-scoped Amazon DynamoDB persistence context.</summary>
    /// <param name="contextFactory">The factory whose returned context is owned and disposed by the repository.</param>
    /// <exception cref="ArgumentNullException"><paramref name="contextFactory"/> is <see langword="null"/>.</exception>
    void UseContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory);
}


/// <summary>Configures Amazon DynamoDB persistence for one versioned saga type.</summary>
/// <typeparam name="TSaga">The versioned saga state stored by the repository.</typeparam>
public interface IDynamoDbSagaRepositoryConfigurator<TSaga> :
    IDynamoDbSagaRepositoryConfigurator
    where TSaga : class, ISagaVersion
{
}
