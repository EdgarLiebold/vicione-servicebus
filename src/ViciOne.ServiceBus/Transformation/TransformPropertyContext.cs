namespace ViciOne.ServiceBus.Transformation;

/// <summary>Exposes a source-property value together with its message-level transform context.</summary>
/// <typeparam name="TProperty">The source-property type.</typeparam>
/// <typeparam name="TMessage">The source-message type.</typeparam>
public interface TransformPropertyContext<out TProperty, out TMessage> :
    TransformContext<TMessage>
    where TMessage : class
{
    /// <summary>Gets a value indicating whether the source property was evaluated.</summary>
    bool HasValue { get; }

    /// <summary>Gets the evaluated source-property value, which may be <see langword="null" /> when <see cref="HasValue" /> is true.</summary>
    TProperty? Value { get; }
}
