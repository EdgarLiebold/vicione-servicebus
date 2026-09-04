using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

public interface ITypeConverterCache
{
    bool TryGetTypeConverter<TProperty, TInput>([NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter);
}
