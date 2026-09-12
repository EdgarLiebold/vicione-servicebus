using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.TypeConverters;

public sealed class SpecialTypeConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "enum-conversion-matrix")]
    public void EnumConverter_CoversEverySupportedSourceAndRejectsUndefinedValues()
    {
        var converter = new EnumTypeConverter<SmallState>();

        AssertConverts<SmallState, byte>(converter, 1, SmallState.Ready);
        AssertConverts<SmallState, sbyte>(converter, 1, SmallState.Ready);
        AssertConverts<SmallState, short>(converter, 1, SmallState.Ready);
        AssertConverts<SmallState, ushort>(converter, 1, SmallState.Ready);
        AssertConverts<SmallState, int>(converter, 1, SmallState.Ready);
        AssertConverts<SmallState, uint>(converter, 1U, SmallState.Ready);
        AssertConverts<SmallState, long>(converter, 1L, SmallState.Ready);
        AssertConverts<SmallState, ulong>(converter, 1UL, SmallState.Ready);
        AssertConverts<SmallState, string>(converter, "ready", SmallState.Ready);
        AssertConverts<SmallState, object>(converter, "READY", SmallState.Ready);
        AssertConverts<SmallState, object>(converter, 1, SmallState.Ready);

        AssertDoesNotConvert<SmallState, string>(converter, "2");
        AssertDoesNotConvert<SmallState, string>(converter, "missing");
        AssertDoesNotConvert<SmallState, int>(converter, 2);
        AssertDoesNotConvert<SmallState, sbyte>(converter, -1);
        AssertDoesNotConvert<SmallState, ulong>(converter, ulong.MaxValue);
        AssertDoesNotConvert<SmallState, object>(converter, null);
        AssertDoesNotConvert<SmallState, object>(converter, new object());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "guid-conversion-matrix")]
    public void GuidConverter_PreservesGuidAndNewIdIdentityAcrossEverySupportedSource()
    {
        var converter = new GuidTypeConverter();
        NewId newId = NewId.Next();
        Guid expected = newId.ToGuid();

        AssertConverts<Guid, NewId>(converter, newId, expected);
        AssertConverts<Guid, object>(converter, expected, expected);
        AssertConverts<Guid, object>(converter, newId, expected);
        AssertConverts<Guid, object>(converter, expected.ToString("D"), expected);
        AssertConverts<Guid, string>(converter, expected.ToString("D"), expected);
        AssertConverts<string, Guid>(converter, expected, expected.ToString("D"));
        AssertDoesNotConvert<Guid, string>(converter, "not-a-guid");
        AssertDoesNotConvert<Guid, object>(converter, null);
        AssertDoesNotConvert<Guid, object>(converter, " ");
        AssertDoesNotConvert<Guid, object>(converter, new object());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "uri-and-version-conversion-matrix")]
    public void UriAndVersionConverters_CoverTypedObjectBlankAndInvalidPartitions()
    {
        var uriConverter = new ViciOne.ServiceBus.Initializers.TypeConverters.UriTypeConverter();
        var uri = new Uri("relative/path", UriKind.Relative);
        AssertConverts<string, Uri>(uriConverter, uri, "relative/path");
        AssertConverts<Uri, string>(uriConverter, "relative/path", uri);
        AssertConverts<Uri, object>(uriConverter, uri, uri);
        AssertConverts<Uri, object>(uriConverter, "relative/path", uri);
        Assert.True(uriConverter.TryConvert((string?)null, out Uri? blankUri));
        Assert.Null(blankUri);
        Assert.True(uriConverter.TryConvert((Uri?)null, out string? nullUriText));
        Assert.Null(nullUriText);
        AssertDoesNotConvert<Uri, object>(uriConverter, null);
        AssertDoesNotConvert<Uri, object>(uriConverter, " ");
        AssertDoesNotConvert<Uri, object>(uriConverter, new object());
        AssertDoesNotConvert<Uri, string>(uriConverter, "http://[invalid");

        var versionConverter = new VersionTypeConverter();
        var version = new Version(10, 2, 3, 4);
        AssertConverts<string, Version>(versionConverter, version, "10.2.3.4");
        AssertConverts<Version, string>(versionConverter, "10.2.3.4", version);
        AssertConverts<Version, object>(versionConverter, version, version);
        AssertConverts<Version, object>(versionConverter, "10.2.3.4", version);
        Assert.True(versionConverter.TryConvert((string?)null, out Version? blankVersion));
        Assert.Null(blankVersion);
        Assert.True(versionConverter.TryConvert((Version?)null, out string? nullVersionText));
        Assert.Null(nullVersionText);
        AssertDoesNotConvert<Version, object>(versionConverter, null);
        AssertDoesNotConvert<Version, object>(versionConverter, " ");
        AssertDoesNotConvert<Version, object>(versionConverter, new object());
        AssertDoesNotConvert<Version, string>(versionConverter, "10.invalid");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "string-named-value-and-object-matrix")]
    public void TextAndObjectConverters_ReportSuccessOnlyWhenAValueIsProduced()
    {
        var stringConverter = new StringTypeConverter();
        AssertConverts<string, object>(stringConverter, 42.5m, "42.5");
        AssertConverts<string, object>(stringConverter, new TextValue("stable"), "stable");
        AssertDoesNotConvert<string, object>(stringConverter, null);
        AssertDoesNotConvert<string, object>(stringConverter, new NullTextValue());

        var namedConverter = new NamedInitializerValueTypeConverter<NamedValue>();
        AssertConverts<string, NamedValue>(namedConverter, new NamedValue("ready"), "ready");
        AssertDoesNotConvert<string, NamedValue>(namedConverter, null);
        Assert.True(TypeConverterCache.TryGetTypeConverter<string, NamedValue>(out var cachedNamedConverter));
        AssertConverts<string, NamedValue>(cachedNamedConverter, new NamedValue("cached"), "cached");

        var valueObjectConverter = new ToObjectTypeConverter<Guid>();
        Guid id = Guid.NewGuid();
        AssertConverts<object, Guid>(valueObjectConverter, id, id);
        var referenceObjectConverter = new ToObjectTypeConverter<string>();
        Assert.True(referenceObjectConverter.TryConvert(null, out object? nullObject));
        Assert.Null(nullObject);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "nullable-adapter-and-cache-matrix")]
    public void NullableAdaptersAndCache_CoverDirectConvertedAbsentAndUnsupportedPaths()
    {
        var toNullable = new ToNullableTypeConverter<int>();
        AssertConverts<int?, int>(toNullable, 42, 42);
        var fromNullable = new FromNullableTypeConverter<int>();
        AssertConverts<int, int?>(fromNullable, 42, 42);
        AssertConverts<int, int?>(fromNullable, null, 0);

        var convertedToNullable = new ToNullableTypeConverter<int, string>(new IntTypeConverter());
        AssertConverts<int?, string>(convertedToNullable, "42", 42);
        AssertDoesNotConvert<int?, string>(convertedToNullable, "invalid");
        var convertedFromNullable = new FromNullableTypeConverter<long, int>(new LongTypeConverter());
        AssertConverts<long, int?>(convertedFromNullable, 42, 42L);
        AssertConverts<long, int?>(convertedFromNullable, null, 0L);

        Assert.True(TypeConverterCache.TryGetTypeConverter<int?, int>(out var cachedToNullable));
        AssertConverts<int?, int>(cachedToNullable, 7, 7);
        Assert.True(TypeConverterCache.TryGetTypeConverter<int?, string>(out var cachedConvertedToNullable));
        AssertConverts<int?, string>(cachedConvertedToNullable, "8", 8);
        Assert.True(TypeConverterCache.TryGetTypeConverter<int, int?>(out var cachedFromNullable));
        AssertConverts<int, int?>(cachedFromNullable, null, 0);
        Assert.True(TypeConverterCache.TryGetTypeConverter<long, int?>(out var cachedConvertedFromNullable));
        AssertConverts<long, int?>(cachedConvertedFromNullable, 9, 9L);
        Assert.False(TypeConverterCache.TryGetTypeConverter<DateOnly?, string>(out _));
        Assert.False(TypeConverterCache.TryGetTypeConverter<DateOnly, int?>(out _));
        Assert.False(TypeConverterCache.TryGetTypeConverter<DateOnly, object>(out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-TYPE-CONVERSION", "exception-conversion-matrix")]
    public void ExceptionConverter_CoversExceptionSnapshotExistingSnapshotTextAndMissingValues()
    {
        var converter = new ExceptionTypeConverter();
        var exception = new InvalidOperationException("outer", new ArgumentException("inner"));

        Assert.True(converter.TryConvert(exception, out ExceptionInfo? snapshot));
        Assert.NotNull(snapshot);
        Assert.Contains(nameof(InvalidOperationException), snapshot.ExceptionType, StringComparison.Ordinal);
        Assert.Equal("outer", snapshot.Message);
        Assert.NotNull(snapshot.InnerException);
        Assert.True(converter.TryConvert((object)exception, out ExceptionInfo? objectSnapshot));
        Assert.Equal(snapshot.ExceptionType, objectSnapshot?.ExceptionType);
        Assert.True(converter.TryConvert((object)snapshot, out ExceptionInfo? preservedSnapshot));
        Assert.Same(snapshot, preservedSnapshot);
        Assert.True(converter.TryConvert(exception, out string? text));
        Assert.Contains("outer", text, StringComparison.Ordinal);

        Assert.False(converter.TryConvert((Exception?)null, out ExceptionInfo? missingSnapshot));
        Assert.Null(missingSnapshot);
        Assert.False(converter.TryConvert((Exception?)null, out string? missingText));
        Assert.Null(missingText);
        Assert.False(converter.TryConvert((object?)null, out _));
        Assert.False(converter.TryConvert((object)new object(), out _));
    }

    static void AssertConverts<TResult, TInput>(ITypeConverter<TResult, TInput> converter, TInput? input, TResult expected)
    {
        Assert.True(converter.TryConvert(input, out TResult? result));
        Assert.Equal(expected, result);
    }

    static void AssertDoesNotConvert<TResult, TInput>(ITypeConverter<TResult, TInput> converter, TInput? input)
    {
        Assert.False(converter.TryConvert(input, out TResult? result));
        Assert.Equal(default, result);
    }

    private enum SmallState : byte
    {
        Unknown,
        Ready,
    }

    private sealed record NamedValue(string Name) : INamedInitializerValue;

    private sealed record TextValue(string Value)
    {
        public override string ToString() => Value;
    }

    private sealed class NullTextValue
    {
        public override string ToString() => null!;
    }
}
