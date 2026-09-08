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
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

internal sealed class DynamoDbSagaStore<TSaga>(
    IDynamoDBContext providerContext,
    DynamoDbSagaRepositoryOptions<TSaga> options) :
    IDynamoDbSagaStore<TSaga>
    where TSaga : class, ISagaVersion
{
    readonly IDynamoDBContext _providerContext = providerContext ?? throw new ArgumentNullException(nameof(providerContext));
    readonly DynamoDbSagaRepositoryOptions<TSaga> _options = options ?? throw new ArgumentNullException(nameof(options));
    int _disposed;

    public async Task CreateAsync(TSaga instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);

        try
        {
            DynamoDbSagaDocument sagaDocument = CreateDocument(instance);
            var operationConfig = new PutItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionStatement = "attribute_not_exists(PK) AND attribute_not_exists(SK)",
                },
            };

            await _providerContext.GetTargetTable<DynamoDbSagaDocument>(_options.CreateTargetTableConfig())
                .PutItemAsync(sagaDocument.ToDocument(), operationConfig, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ConditionalCheckFailedException exception)
        {
            throw new DynamoDbSagaConcurrencyException(
                "A saga with the same correlation identifier already exists.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
        }
    }

    /// <summary>Loads, deserializes, and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the validated saga state, or <see langword="null"/> when no document exists.</returns>
    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken)
    {
        DynamoDbSagaDocument? value = await _providerContext.LoadAsync<DynamoDbSagaDocument>(
                DynamoDbSagaRepositoryOptions<TSaga>.FormatSagaKey(correlationId),
                DynamoDbSagaDocument.EntityTypeValue,
                _options.CreateLoadConfig(),
                cancellationToken)
            .ConfigureAwait(false);

        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value.Properties))
            throw new SerializationException($"The DynamoDB saga payload for {typeof(TSaga).Name} was empty.");

        TSaga instance = JsonSerializer.Deserialize<TSaga>(value.Properties, ServiceBusMetadataJson.Options)
            ?? throw new SerializationException($"The DynamoDB saga payload for {typeof(TSaga).Name} was null.");

        string expectedKey = DynamoDbSagaRepositoryOptions<TSaga>.FormatSagaKey(correlationId);
        if (!string.Equals(value.CorrelationId, expectedKey, StringComparison.Ordinal)
            || !string.Equals(value.EntityType, DynamoDbSagaDocument.EntityTypeValue, StringComparison.Ordinal)
            || instance.CorrelationId != correlationId
            || instance.Version != value.VersionNumber)
        {
            throw new SerializationException(
                $"The DynamoDB saga payload for {typeof(TSaga).Name} does not match its persisted identity, entity type, or version.");
        }

        return instance;
    }

    /// <summary>Increments the saga version and updates the document only when the persisted version still matches.</summary>
    /// <param name="instance">The saga state to serialize and update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional update.</returns>
    public async Task UpdateAsync(TSaga instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        int expectedVersion = instance.Version;

        try
        {
            UpdateItemOperationConfig operationConfig = BuildUpdateItemOperationConfig(expectedVersion);
            int nextVersion = checked(expectedVersion + 1);

            instance.Version = nextVersion;

            DynamoDbSagaDocument updateSaga = CreateDocument(instance);

            await _providerContext.GetTargetTable<DynamoDbSagaDocument>(_options.CreateTargetTableConfig())
                .UpdateItemAsync(
                    updateSaga.ToDocument(),
                    new Primitive(updateSaga.CorrelationId),
                    new Primitive(DynamoDbSagaDocument.EntityTypeValue),
                    operationConfig,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ConditionalCheckFailedException exception)
        {
            instance.Version = expectedVersion;
            throw new DynamoDbSagaConcurrencyException(
                "The persisted saga version changed before the update completed.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
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
                ConditionalExpression = BuildVersionCondition(instance.Version),
            };

            await _providerContext.GetTargetTable<DynamoDbSagaDocument>(_options.CreateTargetTableConfig())
                .DeleteItemAsync(
                    new Primitive(DynamoDbSagaRepositoryOptions<TSaga>.FormatSagaKey(instance.CorrelationId)),
                    new Primitive(DynamoDbSagaDocument.EntityTypeValue),
                    operationConfig,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ConditionalCheckFailedException exception)
        {
            throw new DynamoDbSagaConcurrencyException(
                "The persisted saga version changed before the delete completed.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
        }
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _providerContext.Dispose();
    }

    private DynamoDbSagaDocument CreateDocument(TSaga instance)
    {
        return new DynamoDbSagaDocument
        {
            CorrelationId = DynamoDbSagaRepositoryOptions<TSaga>.FormatSagaKey(instance.CorrelationId),
            VersionNumber = instance.Version,
            Properties = JsonSerializer.Serialize(instance, ServiceBusMetadataJson.Options),
            ExpirationEpochSeconds = GetExpirationEpochSeconds(),
        };
    }

    private long? GetExpirationEpochSeconds()
    {
        return _options.TimeToLive.HasValue
            ? _options.TimeProvider.GetUtcNow().Add(_options.TimeToLive.Value).ToUnixTimeSeconds()
            : (long?)null;
    }

    private static UpdateItemOperationConfig BuildUpdateItemOperationConfig(int expectedVersion)
    {
        return new UpdateItemOperationConfig
        {
            ConditionalExpression = BuildVersionCondition(expectedVersion),
        };
    }

    private static Expression BuildVersionCondition(int expectedVersion)
    {
        return new Expression
        {
            ExpressionStatement = "VersionNumber = :versionNumber",
            ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>
            {
                [":versionNumber"] = new Primitive(
                    expectedVersion.ToString(CultureInfo.InvariantCulture),
                    true),
            },
        };
    }
}
