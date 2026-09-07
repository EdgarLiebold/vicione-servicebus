using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Marks a class or interface as a Courier activity transport message.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false)]
internal sealed class ActivityMessageAttribute : Attribute;
