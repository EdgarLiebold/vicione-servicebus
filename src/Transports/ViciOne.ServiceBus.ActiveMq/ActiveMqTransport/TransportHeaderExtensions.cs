using System;
using System.Collections.Generic;
using System.Globalization;
using Apache.NMS;
using ViciOne.ServiceBus.Initializers.TypeConverters;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Copies supported service-bus headers into Apache NMS primitive properties.</summary>
public static class TransportHeaderExtensions
{
    static readonly DateTimeOffsetTypeConverter _dateTimeOffsetConverter = new DateTimeOffsetTypeConverter();

    /// <summary>Copies absent, non-null headers using provider-compatible primitive representations.</summary>
    /// <param name="dictionary">The native message-property map to populate.</param>
    /// <param name="headers">The send headers to copy.</param>
    public static void SetHeaders(this IPrimitiveMap dictionary, SendHeaders headers)
    {
        foreach (KeyValuePair<string, object> header in headers.GetAll())
        {
            if (header.Value == null)
            {
                if (dictionary.Contains(header.Key))
                    dictionary.Remove(header.Key);

                continue;
            }

            if (header.Key == MessageHeaders.TransportMessageId)
                continue;

            if (dictionary.Contains(header.Key))
                continue;

            SetHeaderValue(dictionary, header.Key, header.Value);

            if (header.Key == "AMQ_SCHEDULED_DELAY")
                headers.Set(header.Key, null);
        }
    }

    static void SetHeaderValue(IPrimitiveMap dictionary, string key, object value)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffset:
                _dateTimeOffsetConverter.TryConvert(dateTimeOffset, out long offsetMilliseconds);
                dictionary[key] = offsetMilliseconds;
                break;

            case DateTime dateTime:
                DateTimeOffset instant = dateTime.Kind switch
                {
                    DateTimeKind.Local => new DateTimeOffset(dateTime).ToUniversalTime(),
                    DateTimeKind.Utc => new DateTimeOffset(dateTime),
                    _ => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
                };
                _dateTimeOffsetConverter.TryConvert(instant, out long dateTimeMilliseconds);
                dictionary[key] = dateTimeMilliseconds;
                break;

            case Uri uri:
                dictionary[key] = uri.ToString();
                break;

            case string text:
                dictionary[key] = text;
                break;

            case bool boolean:
                dictionary[key] = boolean ? bool.TrueString : bool.FalseString;
                break;

            case byte or char or short or int or long or float or double:
                dictionary[key] = value;
                break;

            case IFormattable formattable:
                dictionary[key] = formattable.ToString(null, CultureInfo.InvariantCulture);
                break;
        }
    }
}
