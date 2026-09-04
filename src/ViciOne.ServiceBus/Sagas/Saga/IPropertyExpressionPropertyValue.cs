namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Defines the contract for property expression property value.
/// </summary>
public interface IPropertyExpressionPropertyValue
{
    /// <summary>
    /// Gets value.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    object? GetValue();
}
