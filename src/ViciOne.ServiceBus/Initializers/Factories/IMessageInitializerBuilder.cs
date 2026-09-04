namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Defines the contract for message initializer builder.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IMessageInitializerBuilder<out TMessage, out TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="initializer">The initializer value.</param>
    void Add(string propertyName, IPropertyInitializer<TMessage> initializer);

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="initializer">The initializer value.</param>
    void Add(string propertyName, IPropertyInitializer<TMessage, TInput> initializer);

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    void Add(IHeaderInitializer<TMessage> initializer);

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    void Add(IHeaderInitializer<TMessage, TInput> initializer);

    /// <summary>
    /// Determines whether input property used.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsInputPropertyUsed(string propertyName);

    /// <summary>
    /// Sets input property used.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    void SetInputPropertyUsed(string propertyName);
}
