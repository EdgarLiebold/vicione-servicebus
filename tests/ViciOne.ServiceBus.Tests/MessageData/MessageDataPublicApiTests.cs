using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataPublicApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "exact-core-exported-surface")]
    public void CoreAssembly_ExportsOnlyApplicationFacingMessageDataTypes()
    {
        string[] expected =
        [
            "ViciOne.ServiceBus.Advanced.MessageData",
            "ViciOne.ServiceBus.Advanced.MessageDataExtensions",
            "ViciOne.ServiceBus.Configuration.IMessageDataRepositorySelector",
            "ViciOne.ServiceBus.Configuration.MessageDataConfiguratorExtensions",
            "ViciOne.ServiceBus.Configuration.MessageDataRepositorySelectorExtensions",
            "ViciOne.ServiceBus.MessageData.EncryptedMessageDataRepository",
            "ViciOne.ServiceBus.MessageData.FileSystemMessageDataRepository",
            "ViciOne.ServiceBus.MessageData.InMemoryMessageDataRepository",
        ];

        string[] actual = typeof(InMemoryMessageDataRepository).Assembly.GetExportedTypes()
            .Where(type => type.Name.Contains("MessageData", StringComparison.Ordinal)
                || type.Namespace?.Contains(".MessageData", StringComparison.Ordinal) == true)
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "strongly-typed-value-factory")]
    public async Task ValueFactory_CreatesPopulatedAndEmptyMessageDataWithoutExposingImplementationsAsync()
    {
        object value = new();

        MessageData<object> populated = ViciOne.ServiceBus.Advanced.MessageData.FromValue(value);
        MessageData<object> empty = ViciOne.ServiceBus.Advanced.MessageData.Empty<object>();

        Assert.True(populated.HasValue);
        Assert.Same(value, await populated.Value);
        Assert.Null(populated.Address);
        Assert.False(empty.HasValue);
        Assert.Throws<MessageDataException>(() => _ = empty.Address);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() =>
            ViciOne.ServiceBus.Advanced.MessageData.FromValue<object>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "binary-value-factory-snapshot")]
    public async Task ValueFactory_CapturesAndReturnsIndependentBinarySnapshotsAsync()
    {
        byte[] source = [1, 2, 3];
        MessageData<byte[]> value = ViciOne.ServiceBus.Advanced.MessageData.FromValue(source);

        source[0] = 9;
        byte[] first = Assert.IsType<byte[]>(await value.Value);
        first[1] = 8;
        byte[] second = Assert.IsType<byte[]>(await value.Value);

        Assert.Equal([1, 2, 3], second);
        Assert.NotSame(source, first);
        Assert.NotSame(first, second);
    }
}
