using System.Reflection;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Resolves the first convention that can populate one writable message property.</summary>
internal sealed class PropertyInitializerInspector<TMessage, TInput, TProperty> :
    IPropertyInitializerInspector<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly PropertyInfo _propertyInfo;

    public PropertyInitializerInspector(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
    }

    public bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(convention);

        if (builder.IsInputPropertyUsed(_propertyInfo.Name))
            return false;

        if (!WritePropertyCache<TMessage>.CanWrite(_propertyInfo.Name))
            return false;

        if (convention.TryGetPropertyInitializer<TMessage, TInput, TProperty>(_propertyInfo,
                out IPropertyInitializer<TMessage, TInput>? initializer))
        {
            builder.Add(_propertyInfo.Name, initializer);

            builder.SetInputPropertyUsed(_propertyInfo.Name);
            return true;
        }

        return false;
    }
}
