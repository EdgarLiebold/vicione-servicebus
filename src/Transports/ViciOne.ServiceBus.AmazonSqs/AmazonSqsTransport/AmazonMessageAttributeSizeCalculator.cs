using System;
using System.Collections.Generic;
using System.Text;
using SnsMessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using SqsMessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace ViciOne.ServiceBus.AmazonSqs;

static class AmazonMessageAttributeSizeCalculator
{
    static readonly Encoding _encoding = MessageDefaults.Encoding;

    public static int Calculate(IReadOnlyDictionary<string, SnsMessageAttributeValue>? attributes)
    {
        if (attributes == null)
            return 0;

        long length = 0;

        foreach ((string name, SnsMessageAttributeValue value) in attributes)
        {
            ArgumentNullException.ThrowIfNull(value);

            length += GetUtf8Length(name);
            length += GetUtf8Length(value.DataType);
            length += GetUtf8Length(value.StringValue);
            length += value.BinaryValue?.Length ?? 0;
        }

        return checked((int)length);
    }

    public static int Calculate(IReadOnlyDictionary<string, SqsMessageAttributeValue>? attributes)
    {
        if (attributes == null)
            return 0;

        long length = 0;

        foreach ((string name, SqsMessageAttributeValue value) in attributes)
        {
            ArgumentNullException.ThrowIfNull(value);

            length += GetUtf8Length(name);
            length += GetUtf8Length(value.DataType);
            length += GetUtf8Length(value.StringValue);
            length += value.BinaryValue?.Length ?? 0;

            foreach (string item in value.StringListValues ?? [])
                length += GetUtf8Length(item);
            foreach (var item in value.BinaryListValues ?? [])
                length += item.Length;
        }

        return checked((int)length);
    }

    static int GetUtf8Length(string? value) => string.IsNullOrEmpty(value) ? 0 : _encoding.GetByteCount(value);
}
