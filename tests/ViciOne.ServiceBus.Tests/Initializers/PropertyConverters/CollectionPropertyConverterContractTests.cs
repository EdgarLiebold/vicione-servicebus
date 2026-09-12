using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class CollectionPropertyConverterContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-CONVERSION", "identity-shape-null-and-materialization-matrix")]
    public async Task IdentityConverters_PreserveCompatibleInstancesAndMaterializeEveryRequestedShapeAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;

        var arrayConverter = new ArrayPropertyConverter<int>();
        int[] array = [1, 2];
        Assert.Same(array, await arrayConverter.ConvertAsync(context, array, token));
        int[]? materializedArray = await arrayConverter.ConvertAsync(context, Yield(1, 2), token);
        Assert.NotNull(materializedArray);
        Assert.Equal([1, 2], materializedArray);
        Assert.Null(await arrayConverter.ConvertAsync(context, null, token));

        var listConverter = new ListPropertyConverter<int>();
        var list = new List<int> { 1, 2 };
        Assert.Same(list, await ConvertAsync<List<int>, IEnumerable<int>>(listConverter, context, list, token));
        Assert.Same(list, await ConvertAsync<IList<int>, IEnumerable<int>>(listConverter, context, list, token));
        Assert.Same(list, await ConvertAsync<ICollection<int>, IEnumerable<int>>(listConverter, context, list, token));
        Assert.Same(list, await ConvertAsync<IReadOnlyList<int>, IEnumerable<int>>(listConverter, context, list, token));
        Assert.Same(list, await ConvertAsync<IEnumerable<int>, IEnumerable<int>>(listConverter, context, list, token));
        Assert.Equal([1, 2], await ConvertAsync<List<int>, IEnumerable<int>>(listConverter, context, Yield(1, 2), token));
        Assert.Equal([1, 2], await ConvertAsync<IList<int>, IEnumerable<int>>(listConverter, context, Yield(1, 2), token));
        Assert.Equal([1, 2], await ConvertAsync<ICollection<int>, IEnumerable<int>>(listConverter, context, Yield(1, 2), token));
        Assert.Equal([1, 2], await ConvertAsync<IReadOnlyList<int>, IEnumerable<int>>(listConverter, context, Yield(1, 2), token));
        Assert.Null(await ConvertAsync<List<int>, IEnumerable<int>>(listConverter, context, null, token));
        Assert.Null(await ConvertAsync<IList<int>, IEnumerable<int>>(listConverter, context, null, token));
        Assert.Null(await ConvertAsync<ICollection<int>, IEnumerable<int>>(listConverter, context, null, token));
        Assert.Null(await ConvertAsync<IReadOnlyList<int>, IEnumerable<int>>(listConverter, context, null, token));
        Assert.Null(await ConvertAsync<IEnumerable<int>, IEnumerable<int>>(listConverter, context, null, token));

        var dictionaryConverter = new DictionaryPropertyConverter<int, string>();
        var dictionary = new Dictionary<int, string> { [1] = "one", [2] = "two" };
        Assert.Same(dictionary, await ConvertAsync<Dictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, dictionary, token));
        Assert.Same(dictionary, await ConvertAsync<IDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, dictionary, token));
        Assert.Same(dictionary, await ConvertAsync<IReadOnlyDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, dictionary, token));
        Assert.Same(dictionary, await ConvertAsync<IEnumerable<KeyValuePair<int, string>>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, dictionary, token));
        KeyValuePair<int, string>[] entries = [new(1, "one"), new(2, "two")];
        AssertDictionary(await ConvertAsync<Dictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, entries, token));
        AssertDictionary(await ConvertAsync<IDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, entries, token));
        AssertDictionary(await ConvertAsync<IReadOnlyDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, entries, token));
        Assert.Null(await ConvertAsync<Dictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, null, token));
        Assert.Null(await ConvertAsync<IDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, null, token));
        Assert.Null(await ConvertAsync<IReadOnlyDictionary<int, string>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, null, token));
        Assert.Null(await ConvertAsync<IEnumerable<KeyValuePair<int, string>>, IEnumerable<KeyValuePair<int, string>>>(
            dictionaryConverter, context, null, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-CONVERSION", "synchronous-element-and-shape-matrix")]
    public async Task ConvertedCollections_ProduceEveryShapeForSynchronousElementConversionsAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        int[] input = [1, 2];

        var arrayConverter = new ArrayPropertyConverter<long, int>(new TransformConverter<long, int>(value => value * 10L));
        long[]? convertedArray = await arrayConverter.ConvertAsync(context, input, token);
        Assert.NotNull(convertedArray);
        Assert.Equal([10L, 20L], convertedArray);
        Assert.Empty(Assert.IsType<long[]>(await arrayConverter.ConvertAsync(context, [], token)));
        Assert.Null(await arrayConverter.ConvertAsync(context, null, token));

        var listConverter = new ListPropertyConverter<long, int>(new TransformConverter<long, int>(value => value * 10L));
        await AssertEveryListShapeAsync(listConverter, context, input, [10L, 20L], token);
        Assert.Empty(Assert.IsType<List<long>>(await ConvertAsync<List<long>, IEnumerable<int>>(listConverter, context, [], token)));
        Assert.Null(await ConvertAsync<List<long>, IEnumerable<int>>(listConverter, context, null, token));

        var valueConverter = new DictionaryPropertyConverter<int, long, int>(new TransformConverter<long, int>(value => value * 10L));
        KeyValuePair<int, int>[] valueInput = [new(1, 2), new(3, 4)];
        await AssertEveryDictionaryShapeAsync<int, long, int, int>(valueConverter, context, valueInput,
            new Dictionary<int, long> { [1] = 20, [3] = 40 }, token);
        Assert.Empty(Assert.IsType<Dictionary<int, long>>(await valueConverter.ConvertAsync(context, [], token)));
        Assert.Null(await valueConverter.ConvertAsync(context, null, token));

        var keyConverter = new DictionaryKeyPropertyConverter<long, int, string>(new TransformConverter<long, int>(value => value * 10L));
        KeyValuePair<int, string>[] keyInput = [new(1, "one"), new(2, "two")];
        await AssertEveryDictionaryShapeAsync<long, string, int, string>(keyConverter, context, keyInput,
            new Dictionary<long, string> { [10] = "one", [20] = "two" }, token);
        Assert.Empty(Assert.IsType<Dictionary<long, string>>(await keyConverter.ConvertAsync(context, [], token)));
        Assert.Null(await keyConverter.ConvertAsync(context, null, token));

        var combinedConverter = new DictionaryPropertyConverter<long, string, int, int>(
            new TransformConverter<long, int>(value => value * 10L),
            new TransformConverter<string, int>(value => $"value-{value}"));
        await AssertEveryDictionaryShapeAsync<long, string, int, int>(combinedConverter, context, valueInput,
            new Dictionary<long, string> { [10] = "value-2", [30] = "value-4" }, token);
        Assert.Empty(Assert.IsType<Dictionary<long, string>>(await combinedConverter.ConvertAsync(context, [], token)));
        Assert.Null(await combinedConverter.ConvertAsync(context, null, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-CONVERSION", "pending-element-and-shape-matrix")]
    public async Task ConvertedCollections_CompleteEveryShapeForPendingElementConversionsAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        int[] input = [1, 2];

        var arrayConverter = new ArrayPropertyConverter<long, int>(new YieldingTransformConverter<long, int>(value => value * 10L));
        long[]? convertedArray = await arrayConverter.ConvertAsync(context, input, token);
        Assert.NotNull(convertedArray);
        Assert.Equal([10L, 20L], convertedArray);

        var listConverter = new ListPropertyConverter<long, int>(new YieldingTransformConverter<long, int>(value => value * 10L));
        await AssertEveryListShapeAsync(listConverter, context, input, [10L, 20L], token);

        KeyValuePair<int, int>[] dictionaryInput = [new(1, 2), new(3, 4)];
        var valueConverter = new DictionaryPropertyConverter<int, long, int>(new YieldingTransformConverter<long, int>(value => value * 10L));
        await AssertEveryDictionaryShapeAsync<int, long, int, int>(valueConverter, context, dictionaryInput,
            new Dictionary<int, long> { [1] = 20, [3] = 40 }, token);

        var keyConverter = new DictionaryKeyPropertyConverter<long, int, int>(new YieldingTransformConverter<long, int>(value => value * 10L));
        await AssertEveryDictionaryShapeAsync<long, int, int, int>(keyConverter, context, dictionaryInput,
            new Dictionary<long, int> { [10] = 2, [30] = 4 }, token);

        var combinedConverter = new DictionaryPropertyConverter<long, string, int, int>(
            new YieldingTransformConverter<long, int>(value => value * 10L),
            new YieldingTransformConverter<string, int>(value => $"value-{value}"));
        await AssertEveryDictionaryShapeAsync<long, string, int, int>(combinedConverter, context, dictionaryInput,
            new Dictionary<long, string> { [10] = "value-2", [30] = "value-4" }, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-CONVERSION", "pending-to-completed-transition-matrix")]
    public async Task ConvertedCollections_HandlePendingToCompletedElementTransitionsAsync()
    {
        InitializeContext<TestMessage> context = CreateContext();
        CancellationToken token = TestContext.Current.CancellationToken;
        int[] input = [1, 2];

        var arrayConverter = new ArrayPropertyConverter<long, int>(new FirstCallYieldingConverter<long, int>(value => value * 10L));
        long[]? convertedArray = await arrayConverter.ConvertAsync(context, input, token);
        Assert.NotNull(convertedArray);
        Assert.Equal([10L, 20L], convertedArray);

        var listConverter = new ListPropertyConverter<long, int>(new FirstCallYieldingConverter<long, int>(value => value * 10L));
        Assert.Equal([10L, 20L], await ConvertAsync<List<long>, IEnumerable<int>>(listConverter, context, input, token));

        KeyValuePair<int, int>[] dictionaryInput = [new(1, 2), new(3, 4)];
        var valueConverter = new DictionaryPropertyConverter<int, long, int>(
            new FirstCallYieldingConverter<long, int>(value => value * 10L));
        AssertEquivalent(new Dictionary<int, long> { [1] = 20, [3] = 40 },
            await valueConverter.ConvertAsync(context, dictionaryInput, token));

        var pendingKeyConverter = new DictionaryPropertyConverter<long, string, int, int>(
            new FirstCallYieldingConverter<long, int>(value => value * 10L),
            new TransformConverter<string, int>(value => $"value-{value}"));
        AssertEquivalent(new Dictionary<long, string> { [10] = "value-2", [30] = "value-4" },
            await pendingKeyConverter.ConvertAsync(context, dictionaryInput, token));

        var pendingValueConverter = new DictionaryPropertyConverter<long, string, int, int>(
            new TransformConverter<long, int>(value => value * 10L),
            new FirstCallYieldingConverter<string, int>(value => $"value-{value}"));
        AssertEquivalent(new Dictionary<long, string> { [10] = "value-2", [30] = "value-4" },
            await pendingValueConverter.ConvertAsync(context, dictionaryInput, token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-COLLECTION-CONVERSION", "pre-canceled-shape-matrix")]
    public async Task EveryCollectionShape_RejectsPreCanceledCallsBeforeConversionAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CancellationToken token = cancellation.Token;
        InitializeContext<TestMessage> context = CreateContext();
        int[] values = [1];
        KeyValuePair<int, int>[] entries = [new(1, 2)];

        var exactArray = new ArrayPropertyConverter<int>();
        var convertedArray = new ArrayPropertyConverter<long, int>(new TransformConverter<long, int>(value => value));
        await AssertCanceledAsync<int[], IEnumerable<int>>(exactArray, context, values, token);
        await AssertCanceledAsync<long[], IEnumerable<int>>(convertedArray, context, values, token);

        var exactList = new ListPropertyConverter<int>();
        await AssertCanceledAsync<List<int>, IEnumerable<int>>(exactList, context, values, token);
        await AssertCanceledAsync<IList<int>, IEnumerable<int>>(exactList, context, values, token);
        await AssertCanceledAsync<ICollection<int>, IEnumerable<int>>(exactList, context, values, token);
        await AssertCanceledAsync<IReadOnlyList<int>, IEnumerable<int>>(exactList, context, values, token);
        await AssertCanceledAsync<IEnumerable<int>, IEnumerable<int>>(exactList, context, values, token);

        var convertedList = new ListPropertyConverter<long, int>(new TransformConverter<long, int>(value => value));
        await AssertCanceledAsync<List<long>, IEnumerable<int>>(convertedList, context, values, token);
        await AssertCanceledAsync<IList<long>, IEnumerable<int>>(convertedList, context, values, token);
        await AssertCanceledAsync<ICollection<long>, IEnumerable<int>>(convertedList, context, values, token);
        await AssertCanceledAsync<IReadOnlyList<long>, IEnumerable<int>>(convertedList, context, values, token);
        await AssertCanceledAsync<IEnumerable<long>, IEnumerable<int>>(convertedList, context, values, token);

        var exactDictionary = new DictionaryPropertyConverter<int, int>();
        await AssertEveryDictionaryShapeIsCanceledAsync<int, int, int, int>(exactDictionary, context, entries, token);

        var convertedValues = new DictionaryPropertyConverter<int, long, int>(new TransformConverter<long, int>(value => value));
        await AssertEveryDictionaryShapeIsCanceledAsync<int, long, int, int>(convertedValues, context, entries, token);

        var convertedKeys = new DictionaryKeyPropertyConverter<long, int, int>(new TransformConverter<long, int>(value => value));
        await AssertEveryDictionaryShapeIsCanceledAsync<long, int, int, int>(convertedKeys, context, entries, token);

        var convertedKeysAndValues = new DictionaryPropertyConverter<long, long, int, int>(
            new TransformConverter<long, int>(value => value),
            new TransformConverter<long, int>(value => value));
        await AssertEveryDictionaryShapeIsCanceledAsync<long, long, int, int>(convertedKeysAndValues, context, entries, token);
    }

    static async Task AssertEveryListShapeAsync(
        ListPropertyConverter<long, int> converter,
        InitializeContext<TestMessage> context,
        IEnumerable<int> input,
        long[] expected,
        CancellationToken cancellationToken)
    {
        Assert.Equal(expected, await ConvertAsync<List<long>, IEnumerable<int>>(converter, context, input, cancellationToken));
        Assert.Equal(expected, await ConvertAsync<IList<long>, IEnumerable<int>>(converter, context, input, cancellationToken));
        Assert.Equal(expected, await ConvertAsync<ICollection<long>, IEnumerable<int>>(converter, context, input, cancellationToken));
        Assert.Equal(expected, await ConvertAsync<IReadOnlyList<long>, IEnumerable<int>>(converter, context, input, cancellationToken));
        Assert.Equal(expected, await ConvertAsync<IEnumerable<long>, IEnumerable<int>>(converter, context, input, cancellationToken));
    }

    static async Task AssertEveryDictionaryShapeAsync<TKey, TElement, TInputKey, TInputElement>(
        object converter,
        InitializeContext<TestMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>> input,
        IReadOnlyDictionary<TKey, TElement> expected,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        AssertEquivalent(expected, await ConvertAsync<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken));
        AssertEquivalent(expected, await ConvertAsync<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken));
        AssertEquivalent(expected, await ConvertAsync<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken));
        AssertEquivalent(expected, await ConvertAsync<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken));
    }

    static async Task AssertEveryDictionaryShapeIsCanceledAsync<TKey, TElement, TInputKey, TInputElement>(
        object converter,
        InitializeContext<TestMessage> context,
        IEnumerable<KeyValuePair<TInputKey, TInputElement>> input,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        await AssertCanceledAsync<Dictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken);
        await AssertCanceledAsync<IDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken);
        await AssertCanceledAsync<IReadOnlyDictionary<TKey, TElement>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken);
        await AssertCanceledAsync<IEnumerable<KeyValuePair<TKey, TElement>>, IEnumerable<KeyValuePair<TInputKey, TInputElement>>>(
            converter, context, input, cancellationToken);
    }

    static async Task AssertCanceledAsync<TResult, TInput>(object converter, InitializeContext<TestMessage> context,
        TInput input, CancellationToken cancellationToken)
    {
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ConvertAsync<TResult, TInput>(converter, context, input, cancellationToken));
        Assert.Equal(cancellationToken, exception.CancellationToken);
    }

    static Task<TResult?> ConvertAsync<TResult, TInput>(object converter, InitializeContext<TestMessage> context,
        TInput? input, CancellationToken cancellationToken) =>
        ((IPropertyConverter<TResult, TInput>)converter).ConvertAsync(context, input, cancellationToken);

    static void AssertDictionary(IEnumerable<KeyValuePair<int, string>>? actual)
    {
        Assert.NotNull(actual);
        AssertEquivalent(new Dictionary<int, string> { [1] = "one", [2] = "two" }, actual);
    }

    static void AssertEquivalent<TKey, TElement>(IReadOnlyDictionary<TKey, TElement> expected,
        IEnumerable<KeyValuePair<TKey, TElement>>? actual)
        where TKey : notnull
    {
        Assert.NotNull(actual);
        Dictionary<TKey, TElement> dictionary = actual.ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Equal(expected.Count, dictionary.Count);
        foreach ((TKey key, TElement value) in expected)
            Assert.Equal(value, dictionary[key]);
    }

    static IEnumerable<int> Yield(params int[] values)
    {
        foreach (int value in values)
            yield return value;
    }

    static InitializeContext<TestMessage> CreateContext() =>
        new BaseInitializeContext(TestContext.Current.CancellationToken).CreateMessageContext(new TestMessage());

    private sealed class TransformConverter<TResult, TInput>(Func<TInput, TResult> transform) : IPropertyConverter<TResult, TInput>
    {
        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class => Task.FromResult<TResult?>(transform(input!));
    }

    private sealed class YieldingTransformConverter<TResult, TInput>(Func<TInput, TResult> transform) : IPropertyConverter<TResult, TInput>
    {
        public async Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return transform(input!);
        }
    }

    private sealed class FirstCallYieldingConverter<TResult, TInput>(Func<TInput, TResult> transform) : IPropertyConverter<TResult, TInput>
    {
        private int _callCount;

        public Task<TResult?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TInput? input,
            CancellationToken cancellationToken = default)
            where TMessage : class
        {
            return _callCount++ == 0
                ? CompleteAsync(input!, cancellationToken)
                : Task.FromResult<TResult?>(transform(input!));
        }

        private async Task<TResult?> CompleteAsync(TInput input, CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return transform(input);
        }
    }

    private sealed class TestMessage;
}
