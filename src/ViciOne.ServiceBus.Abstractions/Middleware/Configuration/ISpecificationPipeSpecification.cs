namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for specification pipe specification.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ISpecificationPipeSpecification<T> :
    ISpecification
    where T : class, PipeContext
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(ISpecificationPipeBuilder<T> builder);
}
