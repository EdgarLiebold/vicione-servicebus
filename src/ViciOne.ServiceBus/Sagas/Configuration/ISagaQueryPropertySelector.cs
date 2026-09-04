namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for saga query property selector.
/// </summary>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public interface ISagaQueryPropertySelector<in TData, TProperty>
    where TData : class
{
    /// <summary>
    /// Attempts to get property.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="property">The property value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetProperty(ConsumeContext<TData> context, out TProperty property);
}
