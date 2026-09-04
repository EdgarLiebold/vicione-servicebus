#nullable enable

namespace ViciOne.ServiceBus.ProviderAbstractions;

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Extension surface used by persistence and transport provider packages.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDurableSenderProviderConfigurator
{
    Type BusType { get; }

    IServiceCollection Services { get; }

    void UseStore(Type implementationType);

    void UseDispatcher(Type implementationType);
}
