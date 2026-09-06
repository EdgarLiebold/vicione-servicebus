using Amazon.SQS.Model;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Converts allowed string headers to Amazon SQS message attributes.</summary>
public class SqsHeaderValueConverter :
    IHeaderValueConverter<MessageAttributeValue>
{
    readonly AllowTransportHeader _allowTransportHeader;

    /// <summary>Initializes an Amazon SQS header converter.</summary>
    /// <param name="allowTransportHeader">An optional predicate that filters string headers; all string headers are allowed by default.</param>
    public SqsHeaderValueConverter(AllowTransportHeader? allowTransportHeader = null)
    {
        _allowTransportHeader = allowTransportHeader ?? AlwaysCopy;
    }

    /// <summary>Tries to convert an allowed string header to an Amazon SQS <c>String</c> message attribute.</summary>
    /// <param name="headerValue">The transport header.</param>
    /// <param name="result">The converted Amazon SQS message attribute when successful.</param>
    /// <returns><see langword="true"/> when the header is a permitted string value; otherwise, <see langword="false"/>.</returns>
    public bool TryConvert(HeaderValue headerValue, out HeaderValue<MessageAttributeValue> result)
    {
        if (headerValue.IsStringValue(out HeaderValue<string> stringValue) && _allowTransportHeader(stringValue))
        {
            result = CreateMessageAttributeValue(stringValue);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Tries to convert a typed header containing an allowed string value to an Amazon SQS message attribute.</summary>
    /// <typeparam name="T">The declared header-value type.</typeparam>
    /// <param name="headerValue">The typed transport header.</param>
    /// <param name="result">The converted Amazon SQS message attribute when successful.</param>
    /// <returns><see langword="true"/> when the header contains a permitted string value; otherwise, <see langword="false"/>.</returns>
    public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue<MessageAttributeValue> result)
    {
        if (headerValue.IsStringValue(out HeaderValue<string> stringValue) && _allowTransportHeader(stringValue))
        {
            result = CreateMessageAttributeValue(stringValue);
            return true;
        }

        result = default;
        return false;
    }

    static HeaderValue<MessageAttributeValue> CreateMessageAttributeValue(HeaderValue<string> stringValue)
    {
        return new HeaderValue<MessageAttributeValue>(stringValue.Key, new MessageAttributeValue
        {
            StringValue = stringValue.Value,
            DataType = "String"
        });
    }

    static bool AlwaysCopy(HeaderValue<string> headerValue)
    {
        return true;
    }
}
