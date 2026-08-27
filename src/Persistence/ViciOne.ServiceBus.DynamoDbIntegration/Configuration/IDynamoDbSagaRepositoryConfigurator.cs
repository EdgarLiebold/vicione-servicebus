namespace ViciOne.ServiceBus
{
    using System;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;


    public interface IDynamoDbSagaRepositoryConfigurator
    {
        string TableName { set; }
        TimeSpan? Expiration { set; }
        TimeProvider TimeProvider { set; }
        bool ConsistentRead { set; }
        bool IsEmptyStringValueEnabled { set; }
        bool RetrieveDateTimeInUtc { set; }
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


    public interface IDynamoDbSagaRepositoryConfigurator<TSaga> :
        IDynamoDbSagaRepositoryConfigurator
        where TSaga : class, ISagaVersion
    {
    }
}
