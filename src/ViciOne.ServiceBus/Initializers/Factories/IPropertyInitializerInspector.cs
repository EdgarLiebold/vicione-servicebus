using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Defines the operations required by property initializer inspector.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IPropertyInitializerInspector<in TMessage, in TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    /// <param name="convention">The convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Apply(IMessageInitializerBuilder<TMessage, TInput> builder, IInitializerConvention convention);
}
