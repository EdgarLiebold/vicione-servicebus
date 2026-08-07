// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public interface IHeaderValueConverter
    {
        bool TryConvert(HeaderValue headerValue, out HeaderValue result);
        bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue result);
    }


    public interface IHeaderValueConverter<TValueType>
    {
        bool TryConvert(HeaderValue headerValue, out HeaderValue<TValueType> result);
        bool TryConvert<T>(HeaderValue<T> headerValue, out HeaderValue<TValueType> result);
    }
}
