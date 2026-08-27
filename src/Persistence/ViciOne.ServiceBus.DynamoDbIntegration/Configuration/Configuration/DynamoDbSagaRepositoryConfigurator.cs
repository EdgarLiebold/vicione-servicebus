namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DataModel;
    using DynamoDbIntegration.Saga;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Saga;


    public class DynamoDbSagaRepositoryConfigurator<TSaga> :
        IDynamoDbSagaRepositoryConfigurator<TSaga>,
        ISpecification
        where TSaga : class, ISagaVersion
    {
        Func<IServiceProvider, IDynamoDBContext> _contextFactory;

        public DynamoDbSagaRepositoryConfigurator()
        {
            TimeProvider = TimeProvider.System;
            ConsistentRead = true;
            IsEmptyStringValueEnabled = true;
            RetrieveDateTimeInUtc = true;
            Conversion = DynamoDBEntryConversion.V2;
        }

        public string TableName { get; set; }
        public TimeSpan? Expiration { get; set; }
        public TimeProvider TimeProvider { get; set; }
        public bool ConsistentRead { get; set; }
        public bool IsEmptyStringValueEnabled { get; set; }
        public bool RetrieveDateTimeInUtc { get; set; }
        public DynamoDBEntryConversion Conversion { get; set; }

        public void ContextFactory(Func<IDynamoDBContext> contextFactory)
        {
            ArgumentNullException.ThrowIfNull(contextFactory);

            _contextFactory = _ => contextFactory();
        }

        public void ContextFactory(Func<IServiceProvider, IDynamoDBContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

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

        public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
        {
            ArgumentNullException.ThrowIfNull(configurator);

            configurator.TryAddSingleton<Func<IDynamoDBContext>>(provider => () => _contextFactory(provider));
            configurator.TryAddSingleton(new DynamoDbSagaRepositoryOptions<TSaga>(TableName, Expiration, TimeProvider, ConsistentRead,
                IsEmptyStringValueEnabled, RetrieveDateTimeInUtc, Conversion));
            configurator.RegisterLoadSagaRepository<TSaga, DynamoDbSagaRepositoryContextFactory<TSaga>>();
            configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
                DynamoDbSagaRepositoryContextFactory<TSaga>>();
        }
    }
}
