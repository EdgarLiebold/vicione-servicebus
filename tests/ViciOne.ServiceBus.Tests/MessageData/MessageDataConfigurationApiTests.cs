using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

public sealed class MessageDataConfigurationApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "repository-selector-method-surface")]
    public void BuiltInRepositorySelector_ExposesOneConsistentUseVerbPerCompositionOperation()
    {
        (string Name, int ParameterCount)[] actual = typeof(MessageDataRepositorySelectorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => (method.Name, method.GetParameters().Length))
            .OrderBy(signature => signature.Name, StringComparer.Ordinal)
            .ToArray();

        (string Name, int ParameterCount)[] expected =
        [
            ("UseEncryption", 4),
            ("UseFileSystem", 2),
            ("UseInMemory", 1),
        ];

        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "repository-selector-argument-boundaries")]
    public void BuiltInRepositorySelector_RejectsInvalidArgumentsAtTheirOwningBoundary()
    {
        var selector = new TestSelector();
        var keyProvider = new TestEncryptionKeyProvider();

        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            MessageDataRepositorySelectorExtensions.UseInMemory(null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            MessageDataRepositorySelectorExtensions.UseFileSystem(null!, "/tmp/data")).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentNullException>(() => selector.UseFileSystem(null!)).ParamName);
        Assert.Equal("path", Assert.Throws<ArgumentException>(() => selector.UseFileSystem("  ")).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            MessageDataRepositorySelectorExtensions.UseEncryption(null!, keyProvider, 1024, _ => new InMemoryMessageDataRepository())).ParamName);
        Assert.Equal("keyProvider", Assert.Throws<ArgumentNullException>(() =>
            selector.UseEncryption(null!, 1024, _ => new InMemoryMessageDataRepository())).ParamName);
        Assert.Equal("maximumObjectBytes", Assert.Throws<ArgumentOutOfRangeException>(() =>
            selector.UseEncryption(keyProvider, 0, _ => new InMemoryMessageDataRepository())).ParamName);
        Assert.Equal("innerSelector", Assert.Throws<ArgumentNullException>(() =>
            selector.UseEncryption(keyProvider, 1024, null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() =>
            selector.UseEncryption(keyProvider, 1024, _ => null!));

        var innerEncrypted = new EncryptedMessageDataRepository(new InMemoryMessageDataRepository(), keyProvider, 1024);
        Assert.Equal("innerSelector", Assert.Throws<ArgumentException>(() =>
            selector.UseEncryption(keyProvider, 1024, _ => innerEncrypted)).ParamName);
        Assert.IsType<InMemoryMessageDataRepository>(selector.UseInMemory());
        Assert.IsType<FileSystemMessageDataRepository>(selector.UseFileSystem(Path.Combine(Path.GetTempPath(), "vsb-selector")));
        Assert.IsType<EncryptedMessageDataRepository>(
            selector.UseEncryption(keyProvider, 1024, _ => new InMemoryMessageDataRepository()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLIC-API", "selector-result-and-owner-boundaries")]
    public void MessageDataConfiguration_RejectsMissingOwnersAndNullSelectorResults()
    {
        var repository = new InMemoryMessageDataRepository();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessageDataConfiguratorExtensions.UseMessageData(null!, repository)).ParamName);
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(configurator =>
                configurator.UseMessageData((IMessageDataRepository)null!))).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessageDataConfiguratorExtensions.UseMessageData(null!, _ => repository)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(configurator => configurator.UseMessageData(
                (Func<IMessageDataRepositorySelector, IMessageDataRepository>)null!))).ParamName);
        Assert.Throws<InvalidOperationException>(() =>
            Bus.Factory.CreateUsingInMemory(configurator => configurator.UseMessageData(_ => null!)));
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            new MessageDataRepositorySelector(null!)).ParamName);
    }

    private sealed class TestSelector : IMessageDataRepositorySelector
    {
        public IBusFactoryConfigurator Configurator => throw new InvalidOperationException(
            "This selector test does not compose a bus observer.");
    }
}
