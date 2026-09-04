using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus;

public sealed class DynamoDbSagaRepositoryOptions<TSaga>
    where TSaga : class, ISaga
{
    internal const string TableNameValidationMessage =
        "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period";

    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration = null)
        : this(tableName, expiration, TimeProvider.System)
    {
    }

    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration, TimeProvider timeProvider,
        bool consistentRead = true, bool isEmptyStringValueEnabled = true, bool retrieveDateTimeInUtc = true,
        DynamoDBEntryConversion conversion = null)
    {
        if (!IsValidTableName(tableName))
            throw new ArgumentException(TableNameValidationMessage, nameof(tableName));
        if (expiration < TimeSpan.FromSeconds(30))
            throw new ArgumentOutOfRangeException(nameof(expiration), expiration, "Expiration must be at least 30 seconds when specified.");

        conversion ??= DynamoDBEntryConversion.V2;
        if (!ReferenceEquals(conversion, DynamoDBEntryConversion.V1) && !ReferenceEquals(conversion, DynamoDBEntryConversion.V2))
            throw new ArgumentException("Only the immutable DynamoDBEntryConversion.V1 and V2 instances are supported.", nameof(conversion));

        TableName = tableName;
        Expiration = expiration;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ConsistentRead = consistentRead;
        IsEmptyStringValueEnabled = isEmptyStringValueEnabled;
        RetrieveDateTimeInUtc = retrieveDateTimeInUtc;
        Conversion = conversion;
    }

    public string TableName { get; }
    public TimeSpan? Expiration { get; }
    public TimeProvider TimeProvider { get; }
    public bool ConsistentRead { get; }
    public bool IsEmptyStringValueEnabled { get; }
    public bool RetrieveDateTimeInUtc { get; }
    public DynamoDBEntryConversion Conversion { get; }

    public string FormatSagaKey(Guid correlationId)
    {
        return correlationId.ToString("D");
    }

    internal LoadConfig CreateLoadConfig()
    {
        return new LoadConfig
        {
            ConsistentRead = ConsistentRead,
            Conversion = Conversion,
            IsEmptyStringValueEnabled = IsEmptyStringValueEnabled,
            OverrideTableName = TableName,
            RetrieveDateTimeInUtc = RetrieveDateTimeInUtc
        };
    }

    internal GetTargetTableConfig CreateTargetTableConfig()
    {
        return new GetTargetTableConfig
        {
            Conversion = Conversion,
            IsEmptyStringValueEnabled = IsEmptyStringValueEnabled,
            OverrideTableName = TableName
        };
    }

    internal DeleteConfig CreateDeleteConfig()
    {
        return new DeleteConfig
        {
            Conversion = Conversion,
            IsEmptyStringValueEnabled = IsEmptyStringValueEnabled,
            OverrideTableName = TableName
        };
    }

    internal static bool IsValidTableName(string tableName)
    {
        if (tableName == null || tableName.Length is < 3 or > 255)
            return false;

        foreach (var character in tableName)
        {
            if ((character is >= 'A' and <= 'Z') || (character is >= 'a' and <= 'z') || (character is >= '0' and <= '9')
                || character is '_' or '-' or '.')
                continue;

            return false;
        }

        return true;
    }
}
