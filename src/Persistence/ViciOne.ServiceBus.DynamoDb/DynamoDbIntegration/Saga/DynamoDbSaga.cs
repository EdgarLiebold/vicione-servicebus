using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>
/// Provides a dynamo db saga implementation.
/// </summary>
public class DynamoDbSaga
{
    /// <summary>
    /// Defines the default entity type value.
    /// </summary>
    [DynamoDBIgnore] public static readonly string DefaultEntityType = "SAGA";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public DynamoDbSaga()
    {
        EntityType = DefaultEntityType;
    }

    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    [DynamoDBHashKey(AttributeName = "PK")]
    public string CorrelationId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the entity type value.
    /// </summary>
    [DynamoDBRangeKey(AttributeName = "SK")]
    public string EntityType { get; set; } = DefaultEntityType;

    /// <summary>
    /// Gets or sets the version number value.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Gets or sets the properties value.
    /// </summary>
    public string Properties { get; set; } = null!;

    /// <summary>
    /// Gets or sets the expiration epoch seconds value.
    /// </summary>
    public long? ExpirationEpochSeconds { get; set; }

    /// <summary>
    /// Performs the to document operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
