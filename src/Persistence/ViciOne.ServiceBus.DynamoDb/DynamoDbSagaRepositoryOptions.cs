using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>
/// Defines configuration options for dynamo db saga repository.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public sealed class DynamoDbSagaRepositoryOptions<TSaga>
    where TSaga : class, ISaga
{
    internal const string TableNameValidationMessage =
        "must contain 3 to 255 characters from A-Z, a-z, 0-9, underscore, hyphen, or period";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tableName">The table name value.</param>
    /// <param name="expiration">The expiration value.</param>
    public DynamoDbSagaRepositoryOptions(string tableName, TimeSpan? expiration = null)
        : this(tableName, expiration, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tableName">The table name value.</param>
    /// <param name="expiration">The expiration value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="consistentRead">The consistent read value.</param>
    /// <param name="isEmptyStringValueEnabled">The is empty string value enabled value.</param>
    /// <param name="retrieveDateTimeInUtc">The retrieve date time in utc value.</param>
    /// <param name="conversion">The conversion value.</param>
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

    /// <summary>
    /// Gets the table name value.
    /// </summary>
    public string TableName { get; }
    /// <summary>
    /// Gets the expiration value.
    /// </summary>
    public TimeSpan? Expiration { get; }
    /// <summary>
    /// Gets the time provider value.
    /// </summary>
    public TimeProvider TimeProvider { get; }
    /// <summary>
    /// Gets the consistent read value.
    /// </summary>
    public bool ConsistentRead { get; }
    /// <summary>
    /// Gets the is empty string value enabled value.
    /// </summary>
    public bool IsEmptyStringValueEnabled { get; }
    /// <summary>
    /// Gets the retrieve date time in utc value.
    /// </summary>
    public bool RetrieveDateTimeInUtc { get; }
    /// <summary>
    /// Gets the conversion value.
    /// </summary>
    public DynamoDBEntryConversion Conversion { get; }

    /// <summary>
    /// Performs the format saga key operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns>The result of the operation.</returns>
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
