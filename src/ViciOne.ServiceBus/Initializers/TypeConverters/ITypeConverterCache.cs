// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.TypeConverters
{
    public interface ITypeConverterCache
    {
        bool TryGetTypeConverter<TProperty, TInput>(out ITypeConverter<TProperty, TInput> typeConverter);
    }
}
