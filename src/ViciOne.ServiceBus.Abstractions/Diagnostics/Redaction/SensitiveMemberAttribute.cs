using System;

namespace ViciOne.ServiceBus;

/// <summary>Marks one message member as sensitive for ServiceBus-owned diagnostic rendering.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveMemberAttribute : Attribute;
