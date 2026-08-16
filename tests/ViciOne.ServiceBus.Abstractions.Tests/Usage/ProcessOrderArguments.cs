namespace ViciOne.ServiceBus.Abstractions.Tests.Usage
{
    using System;


    public interface ProcessOrderArguments
    {
        Guid OrderId { get; }
    }
}
