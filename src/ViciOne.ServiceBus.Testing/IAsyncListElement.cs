using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by async list element.</summary>
public interface IAsyncListElement
{
    /// <summary>Gets the element id.</summary>
    Guid? ElementId { get; }
}
