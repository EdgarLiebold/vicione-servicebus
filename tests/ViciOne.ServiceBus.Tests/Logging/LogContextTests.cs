using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Logging;

public sealed class LogContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-LOG-CONTEXT", "set-if-null-accepts-absent-candidate-and-preserves-current")]
    public void SetCurrentIfNull_AcceptsAnAbsentCandidateAndNeverReplacesAnExistingContext()
    {
        ILogContext? previous = LogContext.Current;
        var existing = new BusLogContext(NullLoggerFactory.Instance);
        var candidate = new BusLogContext(NullLoggerFactory.Instance);

        try
        {
            LogContext.Current = existing;

            LogContext.SetCurrentIfNull(null);
            LogContext.SetCurrentIfNull(candidate);

            Assert.Same(existing, LogContext.Current);

            LogContext.Current = null;
            LogContext.SetCurrentIfNull(null);
            Assert.Null(LogContext.Current);

            LogContext.SetCurrentIfNull(candidate);
            Assert.Same(candidate, LogContext.Current);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }
}
