namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Marks a handler contract that is materialized by a capability-specific consumer kind instead of the core consumer kind.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class ConsumerRegistrationExclusionAttribute : Attribute;
