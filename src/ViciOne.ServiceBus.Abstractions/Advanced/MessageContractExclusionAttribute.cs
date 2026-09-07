using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Excludes a generic infrastructure interface from message-contract discovery.</summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
internal sealed class MessageContractExclusionAttribute : Attribute;
