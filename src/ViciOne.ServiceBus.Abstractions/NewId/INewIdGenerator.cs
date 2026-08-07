// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface INewIdGenerator
    {
        NewId Next();

        ArraySegment<NewId> Next(NewId[] ids, int index, int count);

        Guid NextGuid();

        ArraySegment<Guid> NextGuid(Guid[] ids, int index, int count);

        ArraySegment<Guid> NextSequentialGuid(Guid[] ids, int index, int count);

        Guid NextSequentialGuid();
    }
}
