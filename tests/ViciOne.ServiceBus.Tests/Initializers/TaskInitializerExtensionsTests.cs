using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class TaskInitializerExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "reference")]
    public async Task SelectAsync_ProjectsAReferenceValueAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject { Name = "Frank" });

        string? result = await source.SelectAsync(subject => subject.Name, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Frank", result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "value")]
    public async Task SelectAsync_ProjectsAValueTypeAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject { Id = 27 });

        int result = await source.SelectAsync(subject => subject.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(27, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "nullable-value")]
    public async Task SelectAsync_ProjectsAPresentNullableValueAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject { MemberId = 27 });

        int? result = await source.SelectAsync(subject => subject.MemberId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(27, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "nullable-empty")]
    public async Task SelectAsync_PreservesAnEmptyNullableValueAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());

        int? result = await source.SelectAsync(subject => subject.MemberId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-VALUE", "reference")]
    public async Task SelectOrFallbackAsync_UsesAReferenceFallbackForAMissingSelectionAsync()
    {
        Task<Subject> missing = Task.FromResult(new Subject());
        Task<Subject> present = Task.FromResult(new Subject { Name = "Jane" });

        string fallback = await missing.SelectOrFallbackAsync(subject => subject.Name, "Frank", cancellationToken: TestContext.Current.CancellationToken);
        string selected = await present.SelectOrFallbackAsync(subject => subject.Name, "Frank", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Frank", fallback);
        Assert.Equal("Jane", selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-VALUE", "nullable-value")]
    public async Task SelectOrFallbackAsync_UsesAValueFallbackForAnEmptyNullableSelectionAsync()
    {
        Task<Subject> missing = Task.FromResult(new Subject());
        Task<Subject> present = Task.FromResult(new Subject { MemberId = 19 });

        int fallback = await missing.SelectOrFallbackAsync(subject => subject.MemberId, 27, cancellationToken: TestContext.Current.CancellationToken);
        int selected = await present.SelectOrFallbackAsync(subject => subject.MemberId, 27, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(27, fallback);
        Assert.Equal(19, selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-FACTORY", "reference")]
    public async Task SelectOrFallbackAsync_InvokesAReferenceFallbackFactoryWhenNeededAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var invocations = 0;

        string result = await source.SelectOrFallbackAsync(subject => subject.Name, () =>
            {
                invocations++;
                return "Frank";
            }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Frank", result);
        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-FACTORY", "nullable-value")]
    public async Task SelectOrFallbackAsync_InvokesAValueFallbackFactoryWhenNeededAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var invocations = 0;

        int result = await source.SelectOrFallbackAsync(subject => subject.MemberId, () =>
            {
                invocations++;
                return 27;
            }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(27, result);
        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-ASYNC-FALLBACK", "reference")]
    public async Task SelectOrFallbackAsync_AwaitsAReferenceFallbackTaskAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var fallbackStarted = NewCompletionSource();
        var releaseFallback = NewCompletionSource<string>();

        Task<string> result = source.SelectOrFallbackAsync(subject => subject.Name, () =>
            {
                fallbackStarted.TrySetResult();
                return releaseFallback.Task;
            }, cancellationToken: TestContext.Current.CancellationToken);

        await fallbackStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        try
        {
            Assert.False(result.IsCompleted);
        }
        finally
        {
            releaseFallback.TrySetResult("Frank");
        }

        Assert.Equal("Frank", await result.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-ASYNC-FALLBACK", "nullable-value")]
    public async Task SelectOrFallbackAsync_AwaitsAValueFallbackTaskAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var fallbackStarted = NewCompletionSource();
        var releaseFallback = NewCompletionSource<int>();

        Task<int> result = source.SelectOrFallbackAsync(subject => subject.MemberId, () =>
            {
                fallbackStarted.TrySetResult();
                return releaseFallback.Task;
            }, cancellationToken: TestContext.Current.CancellationToken);

        await fallbackStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        try
        {
            Assert.False(result.IsCompleted);
        }
        finally
        {
            releaseFallback.TrySetResult(27);
        }

        Assert.Equal(27, await result.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK", "lazy")]
    public async Task SelectOrFallbackAsync_DoesNotInvokeFactoriesForAPresentSelectionAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject { MemberId = 19, Name = "Jane" });
        var referenceSynchronousInvocations = 0;
        var referenceAsynchronousInvocations = 0;
        var valueSynchronousInvocations = 0;
        var valueAsynchronousInvocations = 0;

        string referenceSynchronous = await source.SelectOrFallbackAsync(subject => subject.Name, () =>
            {
                referenceSynchronousInvocations++;
                return "fallback";
            }, cancellationToken: TestContext.Current.CancellationToken);
        string referenceAsynchronous = await source.SelectOrFallbackAsync(subject => subject.Name, () =>
            {
                referenceAsynchronousInvocations++;
                return Task.FromResult("fallback");
            }, cancellationToken: TestContext.Current.CancellationToken);
        int valueSynchronous = await source.SelectOrFallbackAsync(subject => subject.MemberId, () =>
            {
                valueSynchronousInvocations++;
                return 27;
            }, cancellationToken: TestContext.Current.CancellationToken);
        int valueAsynchronous = await source.SelectOrFallbackAsync(subject => subject.MemberId, () =>
            {
                valueAsynchronousInvocations++;
                return Task.FromResult(27);
            }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Jane", referenceSynchronous);
        Assert.Equal("Jane", referenceAsynchronous);
        Assert.Equal(19, valueSynchronous);
        Assert.Equal(19, valueAsynchronous);
        Assert.Equal(0, referenceSynchronousInvocations);
        Assert.Equal(0, referenceAsynchronousInvocations);
        Assert.Equal(0, valueSynchronousInvocations);
        Assert.Equal(0, valueAsynchronousInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-SOURCE", "null-value")]
    public async Task NullSourceValue_BypassesTheSelectorAndUsesTheDeclaredFallbackAsync()
    {
        Task<Subject?> source = Task.FromResult<Subject?>(null);
        var selectorInvocations = 0;

        string? projected = await source.SelectAsync(subject =>
        {
            selectorInvocations++;
            return subject?.Name;
        }, cancellationToken: TestContext.Current.CancellationToken);
        string fallback = await source.SelectOrFallbackAsync(subject =>
            {
                selectorInvocations++;
                return subject?.Name;
            }, "Frank", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(projected);
        Assert.Equal("Frank", fallback);
        Assert.Equal(0, selectorInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-SOURCE", "fault-and-cancellation")]
    public async Task SourceTaskState_IsPropagatedWithoutWrappingOrTokenSubstitutionAsync()
    {
        var expected = new ExpectedInitializerException("expected fault");
        Task<Subject> faulted = Task.FromException<Subject>(expected);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<Subject> canceled = Task.FromCanceled<Subject>(cancellation.Token);

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            faulted.SelectAsync(subject => subject.Name, cancellationToken: TestContext.Current.CancellationToken));
        OperationCanceledException canceledException =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                canceled.SelectAsync(subject => subject.Name, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-SOURCE", "caller-cancellation")]
    public async Task PendingSource_ObservesCallerCancellationAsync()
    {
        var source = NewCompletionSource<Subject>();
        using var cancellation = new CancellationTokenSource();

        Task<string?> result = source.Task.SelectAsync(subject => subject.Name, cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.WaitAsync(OperationTimeout, TestCancellationToken));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(source.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-ASYNC-FALLBACK", "caller-cancellation")]
    public async Task PendingFallback_ObservesCallerCancellationAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var fallback = NewCompletionSource<string>();
        using var cancellation = new CancellationTokenSource();

        Task<string> result = source.SelectOrFallbackAsync(
            subject => subject.Name,
            () => fallback.Task,
            cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.WaitAsync(OperationTimeout, TestCancellationToken));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.False(fallback.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-DELEGATE-STATE", "fault-and-cancellation")]
    public async Task SelectorAndFallbackState_IsPropagatedWithoutWrappingOrTokenSubstitutionAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var selectorException = new ExpectedInitializerException("selector fault");
        var synchronousFallbackException = new ExpectedInitializerException("synchronous fallback fault");
        var asynchronousFallbackException = new ExpectedInitializerException("asynchronous fallback fault");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        ExpectedInitializerException actualSelectorException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectAsync<Subject, string>(_ => throw selectorException, cancellationToken: TestContext.Current.CancellationToken));
        ExpectedInitializerException actualSynchronousFallbackException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectOrFallbackAsync(subject => subject.Name, (Func<string>)(() => throw synchronousFallbackException), cancellationToken: TestContext.Current.CancellationToken));
        ExpectedInitializerException actualAsynchronousFallbackException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectOrFallbackAsync(subject => subject.Name, () => Task.FromException<string>(asynchronousFallbackException), cancellationToken: TestContext.Current.CancellationToken));
        OperationCanceledException actualCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                source.SelectOrFallbackAsync(subject => subject.Name, () => Task.FromCanceled<string>(cancellation.Token), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Same(selectorException, actualSelectorException);
        Assert.Same(synchronousFallbackException, actualSynchronousFallbackException);
        Assert.Same(asynchronousFallbackException, actualAsynchronousFallbackException);
        Assert.Equal(cancellation.Token, actualCancellation.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-VALIDATION", "invalid-inputs")]
    public async Task PublicMethods_RejectInvalidInputsAtTheirBoundaryAsync()
    {
        Task<Subject> source = Task.FromResult(new Subject());

        ArgumentNullException nullSource = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskInitializerExtensions.SelectAsync<Subject, string>(null!, subject => subject.Name, TestContext.Current.CancellationToken));
        ArgumentNullException nullSelector = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectAsync<Subject, string>(null!, cancellationToken: TestContext.Current.CancellationToken));
        ArgumentNullException nullFallback = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (string)null!, cancellationToken: TestContext.Current.CancellationToken));
        ArgumentNullException nullSyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (Func<string>)null!, cancellationToken: TestContext.Current.CancellationToken));
        ArgumentNullException nullValueSyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, (Func<int>)null!, cancellationToken: TestContext.Current.CancellationToken));
        ArgumentNullException nullAsyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (Func<Task<string>>)null!, cancellationToken: TestContext.Current.CancellationToken));
        ArgumentNullException nullValueAsyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, (Func<Task<int>>)null!, cancellationToken: TestContext.Current.CancellationToken));
        InvalidOperationException nullFallbackValue = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => (string)null!, cancellationToken: TestContext.Current.CancellationToken));
        InvalidOperationException nullFallbackTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => (Task<string>)null!, cancellationToken: TestContext.Current.CancellationToken));
        InvalidOperationException nullValueFallbackTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, () => (Task<int>)null!, cancellationToken: TestContext.Current.CancellationToken));
        InvalidOperationException nullFallbackTaskResult = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => Task.FromResult<string>(null!), cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("source", nullSource.ParamName);
        Assert.Equal("selector", nullSelector.ParamName);
        Assert.Equal("fallback", nullFallback.ParamName);
        Assert.Equal("fallbackFactory", nullSyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullValueSyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullAsyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullValueAsyncFactory.ParamName);
        Assert.Equal("The fallbackFactory must return a value.", nullFallbackValue.Message);
        Assert.Equal("The fallbackFactory must return a Task.", nullFallbackTask.Message);
        Assert.Equal("The fallbackFactory must return a Task.", nullValueFallbackTask.Message);
        Assert.Equal("The fallbackFactory task must produce a value.", nullFallbackTaskResult.Message);
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class Subject
    {
        public int Id { get; init; }

        public int? MemberId { get; init; }

        public string? Name { get; init; }
    }

    private sealed class ExpectedInitializerException(string message) : Exception(message);
}
