namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Builds message initializer components.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IMessageInitializerBuilder<out TMessage, out TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="initializer">The initializer.</param>
    void Add(string propertyName, IPropertyInitializer<TMessage> initializer);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="initializer">The initializer.</param>
    void Add(string propertyName, IPropertyInitializer<TMessage, TInput> initializer);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="initializer">The initializer.</param>
    void Add(IHeaderInitializer<TMessage> initializer);

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="initializer">The initializer.</param>
    void Add(IHeaderInitializer<TMessage, TInput> initializer);

    /// <summary>Determines whether input property used.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool IsInputPropertyUsed(string propertyName);

    /// <summary>Sets input property used.</summary>
    /// <param name="propertyName">The property name.</param>
    void SetInputPropertyUsed(string propertyName);
}
