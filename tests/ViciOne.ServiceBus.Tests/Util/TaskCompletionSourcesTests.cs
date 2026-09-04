using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskCompletionSourcesTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-COMPLETION-SOURCES", "asynchronous-continuations")]
    public void Create_CombinesRequestedOptionsWithAsynchronousContinuations()
    {
        const TaskCreationOptions requested = TaskCreationOptions.AttachedToParent;

        TaskCompletionSource<int> generic = TaskCompletionSources.Create<int>(requested);
        TaskCompletionSource nonGeneric = TaskCompletionSources.Create(requested);

        TaskCreationOptions expected = requested | TaskCreationOptions.RunContinuationsAsynchronously;
        Assert.Equal(expected, generic.Task.CreationOptions);
        Assert.Equal(expected, nonGeneric.Task.CreationOptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-COMPLETION-SOURCES", "set-completed")]
    public async Task SetCompleted_IsIdempotentAndRejectsANullSourceAsync()
    {
        TaskCompletionSource<bool> source = TaskCompletionSources.Create<bool>();

        source.SetCompleted();
        source.SetCompleted();

        Assert.True(await source.Task);
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            TaskCompletionSourceExtensions.SetCompleted(null!));
        Assert.Equal("source", exception.ParamName);
    }
}
