using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Serialization;

public sealed class JobPropertySnapshotTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "coordinator-snapshots-replace-and-isolate-metadata")]
    public void Create_ReturnsAnIndependentCaseInsensitiveLastWriteWinsSnapshot()
    {
        var source = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Region"] = "west",
            ["region"] = "north",
        };

        Dictionary<string, object> copy = JobPropertySnapshot.Create(source);
        source["Region"] = "east";

        Assert.Single(copy);
        Assert.Equal("north", copy["REGION"]);
        Assert.Empty(JobPropertySnapshot.Create(null));
    }
}
