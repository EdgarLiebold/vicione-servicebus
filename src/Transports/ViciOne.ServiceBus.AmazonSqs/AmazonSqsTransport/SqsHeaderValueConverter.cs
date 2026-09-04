using Amazon.SQS.Model;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a sqs header value converter implementation.
/// </summary>
public class SqsHeaderValueConverter :
    IHeaderValueConverter<MessageAttributeValue>
{
    readonly AllowTransportHeader _allowTransportHeader;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="allowTransportHeader">The allow transport header value.</param>
    public SqsHeaderValueConverter(AllowTransportHeader? allowTransportHeader = null)
    {
        _allowTransportHeader = allowTransportHeader ?? AlwaysCopy;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="headerValue">The header value value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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
