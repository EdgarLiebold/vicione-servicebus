namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a property expression property value implementation.
/// </summary>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class PropertyExpressionPropertyValue<TProperty> :
    IPropertyExpressionPropertyValue
{
    /// <summary>
    /// Gets or sets the underlying value.
    /// </summary>
    public TProperty Value { get; set; } = default!;
    /// <summary>
    /// Gets value.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public object? GetValue()
    {
        return Value;
    }
}
