using System;

namespace ViciOne.ServiceBus.Advanced;

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
internal sealed class MessageContractExclusionAttribute : Attribute;

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
internal sealed class ActivityContractAttribute : Attribute;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false)]
internal sealed class ActivityMessageAttribute : Attribute;
