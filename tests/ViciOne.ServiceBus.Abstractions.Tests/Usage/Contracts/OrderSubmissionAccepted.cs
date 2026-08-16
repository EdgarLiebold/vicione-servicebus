namespace ViciOne.ServiceBus.Abstractions.Tests.Usage
{
    using System;


    public interface OrderSubmissionAccepted
    {
        Guid OrderId { get; }
    }
}
