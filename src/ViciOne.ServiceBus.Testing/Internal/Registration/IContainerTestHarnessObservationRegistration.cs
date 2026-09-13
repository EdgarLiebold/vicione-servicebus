using System;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Identifies an observer registration that must be active before a container-owned bus starts.</summary>
internal interface IContainerTestHarnessObservationRegistration :
    IDisposable
{
}
