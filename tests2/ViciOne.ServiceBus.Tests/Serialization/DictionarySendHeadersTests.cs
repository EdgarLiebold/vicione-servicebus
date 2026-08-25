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

        var headers = new DictionarySendHeaders(source, useExistingDictionary: false);
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
        var headers = new DictionarySendHeaders(source, useExistingDictionary: true);

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
            new DictionarySendHeaders(null!, useExistingDictionary: false));
        var headers = new DictionarySendHeaders();
        ArgumentNullException stringKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, "value"));
        ArgumentNullException objectKey = Assert.Throws<ArgumentNullException>(() =>
            headers.Set(null!, new object()));

        Assert.Equal("headers", constructor.ParamName);
        Assert.Equal("key", stringKey.ParamName);
        Assert.Equal("key", objectKey.ParamName);
    }
}
