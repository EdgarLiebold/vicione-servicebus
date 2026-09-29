using System.Reflection;
using ViciOne.ServiceBus.Internals.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Internals.Reflection;

public sealed class PropertyCacheContractTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "required-property-name")]
    public void NameEntryPoints_RejectAMissingOrBlankPropertyName(string? name)
    {
        Exception[] exceptions =
        [
            Record.Exception(() => ReadPropertyCache<CacheTarget>.GetProperty<string>(name!))!,
            Record.Exception(() => ReadPropertyCache<CacheTarget>.TryGetProperty<string>(name!, out _))!,
            Record.Exception(() => WritePropertyCache<CacheTarget>.GetProperty<string>(name!))!,
            Record.Exception(() => WritePropertyCache<CacheTarget>.CanWrite(name!))!,
        ];

        Assert.All(exceptions, exception =>
        {
            ArgumentException argumentException = Assert.IsAssignableFrom<ArgumentException>(exception);
            Assert.Equal("name", argumentException.ParamName);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "property-metadata-ownership")]
    public void MetadataEntryPoints_RejectAPropertyFromAnUnrelatedType()
    {
        PropertyInfo property = typeof(UnrelatedTarget).GetProperty(nameof(UnrelatedTarget.Name))!;

        ArgumentException readException = Assert.Throws<ArgumentException>(() =>
            ReadPropertyCache<CacheTarget>.GetProperty<string>(property));
        ArgumentException writeException = Assert.Throws<ArgumentException>(() =>
            WritePropertyCache<CacheTarget>.GetProperty<string>(property));

        Assert.Equal("propertyInfo", readException.ParamName);
        Assert.Equal("propertyInfo", writeException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "property-type-mismatch")]
    public void RequiredPropertyLookup_ReportsTheActualAndRequestedTypesConsistently()
    {
        ArgumentException readException = Assert.Throws<ArgumentException>(() =>
            ReadPropertyCache<CacheTarget>.GetProperty<int>(nameof(CacheTarget.Name)));
        ArgumentException writeException = Assert.Throws<ArgumentException>(() =>
            WritePropertyCache<CacheTarget>.GetProperty<int>(nameof(CacheTarget.Name)));

        Assert.Equal("name", readException.ParamName);
        Assert.Equal("name", writeException.ParamName);
        Assert.Contains(nameof(CacheTarget.Name), readException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(CacheTarget.Name), writeException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(String), readException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(String), writeException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Int32), readException.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Int32), writeException.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "stable-case-insensitive-access")]
    public void ValidPropertyLookup_IsCaseInsensitiveStableAndExecutable()
    {
        IReadProperty<CacheTarget, string> firstRead = ReadPropertyCache<CacheTarget>.GetProperty<string>("name");
        IReadProperty<CacheTarget, string> secondRead = ReadPropertyCache<CacheTarget>.GetProperty<string>("NAME");
        IReadProperty<CacheTarget, string> metadataRead = ReadPropertyCache<CacheTarget>.GetProperty<string>(
            typeof(CacheTarget).GetProperty(nameof(CacheTarget.Name)));
        IWriteProperty<CacheTarget, string> firstWrite = WritePropertyCache<CacheTarget>.GetProperty<string>("name");
        IWriteProperty<CacheTarget, string> secondWrite = WritePropertyCache<CacheTarget>.GetProperty<string>("NAME");
        IWriteProperty<CacheTarget, string> metadataWrite = WritePropertyCache<CacheTarget>.GetProperty<string>(
            typeof(CacheTarget).GetProperty(nameof(CacheTarget.Name)));
        var target = new CacheTarget();

        firstWrite.Set(target, "A+");

        Assert.Same(firstRead, secondRead);
        Assert.Same(firstRead, metadataRead);
        Assert.Same(firstWrite, secondWrite);
        Assert.Same(firstWrite, metadataWrite);
        Assert.Equal("A+", firstRead.Get(target));
        Assert.True(WritePropertyCache<CacheTarget>.CanWrite(nameof(CacheTarget.Name)));
        Assert.True(ReadPropertyCache<CacheTarget>.TryGetProperty<string>(nameof(CacheTarget.Name), out var found));
        Assert.Same(firstRead, found);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "missing-and-optional-lookups")]
    public void MissingAndMismatchedOptionalLookups_ReturnFalseWhileRequiredLookupsFailPrecisely()
    {
        bool missing = ReadPropertyCache<CacheTarget>.TryGetProperty<string>("Missing", out var missingProperty);
        bool mismatched = ReadPropertyCache<CacheTarget>.TryGetProperty<int>(nameof(CacheTarget.Name), out var mismatchedProperty);
        ArgumentException readException = Assert.Throws<ArgumentException>(() =>
            ReadPropertyCache<CacheTarget>.GetProperty<string>("Missing"));
        ArgumentException writeException = Assert.Throws<ArgumentException>(() =>
            WritePropertyCache<CacheTarget>.GetProperty<string>("Missing"));
        ArgumentException canWriteException = Assert.Throws<ArgumentException>(() =>
            WritePropertyCache<CacheTarget>.CanWrite("Missing"));

        Assert.False(missing);
        Assert.Null(missingProperty);
        Assert.False(mismatched);
        Assert.Null(mismatchedProperty);
        Assert.Equal("name", readException.ParamName);
        Assert.Equal("name", writeException.ParamName);
        Assert.Equal("name", canWriteException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "interface-implementation-write")]
    public void InterfacePropertyCache_WritesTheGeneratedImplementationAndReadsTheContract()
    {
        Type implementationType = MessageImplementationCache<InterfaceCacheTarget>.ImplementationType;
        var target = (InterfaceCacheTarget)Activator.CreateInstance(implementationType)!;
        IWriteProperty<InterfaceCacheTarget, string> write =
            WritePropertyCache<InterfaceCacheTarget>.GetProperty<string>(nameof(InterfaceCacheTarget.Name));
        IReadProperty<InterfaceCacheTarget, string> read =
            ReadPropertyCache<InterfaceCacheTarget>.GetProperty<string>(nameof(InterfaceCacheTarget.Name));

        write.Set(target, "generated");

        Assert.Equal(implementationType, write.TargetType);
        Assert.Equal("generated", read.Get(target));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RUNTIME-PROPERTY-CACHE", "case-distinct-hidden-property-access")]
    public void CaseDistinctHiddenProperty_UsesDerivedMemberWithoutDuplicateKeyFailure()
    {
        IReadProperty<CaseDerivedTarget, string> read = ReadPropertyCache<CaseDerivedTarget>.GetProperty<string>("ITEM");
        IWriteProperty<CaseDerivedTarget, string> write = WritePropertyCache<CaseDerivedTarget>.GetProperty<string>("item");
        var target = new CaseDerivedTarget();

        write.Set(target, "derived-updated");

        Assert.Equal("derived-updated", read.Get(target));
        Assert.Equal("derived-updated", target.item);
        Assert.Equal("base", ((CaseBaseTarget)target).Item);
    }

    private sealed class CacheTarget
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class UnrelatedTarget
    {
        public string Name { get; set; } = string.Empty;
    }

    private class CaseBaseTarget
    {
        public string Item { get; set; } = "base";
    }

    private sealed class CaseDerivedTarget : CaseBaseTarget
    {
        public string item { get; set; } = "derived";
    }

    public interface InterfaceCacheTarget
    {
        string Name { get; }
    }
}
