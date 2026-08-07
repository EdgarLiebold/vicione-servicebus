// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public class SimpleHeaderValueConverter :
        IHeaderValueConverter
    {
        public bool TryConvert(HeaderValue headerValue, out HeaderValue result)
        {
            if (headerValue.IsSimpleValue(out var simpleValue))
            {
                result = simpleValue;
                return true;
            }

            result = default;
            return false;
        }

        public bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result)
        {
            if (headerValue.IsSimpleValue(out var simpleValue))
            {
                result = simpleValue;
                return true;
            }

            result = default;
            return false;
        }
    }
}
