using System.Reflection;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Inspects property initializer metadata.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class PropertyInitializerInspector<TMessage, TInput, TProperty> :
    IPropertyInitializerInspector<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly PropertyInfo _propertyInfo;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyInfo">The property info.</param>
    public PropertyInitializerInspector(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention)
    {
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
