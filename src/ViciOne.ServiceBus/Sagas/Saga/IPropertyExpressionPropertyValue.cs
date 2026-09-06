namespace ViciOne.ServiceBus.Saga;

/// <summary>Provides access to the property expression property value used by the service bus.</summary>
public interface IPropertyExpressionPropertyValue
{
    /// <summary>Gets value.</summary>
    /// <returns>The value.</returns>
    object? GetValue();
}
