namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by saga query property selector.</summary>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public interface ISagaQueryPropertySelector<in TData, TProperty>
    where TData : class
{
    /// <summary>Attempts to get property.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="property">Receives the property produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetProperty(ConsumeContext<TData> context, out TProperty property);
}
