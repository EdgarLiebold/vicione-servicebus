using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Represents the DynamoDB document used to persist a saga instance.</summary>
public class DynamoDbSaga
{
    /// <summary>Gets the sort-key value that distinguishes saga documents from other entities in the same partition.</summary>
    [DynamoDBIgnore] public static readonly string DefaultEntityType = "SAGA";

    /// <summary>Creates an empty persistence document with the saga sort-key discriminator.</summary>
    public DynamoDbSaga()
    {
        EntityType = DefaultEntityType;
    }

    /// <summary>Gets or sets the canonical saga correlation identifier stored as the partition key.</summary>
    [DynamoDBHashKey(AttributeName = "PK")]
    public string CorrelationId { get; set; } = null!;

    /// <summary>Gets or sets the saga discriminator stored as the sort key.</summary>
    [DynamoDBRangeKey(AttributeName = "SK")]
    public string EntityType { get; set; } = DefaultEntityType;

    /// <summary>Gets or sets the optimistic concurrency version.</summary>
    public int VersionNumber { get; set; }

    /// <summary>Gets or sets the serialized saga state.</summary>
    public string Properties { get; set; } = null!;

    /// <summary>Gets or sets the optional Unix-time expiration consumed by Amazon DynamoDB time to live.</summary>
    public long? ExpirationEpochSeconds { get; set; }

    /// <summary>Projects the persistence model into an Amazon DynamoDB document for conditional writes.</summary>
    /// <returns>A document containing keys, version, serialized state, and optional expiration.</returns>
    public Document ToDocument()
    {
        var attributes = new Dictionary<string, DynamoDBEntry>
        {
            { "PK", new Primitive(CorrelationId) },
            { "SK", new Primitive(DefaultEntityType) },
            { nameof(VersionNumber), new Primitive(VersionNumber.ToString(CultureInfo.InvariantCulture), true) },
            { nameof(Properties), new Primitive(Properties) }
        };

        if (ExpirationEpochSeconds.HasValue)
            attributes.Add(nameof(ExpirationEpochSeconds),
                new Primitive(ExpirationEpochSeconds.Value.ToString(CultureInfo.InvariantCulture), true));

        return new Document(attributes);
    }
}
