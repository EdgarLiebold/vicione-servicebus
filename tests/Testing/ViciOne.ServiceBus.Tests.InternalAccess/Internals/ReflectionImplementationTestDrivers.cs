using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Internals;

public sealed class DynamicImplementationBuilderTestDriver
{
    private readonly DynamicImplementationBuilder _builder = new();

    public Type GetImplementationType(Type interfaceType) => _builder.GetImplementationType(interfaceType);
}

public sealed class ReadPropertyTestDriver<T, TProperty>
    where T : class
{
    private readonly ReadProperty<T, TProperty> _property;

    public ReadPropertyTestDriver(PropertyInfo propertyInfo)
    {
        _property = new ReadProperty<T, TProperty>(propertyInfo);
    }

    public TProperty Get(T instance) => _property.Get(instance);
}

public sealed class WritePropertyTestDriver<T, TProperty>
    where T : class
{
    private readonly WriteProperty<T, TProperty> _property;

    public WritePropertyTestDriver(Type implementationType, PropertyInfo propertyInfo)
    {
        _property = new WriteProperty<T, TProperty>(implementationType, propertyInfo);
    }

    public Type TargetType => _property.TargetType;

    public void Set(T instance, TProperty value) => _property.Set(instance, value);
}
