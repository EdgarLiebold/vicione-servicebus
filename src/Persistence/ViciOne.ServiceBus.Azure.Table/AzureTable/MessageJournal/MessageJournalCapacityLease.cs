#nullable enable
namespace ViciOne.ServiceBus.AzureTable.MessageJournal;

using System;
using Azure;
using Azure.Data.Tables;

internal sealed class MessageJournalCapacityLease : ITableEntity
{
    public const string RowKeyValue = "control|capacity";

    public string PartitionKey { get; set; } = "";

    public string RowKey { get; set; } = RowKeyValue;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public long Generation { get; set; }
}
