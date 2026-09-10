using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Identifies an observation so live enumeration can suppress historical duplicates.</summary>
public interface IAsyncListElement
{
    /// <summary>Gets the stable observation identifier when the underlying context supplies one.</summary>
    Guid? ElementId { get; }
}
