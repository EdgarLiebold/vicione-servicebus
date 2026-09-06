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
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Serializes versioned sagas and executes conditional Amazon DynamoDB persistence operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DynamoDbDatabaseContext<TSaga> :
    DatabaseContext<TSaga>
    where TSaga : class, ISagaVersion
{
    readonly IDynamoDBContext _database;
    readonly DynamoDbSagaRepositoryOptions<TSaga> _options;

    /// <summary>Creates a database context with caller-owned AWS persistence context lifetime.</summary>
    /// <param name="database">The AWS object-persistence context used for saga operations.</param>
    /// <param name="options">The table, conversion, read, time, and expiration settings.</param>
    public DynamoDbDatabaseContext(IDynamoDBContext database, DynamoDbSagaRepositoryOptions<TSaga> options)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Creates a saga document only when its composite key does not already exist.</summary>
    /// <param name="instance">The saga state to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    public Task AddAsync(TSaga instance, CancellationToken cancellationToken)
    {
        return SaveAsync(instance, cancellationToken);
    }

    /// <summary>Inserts a saga document only when its composite key does not already exist.</summary>
    /// <param name="instance">The saga state to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    public Task InsertAsync(TSaga instance, CancellationToken cancellationToken)
    {
        return SaveAsync(instance, cancellationToken);
    }

    /// <summary>Loads, deserializes, and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the validated saga state, or <see langword="null"/> when no document exists.</returns>
    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        var value = await _database.LoadAsync<DynamoDbSaga>(_options.FormatSagaKey(correlationId), DynamoDbSaga.DefaultEntityType,
            _options.CreateLoadConfig(), cancellationToken).ConfigureAwait(false);

        if (value == null)
            return null;

        TSaga? instance = JsonSerializer.Deserialize<TSaga>(value.Properties, ServiceBusMetadataJson.Options);
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

    /// <summary>Increments the saga version and updates the document only when the persisted version still matches.</summary>
    /// <param name="instance">The saga state to serialize and update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional update.</returns>
    public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken)
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

    /// <summary>Deletes the saga document only when the persisted version still matches.</summary>
    /// <param name="instance">The saga state whose document is deleted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional delete.</returns>
    public async Task DeleteAsync(TSaga instance, CancellationToken cancellationToken)
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

    /// <summary>Releases the resources owned by this instance.</summary>
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
            Properties = JsonSerializer.Serialize(instance, ServiceBusMetadataJson.Options),
            ExpirationEpochSeconds = GetExpirationEpochSeconds()
        };
    }

    long? GetExpirationEpochSeconds()
    {
        return _options.Expiration.HasValue
            ? _options.TimeProvider.GetUtcNow().Add(_options.Expiration.Value).ToUnixTimeSeconds()
            : (long?)null;
    }

    async Task SaveAsync(TSaga instance, CancellationToken cancellationToken)
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
