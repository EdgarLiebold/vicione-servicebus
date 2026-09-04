namespace ViciOne.ServiceBus.Saga;

public class PropertyExpressionPropertyValue<TProperty> :
    IPropertyExpressionPropertyValue
{
    public TProperty Value { get; set; } = default!;
    public object? GetValue()
    {
        return Value;
    }
}
