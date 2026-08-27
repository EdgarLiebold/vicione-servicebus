namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.DynamoDBv2.DataModel;
    using Amazon.DynamoDBv2.DocumentModel;
    using Amazon.DynamoDBv2.Model;
    using Serialization;


    public class DynamoDbDatabaseContext<TSaga> :
        DatabaseContext<TSaga>
        where TSaga : class, ISagaVersion
    {
        readonly IDynamoDBContext _database;
        readonly DynamoDbSagaRepositoryOptions<TSaga> _options;

        public DynamoDbDatabaseContext(IDynamoDBContext database, DynamoDbSagaRepositoryOptions<TSaga> options)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Add(TSaga instance, CancellationToken cancellationToken)
        {
            return Save(instance, cancellationToken);
        }

        public Task Insert(TSaga instance, CancellationToken cancellationToken)
        {
            return Save(instance, cancellationToken);
        }

        public async Task<TSaga> Load(Guid correlationId, CancellationToken cancellationToken)
        {
            var value = await _database.LoadAsync<DynamoDbSaga>(_options.FormatSagaKey(correlationId), DynamoDbSaga.DefaultEntityType,
                _options.CreateLoadConfig(), cancellationToken).ConfigureAwait(false);

            if (value == null)
                return null;

            TSaga instance = JsonSerializer.Deserialize<TSaga>(value.Properties, SystemTextJsonMessageSerializer.Options);
            if (instance == null)
                throw new SerializationException($"The DynamoDB saga payload for {typeof(TSaga).Name} was null.");

            string expectedKey = _options.FormatSagaKey(correlationId);
            if (!string.Equals(value.CorrelationId, expectedKey, StringComparison.Ordinal)
                || instance.CorrelationId != correlationId
                || instance.Version != value.VersionNumber)
            {
                throw new SerializationException(
                    $"The DynamoDB saga payload for {typeof(TSaga).Name} does not match its persisted identity or version.");
            }

            return instance;
        }

        public async Task Update(TSaga instance, CancellationToken cancellationToken)
        {
            var expectedVersion = instance.Version;

            try
            {
                var operationConfig = BuildUpdateItemOperationConfig(expectedVersion);
                int nextVersion = checked(expectedVersion + 1);

                instance.Version = nextVersion;

                var updateSaga = GetDynamoDbSaga(instance);

                await _database.GetTargetTable<DynamoDbSaga>(_options.CreateTargetTableConfig())
                    .UpdateItemAsync(updateSaga.ToDocument(), new Primitive(updateSaga.CorrelationId), new Primitive(DynamoDbSaga.DefaultEntityType),
                        operationConfig, cancellationToken).ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException exception)
            {
                instance.Version = expectedVersion;
                throw new DynamoDbSagaConcurrencyException("Saga version conflict", typeof(TSaga), instance.CorrelationId, exception);
            }
            catch
            {
                instance.Version = expectedVersion;
                throw;
            }
        }

        public async Task Delete(TSaga instance, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(instance);

            try
            {
                var operationConfig = new DeleteItemOperationConfig
                {
                    ConditionalExpression = BuildVersionCondition(instance.Version)
                };

                await _database.GetTargetTable<DynamoDbSaga>(_options.CreateTargetTableConfig())
                    .DeleteItemAsync(
                        new Primitive(_options.FormatSagaKey(instance.CorrelationId)),
                        new Primitive(DynamoDbSaga.DefaultEntityType),
                        operationConfig,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException exception)
            {
                throw new DynamoDbSagaConcurrencyException("Saga version conflict", typeof(TSaga), instance.CorrelationId, exception);
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        DynamoDbSaga GetDynamoDbSaga(TSaga instance)
        {
            return new DynamoDbSaga
            {
                CorrelationId = _options.FormatSagaKey(instance.CorrelationId),
                VersionNumber = instance.Version,
                Properties = JsonSerializer.Serialize(instance, SystemTextJsonMessageSerializer.Options),
                ExpirationEpochSeconds = GetExpirationEpochSeconds()
            };
        }

        long? GetExpirationEpochSeconds()
        {
            return _options.Expiration.HasValue
                ? _options.TimeProvider.GetUtcNow().Add(_options.Expiration.Value).ToUnixTimeSeconds()
                : (long?)null;
        }

        async Task Save(TSaga instance, CancellationToken cancellationToken)
        {
            try
            {
                var addSaga = GetDynamoDbSaga(instance);

                var operationConfig = BuildPutItemOperationConfig();

                await _database.GetTargetTable<DynamoDbSaga>(_options.CreateTargetTableConfig())
                    .PutItemAsync(addSaga.ToDocument(), operationConfig, cancellationToken).ConfigureAwait(false);
            }
            catch (ConditionalCheckFailedException exception)
            {
                throw new DynamoDbSagaConcurrencyException("Saga version conflict", typeof(TSaga), instance.CorrelationId, exception);
            }
        }

        static UpdateItemOperationConfig BuildUpdateItemOperationConfig(int expectedVersion)
        {
            return new UpdateItemOperationConfig
            {
                ConditionalExpression = BuildVersionCondition(expectedVersion)
            };
        }

        static Expression BuildVersionCondition(int expectedVersion)
        {
            return new Expression
            {
                ExpressionStatement = "VersionNumber = :versionNumber",
                ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
                {
                    { ":versionNumber", new Primitive(expectedVersion.ToString(CultureInfo.InvariantCulture), true) }
                }
            };
        }

        static PutItemOperationConfig BuildPutItemOperationConfig()
        {
            return new PutItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionStatement = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
                }
            };
        }
    }
}
