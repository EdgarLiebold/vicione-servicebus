namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    [ExcludeFromTopology]
    public interface OrderLine
    {
        Guid OrderId { get; }
        Guid OrderLineId { get; }
    }
}
