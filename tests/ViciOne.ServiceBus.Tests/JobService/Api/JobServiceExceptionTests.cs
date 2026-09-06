using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class JobServiceExceptionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-EXCEPTIONS", "duplicate-job-preserves-identifier")]
    public void JobAlreadyExistsException_PreservesTheConflictingIdentifier()
    {
        Guid jobId = NewId.NextGuid();

        var exception = new JobAlreadyExistsException(jobId);

        Assert.Equal(jobId, exception.JobId);
        Assert.Contains(jobId.ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-EXCEPTIONS", "shutdown-rejection-preserves-identifier")]
    public void JobServiceStoppingException_PreservesTheRejectedIdentifier()
    {
        Guid jobId = NewId.NextGuid();

        var exception = new JobServiceStoppingException(jobId);

        Assert.Equal(jobId, exception.JobId);
        Assert.Contains(jobId.ToString(), exception.Message, StringComparison.Ordinal);
    }
}
