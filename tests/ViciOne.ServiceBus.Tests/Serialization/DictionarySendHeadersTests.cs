using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class DictionarySendHeadersTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "independent-case-insensitive-copy")]
    public void CopyingConstructor_CreatesAnIndependentCaseInsensitiveSnapshot()
    {
        var source = new Dictionary<string, object>
        {
            ["Trace-Id"] = "original",
        };

        var headers = new DictionarySendHeaders(source);
        source["Trace-Id"] = "source-changed";
        headers.Set("TRACE-ID", "copy-changed");

        Assert.Equal("copy-changed", headers.Get<string>("trace-id", null));
        Assert.Equal("source-changed", source["Trace-Id"]);
        Assert.Single(headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "explicit-shared-dictionary")]
    public void ExistingDictionaryConstructor_PreservesTheExplicitSharedStorageContract()
    {
        var source = new Dictionary<string, object>
        {
            ["Trace-Id"] = "original",
        };
        var headers = DictionarySendHeaders.Wrap(source);

        headers.Set("Trace-Id", "through-headers");
        source["Extra"] = 27;

        Assert.Equal("through-headers", source["Trace-Id"]);
        Assert.Equal(27, headers.Get<int>("Extra", null));
        Assert.Equal(2, headers.GetAll().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "overwrite-preservation-and-removal")]
    public void Set_HonorsOverwritePreservationAndNullRemoval()
    {
        var headers = new DictionarySendHeaders();

        headers.Set("Name", "first");
        headers.Set("NAME", "ignored", overwrite: false);

        Assert.Equal("first", headers.Get<string>("name", null));

        headers.Set("name", "replacement", overwrite: true);
        Assert.Equal("replacement", headers.Get<string>("NAME", null));

        headers.Set("NAME", null);
        Assert.False(headers.TryGetHeader("name", out _));
        Assert.Empty(headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "required-inputs")]
    public void ConstructorsAndSet_RejectEveryMissingRequiredInput()
    {
        ArgumentNullException constructor = Assert.Throws<ArgumentNullException>(() =>
            new DictionarySendHeaders(null!));
        var headers = new DictionarySendHeaders();
        ArgumentNullException stringKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, "value"));
        ArgumentNullException objectKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, new object()));

        Assert.Equal("headers", constructor.ParamName);
        Assert.Equal("key", stringKey.ParamName);
        Assert.Equal("key", objectKey.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "arbitrary-dictionary-implementation")]
    public void WrappedDictionary_DoesNotRequireIReadOnlyDictionaryForTypedReads()
    {
        IDictionary<string, object> source = new DictionaryOnly<string, object>
        {
            ["attempt"] = "27",
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);

        Assert.Equal(27, headers.Get<int>("attempt", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "non-empty-header-name")]
    public void Set_RejectsEveryEmptyHeaderName(string key)
    {
        var headers = new DictionarySendHeaders();

        ArgumentException stringValue = Assert.Throws<ArgumentException>(() => headers.Set(key, "value"));
        ArgumentException objectValue = Assert.Throws<ArgumentException>(() => headers.Set(key, new object()));

        Assert.Equal("key", stringValue.ParamName);
        Assert.Equal("key", objectValue.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "wrapped-invalid-entry-containment")]
    public void WrappedDictionary_NeverExposesExternallyInjectedInvalidEntries()
    {
        var source = new Dictionary<string, object>
        {
            ["valid"] = 27,
        };
        DictionarySendHeaders headers = DictionarySendHeaders.Wrap(source);
        source[""] = "invalid-name";
        source["null-value"] = null!;

        Assert.False(headers.TryGetHeader("null-value", out _));
        Assert.Equal([new KeyValuePair<string, object>("valid", 27)], headers.GetAll());
        Assert.Equal([new HeaderValue("valid", 27)], headers.ToArray());
    }

    private sealed class DictionaryOnly<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _items = [];

        public TValue this[TKey key] { get => _items[key]; set => _items[key] = value; }
        public ICollection<TKey> Keys => _items.Keys;
        public ICollection<TValue> Values => _items.Values;
        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public void Add(TKey key, TValue value) => _items.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Contains(item);
        public bool ContainsKey(TKey key) => _items.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)_items).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _items.GetEnumerator();
        public bool Remove(TKey key) => _items.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_items).Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => _items.TryGetValue(key, out value!);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
