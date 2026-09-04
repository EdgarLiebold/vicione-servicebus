using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface ExecutionResult
{
    Task Evaluate();

    bool IsFaulted(out Exception exception);
}
