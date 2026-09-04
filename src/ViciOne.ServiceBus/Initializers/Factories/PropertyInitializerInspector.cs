using System.Reflection;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Provides a property initializer inspector implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class PropertyInitializerInspector<TMessage, TInput, TProperty> :
    IPropertyInitializerInspector<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly PropertyInfo _propertyInfo;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    public PropertyInitializerInspector(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    /// <param name="convention">The convention value.</param>
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
