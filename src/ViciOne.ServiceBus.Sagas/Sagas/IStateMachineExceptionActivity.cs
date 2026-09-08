using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Exposes the exception type handled by a state-machine activity.</summary>
public interface IStateMachineExceptionActivity :
    IStateMachineActivity
{
    /// <summary>Gets the closed exception type handled by the activity.</summary>
    Type ExceptionType { get; }
}
