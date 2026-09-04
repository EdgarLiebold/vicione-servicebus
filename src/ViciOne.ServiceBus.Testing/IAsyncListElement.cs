using System;

namespace ViciOne.ServiceBus.Testing;

public interface IAsyncListElement
{
    Guid? ElementId { get; }
}
