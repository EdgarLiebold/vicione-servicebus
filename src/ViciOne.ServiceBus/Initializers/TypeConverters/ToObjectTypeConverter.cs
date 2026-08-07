// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.TypeConverters
{
    public class ToObjectTypeConverter<T> :
        ITypeConverter<object, T>
    {
        public bool TryConvert(T input, out object result)
        {
            result = input;
            return true;
        }
    }
}
