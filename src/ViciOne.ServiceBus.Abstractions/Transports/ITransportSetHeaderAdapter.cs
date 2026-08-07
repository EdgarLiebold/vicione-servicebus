// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Collections.Generic;


    public interface ITransportSetHeaderAdapter<TValueType>
    {
        void Set(IDictionary<string, TValueType> dictionary, in HeaderValue headerValue);
        void Set<T>(IDictionary<string, TValueType> dictionary, in HeaderValue<T> headerValue);
    }
}
