// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Saga;

public class PropertyExpressionPropertyValue<TProperty> :
    IPropertyExpressionPropertyValue
{
    public TProperty Value { get; set; }

    public object GetValue()
    {
        return Value;
    }
}
