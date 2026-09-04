namespace ViciOne.ServiceBus.Initializers.TypeConverters;

public interface ITypeConverterCache
{
    bool TryGetTypeConverter<TProperty, TInput>(out ITypeConverter<TProperty, TInput> typeConverter);
}
