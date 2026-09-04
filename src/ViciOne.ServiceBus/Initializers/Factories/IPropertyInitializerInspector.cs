using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>
/// Defines the contract for property initializer inspector.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IPropertyInitializerInspector<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention);
}
