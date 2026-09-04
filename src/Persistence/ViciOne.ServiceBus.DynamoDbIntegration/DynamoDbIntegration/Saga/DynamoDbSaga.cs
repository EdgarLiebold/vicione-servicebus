using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

public class DynamoDbSaga
{
    [DynamoDBIgnore] public static readonly string DefaultEntityType = "SAGA";

    public DynamoDbSaga()
    {
        EntityType = DefaultEntityType;
    }

    [DynamoDBHashKey(AttributeName = "PK")]
    public string CorrelationId { get; set; }

    [DynamoDBRangeKey(AttributeName = "SK")]
    public string EntityType { get; set; } = DefaultEntityType;

    public int VersionNumber { get; set; }

    public string Properties { get; set; }

    public long? ExpirationEpochSeconds { get; set; }

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
