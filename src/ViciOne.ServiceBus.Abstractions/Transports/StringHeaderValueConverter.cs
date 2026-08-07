// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public class StringHeaderValueConverter :
        IHeaderValueConverter
    {
        public bool TryConvert(HeaderValue headerValue, out HeaderValue result)
        {
            if (headerValue.IsStringValue(out HeaderValue<string> stringValue))
            {
                result = stringValue;
                return true;
            }

            result = default;
            return false;
        }

        public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result)
        {
            if (headerValue.IsStringValue(out HeaderValue<string> stringValue))
            {
                result = stringValue;
                return true;
            }

            result = default;
            return false;
        }
    }
}
