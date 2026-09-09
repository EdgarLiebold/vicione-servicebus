using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines a configuration component that can report validation results before runtime resources are created.
/// </summary>
public interface ISpecification
{
    /// <summary>Validates the configuration represented by this specification.</summary>
    /// <returns>All success, warning, and failure results produced by validation.</returns>
    IEnumerable<ValidationResult> Validate();
}
