namespace ViciOne.ServiceBus.Saga;

/// <summary>Encapsulates the property expression property value used by the service bus.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
public class PropertyExpressionPropertyValue<TProperty> :
    IPropertyExpressionPropertyValue
{
    /// <summary>Gets or sets the value.</summary>
    public TProperty Value { get; set; } = default!;
    /// <summary>Gets value.</summary>
    /// <returns>The value.</returns>
    public object? GetValue()
    {
        return Value;
    }
}
