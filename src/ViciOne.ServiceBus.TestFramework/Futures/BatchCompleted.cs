// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Futures;

using System;
using System.Collections.Generic;


public interface BatchCompleted
{
    public Guid CorrelationId { get; }
    public IReadOnlyList<string> ProcessedJobsNumbers { get; }
}
