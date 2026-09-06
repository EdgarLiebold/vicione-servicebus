using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// A specification, that can be validated as part of a configurator, is used
/// to allow nesting and chaining of specifications while ensuring that all aspects
/// of the configuration are verified correct.
/// </summary>
public interface ISpecification
{
    /// <summary>Validate the specification, ensuring that a successful build will occur.</summary>
    /// <returns>The validation failures.</returns>
    IEnumerable<ValidationResult> Validate();
}
