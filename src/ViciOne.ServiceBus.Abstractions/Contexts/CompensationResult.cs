using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface CompensationResult
{
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    bool IsFailed([NotNullWhen(true)] out Exception? exception);
}
