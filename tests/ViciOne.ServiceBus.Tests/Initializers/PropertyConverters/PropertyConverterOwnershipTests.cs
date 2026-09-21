using System.Collections;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class PropertyConverterOwnershipTests
{
    public static IEnumerable<object[]> NestedFormsAndOutcomes()
    {
        for (var form = 0; form < 2; form++)
            for (var outcome = 0; outcome < 3; outcome++)
                yield return [form, outcome];
    }

    public static IEnumerable<object[]> SingleCollectionFormsAndOutcomes()
    {
        for (var form = 0; form < 14; form++)
            for (var outcome = 0; outcome < 3; outcome++)
                yield return [form, outcome];
    }

    public static IEnumerable<object[]> CombinedShapesAndOutcomes()
    {
        for (var shape = 0; shape < 4; shape++)
            for (var outcome = 0; outcome < 3; outcome++)
                yield return [shape, outcome];
    }

    public static IEnumerable<object[]> KeyPriorityOutcomes()
    {
        yield return [1, 0];
        yield return [1, 1];
        yield return [2, 1];
        yield return [2, 2];
    }

    public static IEnumerable<object[]> ValueInvocationOutcomes()
    {
        foreach (bool nullTask in new[] { false, true })
            for (var keyOutcome = 0; keyOutcome < 3; keyOutcome++)
                yield return [nullTask, keyOutcome];
    }

    [Theory]
    [MemberData(nameof(NestedFormsAndOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTER-LIFETIME", "typed-and-runtime-nested-initializer-ownership")]
    public async Task NestedConverters_OwnAcceptedInitializerAndOriginalOutcomeAsync(int form, int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var source = new TaskCompletionSource<InitializeContext<NestedMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initializer = new ControlledInitializer(source);
        var input = new NestedInput("owned");
        Type? resolvedType = null;
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<NestedMessage?> root = form switch
        {
            0 => ((IPropertyConverter<NestedMessage, NestedInput>)new InitializePropertyConverter<NestedMessage, NestedInput>(initializer))
                .ConvertAsync(context, input, caller.Token),
            1 => ((IPropertyConverter<NestedMessage, object>)new InitializePropertyConverter<NestedMessage>(type =>
                {
                    resolvedType = type;
                    return initializer;
                }))
                .ConvertAsync(context, input, caller.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(form))
        };
        var expected = new InvalidOperationException("nested initializer failed");
        try
        {
            Assert.Same(input, initializer.Input);
            Assert.Equal(caller.Token, initializer.Token);
            Assert.NotNull(initializer.Context);
            Assert.NotSame(context, initializer.Context);
            if (form == 1)
                Assert.Equal(typeof(NestedInput), resolvedType);
            Assert.False(root.IsCompleted);
            caller.Cancel();
            await AssertPendingAsync(root);
            InitializeContext<NestedMessage> nestedContext = initializer.Context;
            nestedContext.Message.Value = input.Value;
            Complete(source, outcome, nestedContext, expected, producerCancellation.Token);
            NestedMessage? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
            {
                Assert.Same(initializer.Context.Message, result);
                Assert.Equal("owned", result!.Value);
            }
        }
        finally
        {
            source.TrySetResult(initializer.Context ?? CreateNestedContext());
            await ObserveAsync(source.Task);
            await ObserveAsync(root);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-CONVERTER-LIFETIME", "nested-injected-dependency-guards")]
    public async Task NestedConverters_RejectMissingInjectedDependenciesAndResolverResultsAsync()
    {
        Assert.Equal("initializer", Assert.Throws<ArgumentNullException>(() =>
            new InitializePropertyConverter<NestedMessage, NestedInput>(null!)).ParamName);
        Assert.Equal("initializerResolver", Assert.Throws<ArgumentNullException>(() =>
            new InitializePropertyConverter<NestedMessage>(null!)).ParamName);
        var converter = (IPropertyConverter<NestedMessage, object>)new InitializePropertyConverter<NestedMessage>(_ => null!);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            converter.ConvertAsync(CreateContext(), new NestedInput("value"), TestContext.Current.CancellationToken));

        Assert.Equal("The nested message initializer resolver returned null.", exception.Message);

        var source = new TaskCompletionSource<InitializeContext<NestedMessage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var nullTaskInitializer = new ControlledInitializer(source) { ReturnNull = true };
        var typed = (IPropertyConverter<NestedMessage, NestedInput>)
            new InitializePropertyConverter<NestedMessage, NestedInput>(nullTaskInitializer);
        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            typed.ConvertAsync(CreateContext(), new NestedInput("value"), TestContext.Current.CancellationToken));
        Assert.Equal("The nested message initializer returned null.", nullTask.Message);
    }

    [Theory]
    [MemberData(nameof(SingleCollectionFormsAndOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-LIFETIME", "all-single-conversion-shapes-own-task-and-enumerator")]
    public async Task SingleConversionCollections_OwnAcceptedTaskAndEnumeratorAsync(int form, int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var source = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var elementConverter = new ControlledConverter<long, int>(_ => source.Task);
        var values = new TrackingEnumerable<int>([2]);
        var entries = new TrackingEnumerable<KeyValuePair<int, int>>([new(1, 2)]);
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<object?> root = StartSingleCollectionConversionAsync(form, elementConverter, values, entries, context, caller.Token);
        var expected = new InvalidOperationException("element conversion failed");
        TrackingState tracking = form < 6 ? values.State : entries.State;
        try
        {
            Assert.Equal(1, elementConverter.Calls);
            Assert.Equal(form < 10 ? 2 : 1, elementConverter.Inputs.Single());
            Assert.Equal(caller.Token, elementConverter.Tokens.Single());
            Assert.Same(context, elementConverter.Contexts.Single());
            Assert.Equal(0, tracking.DisposeCount);
            caller.Cancel();
            await AssertPendingAsync(root);
            Assert.Equal(0, tracking.DisposeCount);
            Complete(source, outcome, 10, expected, producerCancellation.Token);
            object? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
                AssertSingleCollectionResult(form, result);
            Assert.Equal(1, tracking.DisposeCount);
        }
        finally
        {
            source.TrySetResult(10);
            await ObserveAsync(source.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(CombinedShapesAndOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-LIFETIME", "combined-dictionary-shapes-own-value-outcome")]
    public async Task CombinedDictionaryShapes_OwnAcceptedValueOutcomeAsync(int shape, int outcome)
    {
        using var caller = new CancellationTokenSource();
        using var producerCancellation = new CancellationTokenSource();
        producerCancellation.Cancel();
        var keySource = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        keySource.SetResult(10);
        var valueSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var keyConverter = new ControlledConverter<long, int>(_ => keySource.Task);
        var valueConverter = new ControlledConverter<string, int>(_ => valueSource.Task);
        var entries = new TrackingEnumerable<KeyValuePair<int, int>>([new(1, 2)]);
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<object?> root = StartCombinedDictionaryConversionAsync(shape, keyConverter, valueConverter, entries, context, caller.Token);
        var expected = new InvalidOperationException("value conversion failed");
        try
        {
            Assert.Equal(1, keyConverter.Calls);
            Assert.Equal(1, valueConverter.Calls);
            Assert.Equal(caller.Token, keyConverter.Tokens.Single());
            Assert.Equal(caller.Token, valueConverter.Tokens.Single());
            Assert.Same(context, keyConverter.Contexts.Single());
            Assert.Same(context, valueConverter.Contexts.Single());
            Assert.Equal(0, entries.State.DisposeCount);
            caller.Cancel();
            await AssertPendingAsync(root);
            Complete(valueSource, outcome, "ten", expected, producerCancellation.Token);
            object? result = await AssertOutcomeAsync(root, outcome, expected, producerCancellation.Token);
            if (outcome == 0)
                AssertCombinedDictionaryResult(result);
            Assert.Equal(1, entries.State.DisposeCount);
        }
        finally
        {
            valueSource.TrySetResult("ten");
            await ObserveAsync(keySource.Task);
            await ObserveAsync(valueSource.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(KeyPriorityOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-LIFETIME", "combined-dictionary-drains-siblings-with-key-priority")]
    public async Task CombinedDictionary_OwnsBothAcceptedTasksWithKeyFirstPriorityAsync(int keyOutcome, int valueOutcome)
    {
        using var caller = new CancellationTokenSource();
        using var keyCancellation = new CancellationTokenSource();
        using var valueCancellation = new CancellationTokenSource();
        keyCancellation.Cancel();
        valueCancellation.Cancel();
        var keySource = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var keyConverter = new ControlledConverter<long, int>(_ => keySource.Task);
        var valueConverter = new ControlledConverter<string, int>(_ => valueSource.Task);
        var entries = new TrackingEnumerable<KeyValuePair<int, int>>([new(1, 2)]);
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<object?> root = StartCombinedDictionaryConversionAsync(0, keyConverter, valueConverter, entries, context, caller.Token);
        var keyFailure = new InvalidOperationException("key conversion failed");
        var valueFailure = new InvalidOperationException("value conversion failed");
        try
        {
            Assert.Equal(1, keyConverter.Calls);
            Assert.Equal(1, valueConverter.Calls);
            Assert.Same(context, keyConverter.Contexts.Single());
            Assert.Same(context, valueConverter.Contexts.Single());
            caller.Cancel();
            await AssertPendingAsync(root);
            Complete(keySource, keyOutcome, 10, keyFailure, keyCancellation.Token);
            await AssertPendingAsync(root);
            Assert.Equal(0, entries.State.DisposeCount);
            Complete(valueSource, valueOutcome, "ten", valueFailure, valueCancellation.Token);
            await AssertOutcomeAsync(root, keyOutcome, keyFailure, keyCancellation.Token);
            Assert.True(valueSource.Task.IsCompleted);
            Assert.Equal(1, entries.State.DisposeCount);
        }
        finally
        {
            keySource.TrySetResult(10);
            valueSource.TrySetResult("ten");
            await ObserveAsync(keySource.Task);
            await ObserveAsync(valueSource.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [MemberData(nameof(ValueInvocationOutcomes))]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-LIFETIME", "combined-dictionary-value-invocation-cannot-abandon-key")]
    public async Task CombinedDictionary_ValueInvocationFailureStillOwnsAcceptedKeyAsync(bool nullTask, int keyOutcome)
    {
        using var keyCancellation = new CancellationTokenSource();
        keyCancellation.Cancel();
        var keySource = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var keyConverter = new ControlledConverter<long, int>(_ => keySource.Task);
        var valueFailure = new InvalidOperationException("value invocation failed");
        var keyFailure = new InvalidOperationException("key conversion failed");
        IPropertyConverter<string, int> valueConverter = nullTask
            ? new NullTaskConverter<string, int>()
            : new ThrowingConverter<string, int>(valueFailure);
        var entries = new TrackingEnumerable<KeyValuePair<int, int>>([new(1, 2)]);
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<object?>? root = null;
        try
        {
            Exception? synchronous = null;
            try
            {
                root = StartCombinedDictionaryConversionAsync(
                    0, keyConverter, valueConverter, entries, context, TestContext.Current.CancellationToken);
            }
            catch (Exception exception)
            {
                synchronous = exception;
            }
            Assert.Null(synchronous);
            Assert.NotNull(root);
            Task<object?> ownedRoot = root;
            Assert.Equal(1, keyConverter.Calls);
            Assert.Same(context, keyConverter.Contexts.Single());
            await AssertPendingAsync(ownedRoot);
            Assert.Equal(0, entries.State.DisposeCount);
            Complete(keySource, keyOutcome, 10, keyFailure, keyCancellation.Token);
            if (keyOutcome == 0)
            {
                InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => ownedRoot);
                if (nullTask)
                    Assert.Equal("The dictionary element converter returned a null task.", actual.Message);
                else
                    Assert.Same(valueFailure, actual);
            }
            else
                await AssertOutcomeAsync(ownedRoot, keyOutcome, keyFailure, keyCancellation.Token);
            Assert.True(keySource.Task.IsCompleted);
            Assert.Equal(1, entries.State.DisposeCount);
        }
        finally
        {
            keySource.TrySetResult(10);
            await ObserveAsync(keySource.Task);
            await ObserveAsync(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-LIFETIME", "caller-cancellation-stops-before-next-conversion")]
    public async Task CollectionConverters_StopBeforeStartingAnotherConversionAsync(int form)
    {
        using var caller = new CancellationTokenSource();
        var elementSource = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var elementConverter = new ControlledConverter<long, int>(_ => elementSource.Task);
        var valueConverter = new ControlledConverter<string, int>(_ => valueSource.Task);
        var values = new TrackingEnumerable<int>([1, 2]);
        var entries = new TrackingEnumerable<KeyValuePair<int, int>>([new(1, 1), new(2, 2)]);
        InitializeContext<OwnerMessage> context = CreateContext();
        Task<object?> root = StartTwoEntryConversionAsync(form, elementConverter, valueConverter, values, entries, context, caller.Token);
        TrackingState tracking = form < 2 ? values.State : entries.State;
        try
        {
            Assert.Equal(1, elementConverter.Calls);
            Assert.Equal(form == 4 ? 1 : 0, valueConverter.Calls);
            Assert.Same(context, elementConverter.Contexts.Single());
            if (form == 4)
                Assert.Same(context, valueConverter.Contexts.Single());
            caller.Cancel();
            await AssertPendingAsync(root);
            elementSource.SetResult(10);
            if (form == 4)
                valueSource.SetResult("ten");
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => root);
            Assert.Equal(caller.Token, actual.CancellationToken);
            Assert.Equal(1, elementConverter.Calls);
            Assert.Equal(form == 4 ? 1 : 0, valueConverter.Calls);
            Assert.Equal(1, tracking.DisposeCount);
        }
        finally
        {
            elementSource.TrySetResult(10);
            valueSource.TrySetResult("ten");
            await ObserveAsync(elementSource.Task);
            await ObserveAsync(valueSource.Task);
            await ObserveAsync(root);
        }
    }

    static Task<object?> StartSingleCollectionConversionAsync(
        int form,
        IPropertyConverter<long, int> elementConverter,
        IEnumerable<int> values,
        IEnumerable<KeyValuePair<int, int>> entries,
        InitializeContext<OwnerMessage> context,
        CancellationToken cancellationToken)
    {
        if (form == 0)
            return BoxAsync(new ArrayPropertyConverter<long, int>(elementConverter).ConvertAsync(context, values, cancellationToken));

        if (form < 6)
        {
            var converter = new ListPropertyConverter<long, int>(elementConverter);
            return form switch
            {
                1 => BoxAsync(((IPropertyConverter<List<long>, IEnumerable<int>>)converter).ConvertAsync(context, values, cancellationToken)),
                2 => BoxAsync(((IPropertyConverter<IList<long>, IEnumerable<int>>)converter).ConvertAsync(context, values, cancellationToken)),
                3 => BoxAsync(((IPropertyConverter<ICollection<long>, IEnumerable<int>>)converter).ConvertAsync(context, values, cancellationToken)),
                4 => BoxAsync(((IPropertyConverter<IReadOnlyList<long>, IEnumerable<int>>)converter).ConvertAsync(context, values, cancellationToken)),
                5 => BoxAsync(((IPropertyConverter<IEnumerable<long>, IEnumerable<int>>)converter).ConvertAsync(context, values, cancellationToken)),
                _ => throw new ArgumentOutOfRangeException(nameof(form))
            };
        }

        if (form < 10)
        {
            var converter = new DictionaryPropertyConverter<int, long, int>(elementConverter);
            return StartDictionaryShapeAsync<int, long>(form - 6, converter, context, entries, cancellationToken);
        }

        return StartDictionaryShapeAsync<long, int>(form - 10,
            new DictionaryKeyPropertyConverter<long, int, int>(elementConverter), context, entries, cancellationToken);
    }

    static Task<object?> StartCombinedDictionaryConversionAsync(
        int shape,
        IPropertyConverter<long, int> keyConverter,
        IPropertyConverter<string, int> valueConverter,
        IEnumerable<KeyValuePair<int, int>> entries,
        InitializeContext<OwnerMessage> context,
        CancellationToken cancellationToken)
    {
        var converter = new DictionaryPropertyConverter<long, string, int, int>(keyConverter, valueConverter);
        return StartDictionaryShapeAsync<long, string>(shape, converter, context, entries, cancellationToken);
    }

    static Task<object?> StartDictionaryShapeAsync<TKey, TElement>(
        int shape,
        object converter,
        InitializeContext<OwnerMessage> context,
        IEnumerable<KeyValuePair<int, int>> entries,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        return shape switch
        {
            0 => BoxAsync(((IPropertyConverter<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<int, int>>>)converter)
                .ConvertAsync(context, entries, cancellationToken)),
            1 => BoxAsync(((IPropertyConverter<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<int, int>>>)converter)
                .ConvertAsync(context, entries, cancellationToken)),
            2 => BoxAsync(((IPropertyConverter<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<int, int>>>)converter)
                .ConvertAsync(context, entries, cancellationToken)),
            3 => BoxAsync(((IPropertyConverter<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<int, int>>>)converter)
                .ConvertAsync(context, entries, cancellationToken)),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
    }

    static Task<object?> StartTwoEntryConversionAsync(
        int form,
        IPropertyConverter<long, int> elementConverter,
        IPropertyConverter<string, int> valueConverter,
        IEnumerable<int> values,
        IEnumerable<KeyValuePair<int, int>> entries,
        InitializeContext<OwnerMessage> context,
        CancellationToken cancellationToken)
    {
        return form switch
        {
            0 => BoxAsync(new ArrayPropertyConverter<long, int>(elementConverter).ConvertAsync(context, values, cancellationToken)),
            1 => BoxAsync(((IPropertyConverter<List<long>, IEnumerable<int>>)new ListPropertyConverter<long, int>(elementConverter))
                .ConvertAsync(context, values, cancellationToken)),
            2 => BoxAsync(new DictionaryPropertyConverter<int, long, int>(elementConverter).ConvertAsync(context, entries, cancellationToken)),
            3 => BoxAsync(new DictionaryKeyPropertyConverter<long, int, int>(elementConverter).ConvertAsync(context, entries, cancellationToken)),
            4 => BoxAsync(new DictionaryPropertyConverter<long, string, int, int>(elementConverter, valueConverter)
                .ConvertAsync(context, entries, cancellationToken)),
            _ => throw new ArgumentOutOfRangeException(nameof(form))
        };
    }

    static async Task<object?> BoxAsync<T>(Task<T?> task) => await task.ConfigureAwait(false);

    static void AssertSingleCollectionResult(int form, object? result)
    {
        Assert.NotNull(result);
        if (form < 6)
            Assert.Equal([10L], Assert.IsAssignableFrom<IEnumerable<long>>(result));
        else if (form < 10)
            Assert.Equal(10, Assert.Single(Assert.IsAssignableFrom<IEnumerable<KeyValuePair<int, long>>>(result)).Value);
        else
        {
            KeyValuePair<long, int> entry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<KeyValuePair<long, int>>>(result));
            Assert.Equal(10, entry.Key);
            Assert.Equal(2, entry.Value);
        }
    }

    static void AssertCombinedDictionaryResult(object? result)
    {
        KeyValuePair<long, string> entry = Assert.Single(Assert.IsAssignableFrom<IEnumerable<KeyValuePair<long, string>>>(result));
        Assert.Equal(10, entry.Key);
        Assert.Equal("ten", entry.Value);
    }

    static InitializeContext<OwnerMessage> CreateContext()
    {
        var context = new BaseInitializeContext(TestContext.Current.CancellationToken);
        return context.CreateMessageContext(new OwnerMessage());
    }

    static InitializeContext<NestedMessage> CreateNestedContext()
    {
        var context = new BaseInitializeContext(TestContext.Current.CancellationToken);
        return context.CreateMessageContext(new NestedMessage());
    }

    static void Complete<T>(TaskCompletionSource<T> source, int outcome, T value, Exception expected, CancellationToken cancellationToken)
    {
        if (outcome == 0)
            source.SetResult(value);
        else if (outcome == 1)
            source.SetException(expected);
        else
            source.SetCanceled(cancellationToken);
    }

    static async Task<T?> AssertOutcomeAsync<T>(Task<T> task, int outcome, Exception expected, CancellationToken cancellationToken)
    {
        if (outcome == 0)
            return await task;

        if (outcome == 1)
        {
            Assert.Same(expected, await Assert.ThrowsAsync(expected.GetType(), () => task));
            Assert.True(task.IsFaulted);
        }
        else
        {
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cancellationToken, actual.CancellationToken);
            Assert.True(task.IsCanceled);
        }

        return default;
    }

    static async Task AssertPendingAsync(Task task)
    {
        await Assert.ThrowsAsync<TimeoutException>(() => task.WaitAsync(TimeSpan.FromMilliseconds(50)));
        Assert.False(task.IsCompleted);
    }

    static async Task ObserveAsync(Task? task)
    {
        if (task == null)
            return;

        try
        {
            await task;
        }
        catch (Exception) when (task.IsCompleted)
        {
        }
    }

    private sealed class ControlledConverter<TResult, TInput>(Func<TInput?, Task<TResult?>> body) : IPropertyConverter<TResult, TInput>
    {
        public int Calls { get; private set; }

        public List<TInput?> Inputs { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public List<object> Contexts { get; } = [];

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            Calls++;
            Inputs.Add(input);
            Tokens.Add(cancellationToken);
            Contexts.Add(context);
            return body(input);
        }
    }

    private sealed class ThrowingConverter<TResult, TInput>(Exception exception) : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => throw exception;
    }

    private sealed class NullTaskConverter<TResult, TInput> : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => null!;
    }

    private sealed class ControlledInitializer(TaskCompletionSource<InitializeContext<NestedMessage>> source) : IMessageInitializer<NestedMessage>
    {
        public bool ReturnNull { get; init; }

        public InitializeContext<NestedMessage>? Context { get; private set; }

        public object? Input { get; private set; }

        public CancellationToken Token { get; private set; }

        public InitializeContext<NestedMessage> Create(PipeContext context) => throw new NotSupportedException();

        public InitializeContext<NestedMessage> Create(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<InitializeContext<NestedMessage>> InitializeAsync(object input, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InitializeContext<NestedMessage>> InitializeAsync(
            InitializeContext<NestedMessage> context,
            object input,
            CancellationToken cancellationToken = default)
        {
            Context = context;
            Input = input;
            Token = cancellationToken;
            return ReturnNull ? null! : source.Task;
        }

        public Task<InitializedMessage<NestedMessage>> InitializeMessageAsync(
            PipeContext context,
            object input,
            IPipe<SendContext<NestedMessage>>? pipe = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InitializedMessage<NestedMessage>> InitializeMessageAsync(
            PipeContext context,
            object input,
            object?[] moreInputs,
            IPipe<SendContext<NestedMessage>>? pipe = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InitializedMessage<NestedMessage>> InitializeMessageAsync(
            object input,
            IPipe<SendContext<NestedMessage>> pipe,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class TrackingEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
    {
        public TrackingState State { get; } = new();

        public IEnumerator<T> GetEnumerator() => new TrackingEnumerator<T>(values, State);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TrackingEnumerator<T>(IReadOnlyList<T> values, TrackingState state) : IEnumerator<T>
    {
        int _index = -1;

        public T Current => values[_index];

        object IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            state.MoveNextCount++;
            return ++_index < values.Count;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose() => state.DisposeCount++;
    }

    private sealed class TrackingState
    {
        public int DisposeCount { get; set; }

        public int MoveNextCount { get; set; }
    }

    public sealed class OwnerMessage;

    public sealed class NestedMessage
    {
        public string? Value { get; set; }
    }

    private sealed record NestedInput(string Value);
}
