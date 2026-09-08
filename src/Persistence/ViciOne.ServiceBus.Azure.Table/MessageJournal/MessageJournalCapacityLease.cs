using System;
using Azure;
using Azure.Data.Tables;

namespace ViciOne.ServiceBus.Azure.Table.MessageJournal;

internal sealed class MessageJournalCapacityLease : ITableEntity
{
    public const string RowKeyValue = "control|capacity";

    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = RowKeyValue;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public long Generation { get; set; }
}
