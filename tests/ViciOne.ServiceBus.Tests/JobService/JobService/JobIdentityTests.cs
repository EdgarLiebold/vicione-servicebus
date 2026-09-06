using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobIdentityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-IDENTITY", "sha256-golden-vector-is-platform-independent")]
    public void DeterministicIdentity_MatchesTheSha256GoldenVector()
    {
        Guid actual = JobIdentity.CreateDeterministicId("ViciOne.Job.Identity.Contract");

        Assert.Equal(new Guid("760c9b52-8751-a488-7035-4a755b605360"), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-IDENTITY", "same-name-is-stable-and-different-name-is-distinct")]
    public void DeterministicIdentity_IsStableAndNameSensitive()
    {
        Guid first = JobIdentity.CreateDeterministicId("invoice-import");
        Guid repeated = JobIdentity.CreateDeterministicId("invoice-import");
        Guid different = JobIdentity.CreateDeterministicId("invoice-export");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, different);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-IDENTITY", "required-identity-inputs-are-rejected")]
    public void IdentityFactories_RejectMissingNames()
    {
        Assert.Equal(
            "key",
            Assert.Throws<ArgumentNullException>(() => JobIdentity.CreateDeterministicId(null!)).ParamName);
        Assert.Equal(
            "jobName",
            Assert.Throws<ArgumentNullException>(() => RecurringJobIdentity<TestJob>.CreateId(null!)).ParamName);
        Assert.Equal(
            "jobName",
            Assert.Throws<ArgumentException>(() => RecurringJobIdentity<TestJob>.CreateName(" ")).ParamName);
    }

    private sealed record TestJob;
}
