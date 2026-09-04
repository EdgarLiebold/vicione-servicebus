using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

public interface MessageLockContext
{
    Task Complete();

    Task Abandon(Exception exception);
    Task DeadLetter();
    Task DeadLetter(Exception exception);
}
