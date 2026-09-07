using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Marks an interface as infrastructure owned by the Courier activity contract model.</summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
internal sealed class ActivityContractAttribute : Attribute;
