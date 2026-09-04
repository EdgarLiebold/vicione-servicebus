using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface CompensationResult
{
    Task Evaluate();

    bool IsFailed(out Exception exception);
}
