using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Excludes the annotated consumer, saga, or activity from automatic receive-endpoint configuration.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ExcludeFromConfigureEndpointsAttribute :
    Attribute
{
}
