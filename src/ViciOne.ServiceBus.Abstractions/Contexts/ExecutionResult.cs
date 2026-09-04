using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface ExecutionResult
{
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    bool IsFaulted([NotNullWhen(true)] out Exception? exception);
}
