using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Defines immutable Amazon DynamoDB persistence settings for one saga type.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public sealed class DynamoDbSagaRepositoryOptions<TSaga>
    where TSaga : class, ISaga
{
    internal const string TableNameValidationMessage =
        "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period";

    /// <summary>Creates repository settings with system time and the default AWS SDK persistence options.</summary>
    /// <param name="tableName">The Amazon DynamoDB table that stores saga documents.</param>
    /// <param name="expiration">An optional lifetime written to the document's time-to-live attribute.</param>
    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration = null)
        : this(tableName, expiration, TimeProvider.System)
    {
    }

    /// <summary>Creates fully specified and validated repository settings.</summary>
    /// <param name="tableName">The Amazon DynamoDB table that stores saga documents.</param>
    /// <param name="expiration">An optional lifetime written to the document's time-to-live attribute.</param>
    /// <param name="timeProvider">The time source used to calculate the expiration epoch.</param>
    /// <param name="consistentRead">Whether saga loads use strongly consistent reads.</param>
    /// <param name="isEmptyStringValueEnabled">Whether the AWS object-persistence model accepts empty string values.</param>
    /// <param name="retrieveDateTimeInUtc">Whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</param>
    /// <param name="conversion">The immutable AWS SDK V1 or V2 entry-conversion rules, or <see langword="null"/> for V2.</param>
    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration, TimeProvider timeProvider,
        bool consistentRead = true, bool isEmptyStringValueEnabled = true, bool retrieveDateTimeInUtc = true,
        DynamoDBEntryConversion? conversion = null)
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

    /// <summary>Gets the Amazon DynamoDB table that stores saga documents.</summary>
    public string TableName { get; }
    /// <summary>Gets the optional relative lifetime written to each persisted saga document.</summary>
    public TimeSpan? Expiration { get; }
    /// <summary>Gets the time source used to calculate document expiration.</summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>Gets whether saga loads use strongly consistent reads.</summary>
    public bool ConsistentRead { get; }
    /// <summary>Gets whether the AWS object-persistence model accepts empty string values.</summary>
    public bool IsEmptyStringValueEnabled { get; }
    /// <summary>Gets whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</summary>
    public bool RetrieveDateTimeInUtc { get; }
    /// <summary>Gets the AWS SDK entry-conversion rules used for persistence.</summary>
    public DynamoDBEntryConversion Conversion { get; }

    /// <summary>Formats a saga correlation identifier as the document partition key.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <returns>The canonical dashed GUID representation used as the partition key.</returns>
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
