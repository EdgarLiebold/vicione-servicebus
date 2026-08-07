// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
{
    using System;


    public interface IAsyncListElement
    {
        Guid? ElementId { get; }
    }
}
