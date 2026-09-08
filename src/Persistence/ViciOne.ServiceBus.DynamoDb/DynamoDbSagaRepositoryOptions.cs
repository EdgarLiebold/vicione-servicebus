using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Defines immutable Amazon DynamoDB persistence settings for one saga type.</summary>
/// <typeparam name="TSaga">The versioned saga state stored in the configured table.</typeparam>
public sealed class DynamoDbSagaRepositoryOptions<TSaga>
    where TSaga : class, ISagaVersion
{
    internal const string TableNameValidationMessage =
        "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period";

    /// <summary>Creates repository settings with system time and the default AWS SDK persistence options.</summary>
    /// <param name="tableName">The Amazon DynamoDB table that stores saga documents.</param>
    /// <param name="timeToLive">An optional lifetime written to each saga document's time-to-live attribute.</param>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> is not a valid Amazon DynamoDB table name.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeToLive"/> is shorter than 30 seconds.</exception>
    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? timeToLive = null)
        : this(tableName, timeToLive, TimeProvider.System)
    {
    }

    /// <summary>Creates fully specified and validated repository settings.</summary>
    /// <param name="tableName">The Amazon DynamoDB table that stores saga documents.</param>
    /// <param name="timeToLive">An optional lifetime written to each saga document's time-to-live attribute.</param>
    /// <param name="timeProvider">The time source used to calculate the expiration epoch.</param>
    /// <param name="consistentRead">Whether saga loads use strongly consistent reads.</param>
    /// <param name="allowEmptyStrings">Whether the AWS object-persistence model accepts empty string values.</param>
    /// <param name="retrieveDateTimeAsUtc">Whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</param>
    /// <param name="entryConversion">The immutable AWS SDK V1 or V2 entry-conversion rules, or <see langword="null"/> for V2.</param>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> or <paramref name="entryConversion"/> is invalid.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeToLive"/> is shorter than 30 seconds.</exception>
    public DynamoDbSagaRepositoryOptions(
        string tableName,
        TimeSpan? timeToLive,
        TimeProvider timeProvider,
        bool consistentRead = true,
        bool allowEmptyStrings = true,
        bool retrieveDateTimeAsUtc = true,
        DynamoDBEntryConversion? entryConversion = null)
    {
        if (!IsValidTableName(tableName))
            throw new ArgumentException(TableNameValidationMessage, nameof(tableName));
        if (timeToLive < TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeToLive),
                timeToLive,
                "Time to live must be at least 30 seconds when specified.");
        }

        entryConversion ??= DynamoDBEntryConversion.V2;
        if (!ReferenceEquals(entryConversion, DynamoDBEntryConversion.V1) &&
            !ReferenceEquals(entryConversion, DynamoDBEntryConversion.V2))
        {
            throw new ArgumentException(
                "Only the immutable DynamoDBEntryConversion.V1 and V2 instances are supported.",
                nameof(entryConversion));
        }

        TableName = tableName;
        TimeToLive = timeToLive;
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ConsistentRead = consistentRead;
        AllowEmptyStrings = allowEmptyStrings;
        RetrieveDateTimeAsUtc = retrieveDateTimeAsUtc;
        EntryConversion = entryConversion;
    }

    /// <summary>Gets the Amazon DynamoDB table that stores saga documents.</summary>
    public string TableName { get; }
    /// <summary>Gets the optional relative lifetime written to each persisted saga document.</summary>
    public TimeSpan? TimeToLive { get; }
    /// <summary>Gets the time source used to calculate document expiration.</summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>Gets whether saga loads use strongly consistent reads.</summary>
    public bool ConsistentRead { get; }
    /// <summary>Gets whether the AWS object-persistence model accepts empty string values.</summary>
    public bool AllowEmptyStrings { get; }
    /// <summary>Gets whether the AWS object-persistence model materializes <see cref="DateTime"/> values in UTC.</summary>
    public bool RetrieveDateTimeAsUtc { get; }
    /// <summary>Gets the AWS SDK entry-conversion rules used for persistence.</summary>
    public DynamoDBEntryConversion EntryConversion { get; }

    /// <summary>Formats a saga correlation identifier as the document partition key.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <returns>The canonical dashed GUID representation used as the partition key.</returns>
    internal static string FormatSagaKey(Guid correlationId) => correlationId.ToString("D");

    internal LoadConfig CreateLoadConfig()
    {
        return new LoadConfig
        {
            ConsistentRead = ConsistentRead,
            Conversion = EntryConversion,
            IsEmptyStringValueEnabled = AllowEmptyStrings,
            OverrideTableName = TableName,
            RetrieveDateTimeInUtc = RetrieveDateTimeAsUtc,
        };
    }

    internal GetTargetTableConfig CreateTargetTableConfig()
    {
        return new GetTargetTableConfig
        {
            Conversion = EntryConversion,
            IsEmptyStringValueEnabled = AllowEmptyStrings,
            OverrideTableName = TableName,
        };
    }

    internal DeleteConfig CreateDeleteConfig()
    {
        return new DeleteConfig
        {
            Conversion = EntryConversion,
            IsEmptyStringValueEnabled = AllowEmptyStrings,
            OverrideTableName = TableName,
        };
    }

    internal static bool IsValidTableName(string tableName)
    {
        if (tableName == null || tableName.Length is < 3 or > 255)
            return false;

        foreach (char character in tableName)
        {
            if ((character is >= 'A' and <= 'Z') || (character is >= 'a' and <= 'z') || (character is >= '0' and <= '9')
                || character is '_' or '-' or '.')
                continue;

            return false;
        }

        return true;
    }
}
