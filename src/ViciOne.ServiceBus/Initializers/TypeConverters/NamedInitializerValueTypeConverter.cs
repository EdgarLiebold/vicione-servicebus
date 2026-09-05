using ViciOne.ServiceBus.Advanced.Initializers;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

sealed class NamedInitializerValueTypeConverter<TValue> :
    ITypeConverter<string, TValue>
    where TValue : class, INamedInitializerValue
{
    public bool TryConvert(TValue? input, out string? result)
    {
        result = input?.Name;
        return input != null;
    }
}
