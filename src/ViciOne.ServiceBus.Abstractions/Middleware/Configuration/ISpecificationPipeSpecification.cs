namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for specification pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ISpecificationPipeSpecification<T> :
    ISpecification
    where T : class, PipeContext
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(ISpecificationPipeBuilder<T> builder);
}
