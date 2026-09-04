using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Identifies service-bus infrastructure that adapts a registered message or request handler to
/// the consumer pipeline. Cross-cutting pipeline features use this internal semantic marker
/// instead of implementation type names.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class HandlerConsumerAdapterAttribute : Attribute
{
}
