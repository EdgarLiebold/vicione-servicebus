using System.Reflection;
using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Provides a header initializer inspector implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class HeaderInitializerInspector<TMessage, TInput, TProperty> :
    IHeaderInitializerInspector<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly PropertyInfo _propertyInfo;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    public HeaderInitializerInspector(PropertyInfo propertyInfo)
    {
        _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
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

        if (convention.TryGetHeaderInitializer<TMessage, TInput, TProperty>(_propertyInfo,
                out IHeaderInitializer<TMessage, TInput>? initializer))
        {
            builder.Add(initializer);

            builder.SetInputPropertyUsed(_propertyInfo.Name);
            return true;
        }

        return false;
    }
}
