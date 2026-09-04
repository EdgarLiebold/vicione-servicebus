using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Defines the contract for async list element.
/// </summary>
public interface IAsyncListElement
{
    /// <summary>
    /// Gets the element id value.
    /// </summary>
    Guid? ElementId { get; }
}
