using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskResultsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-CACHED", "completed-values")]
    public async Task CachedTasks_ExposeTheirDeclaredCompletedValuesAndStableIdentity()
    {
        Assert.Same(Task.CompletedTask, TaskResults.Completed);
        Assert.Same(TaskResults.True, TaskResults.True);
        Assert.Same(TaskResults.False, TaskResults.False);
        Assert.Same(TaskResults.Default<string>(), TaskResults.Default<string>());
        Assert.Same(TaskResults.Canceled<int>(), TaskResults.Canceled<int>());
        Assert.True(TaskResults.Completed.IsCompletedSuccessfully);
        Assert.True(await TaskResults.True);
        Assert.False(await TaskResults.False);
        Assert.Null(await TaskResults.Default<string>());
        Assert.Equal(0, await TaskResults.Default<int>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-FAULT", "exact-exception")]
    public async Task Faulted_PreservesTheExactException()
    {
        var expected = new ExpectedTaskResultException("expected fault");

        Task<int> task = TaskResults.Faulted<int>(expected);
        ExpectedTaskResultException actual =
            await Assert.ThrowsAsync<ExpectedTaskResultException>(() => task);

        Assert.Same(expected, actual);
        Assert.True(task.IsFaulted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-CANCELLATION", "cached-task")]
    public async Task Canceled_ExposesACanceledTaskWithACanceledToken()
    {
        Task<int> task = TaskResults.Canceled<int>();

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.True(exception.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-VALIDATION", "null-exception")]
    public void Faulted_RejectsANullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = TaskResults.Faulted<int>(null!);
        });

        Assert.Equal("exception", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-PRIMITIVES-PLATFORM", "no-desktop-windows-reference")]
    public void ProductAssembly_DoesNotReferenceDesktopWindowsFrameworks()
    {
        string[] prohibited =
        [
            "PresentationCore",
            "PresentationFramework",
            "System.Windows.Forms",
            "WindowsBase",
        ];
        string[] references = typeof(TaskResults).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToArray();

        Assert.DoesNotContain(references, reference =>
            prohibited.Contains(reference, StringComparer.OrdinalIgnoreCase));
    }

    private sealed class ExpectedTaskResultException(string message) : Exception(message);
}
