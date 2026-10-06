using System.Reflection;
using System.Runtime.Intrinsics.X86;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.NewIdFormatters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests;

[CollectionDefinition("HostCacheMutationControl", DisableParallelization = true)]
public sealed class HostCacheMutationControlCollection;

[Collection("HostCacheMutationControl")]
public sealed class AbstractionEdgeContractTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(128, 0)]
    [InlineData(128, 1)]
    [InlineData(128, 2)]
    [InlineData(256, 0)]
    [InlineData(256, 1)]
    [InlineData(256, 2)]
    [RequirementCoverage("REQ-VSB-NEWID-FORMATTER", "custom-alphabet-preserves-full-utf16")]
    public void CustomAlphabet_PreservesTheEntireCharacterForEveryInputVector(int alphabetStart, int vector)
    {
        AssertVector(alphabetStart, vector);
    }

    static void AssertVector(int alphabetStart, int vector)
    {
        string alphabet = Alphabet(alphabetStart);
        byte[] bytes = vector switch
        {
            0 => new byte[16],
            1 => Enumerable.Repeat((byte)255, 16).ToArray(),
            2 => Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            _ => throw new ArgumentOutOfRangeException(nameof(vector))
        };
        int[] indices = vector switch
        {
            0 => new int[26],
            1 => [.. Enumerable.Repeat(31, 24), 7, 31],
            // Independent 120-bit big-endian value grouped into 24 five-bit digits,
            // followed by the final eight-bit value grouped into two digits.
            2 => [0, 0, 8, 18, 4, 12, 26, 4, 10, 21, 19, 7, 15, 2, 4, 25,
                21, 10, 29, 28, 25, 23, 15, 14, 7, 31],
            _ => throw new ArgumentOutOfRangeException(nameof(vector))
        };
        string expected = new(indices.Select(index => alphabet[index]).ToArray());

        string actual = new Base32Formatter(alphabet).Format(bytes);

        Assert.Equal(expected, actual);
        Assert.Equal(26, actual.Length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-FORMATTER", "avx2-unicode-custom-alphabet-control")]
    public void Avx2CapableProcess_PreservesTheUnicodeAlphabetInsteadOfItsLowBytes()
    {
        Assert.SkipUnless(Avx2.IsSupported && BitConverter.IsLittleEndian,
            "AVX2/little-endian is unavailable; this run proves no AVX2 branch behavior.");

        AssertVector(256, 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "typed-value-copy-write-is-explicitly-rejected")]
    public void TypedStructSetter_RejectsTheImpossibleByValueMutation(bool fullyTyped)
    {
        var target = new ValueTarget { Count = 13 };
        PropertyInfo property = typeof(ValueTarget).GetProperty(nameof(ValueTarget.Count))!;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            if (fullyTyped)
                new ReadWriteProperty<ValueTarget, int>(property).Set(target, 37);
            else
                new ReadWriteProperty<ValueTarget>(property).Set(target, 37);
        });

        Assert.Contains("value type", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(13, target.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-WRITE", "reference-box-interface-and-struct-reader-remain-valid")]
    public void PropertyAccess_PreservesReferenceMutationExistingBoxesAndStructReaders()
    {
        var reference = new ReferenceTarget { Count = 3 };
        var classAccessor = new ReadWriteProperty<ReferenceTarget, int>(
            typeof(ReferenceTarget).GetProperty(nameof(ReferenceTarget.Count))!);
        classAccessor.Set(reference, 41);
        Assert.Equal(41, reference.Count);
        Assert.Equal(41, classAccessor.Get(reference));

        object box = new ValueTarget { Count = 5 };
        var untyped = new ReadWriteProperty(typeof(ValueTarget).GetProperty(nameof(ValueTarget.Count))!);
        untyped.Set(box, 43);
        Assert.Equal(43, ((ValueTarget)box).Count);
        Assert.Equal(43, untyped.Get(box));

        // Use interface-declared metadata: object + struct-declared metadata is
        // already an incompatible target shape under the public validation rules.
        var interfaceAccessor = new ReadWriteProperty<ICountTarget, int>(
            typeof(ICountTarget).GetProperty(nameof(ICountTarget.Count))!);
        interfaceAccessor.Set((ICountTarget)box, 47);
        Assert.Equal(47, ((ValueTarget)box).Count);
        Assert.Equal(47, interfaceAccessor.Get((ICountTarget)box));

        var value = new ValueTarget { Count = 53 };
        var reader = new ReadOnlyProperty<ValueTarget, int>(
            typeof(ValueTarget).GetProperty(nameof(ValueTarget.Count))!);
        Assert.Equal(53, reader.Get(value));
        Assert.Equal(53, value.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-NEWID-VALUE", "timestamp-clamps-with-utc-offset-zero")]
    public void Timestamp_ClampsRawBoundsAndPreservesAnOrdinaryUtcInstant(int mode)
    {
        DateTimeOffset expected = mode switch
        {
            0 => DateTimeOffset.MinValue,
            1 => DateTimeOffset.MaxValue,
            2 => new DateTimeOffset(2026, 8, 17, 21, 4, 5, TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        long ticks = expected.Ticks;
        NewIdValue id = mode switch
        {
            0 => new NewIdValue(-1, 0, 0, 0),
            1 => new NewIdValue(int.MaxValue, 0, 0, 0),
            _ => new NewIdValue((int)(ticks >> 32), unchecked((int)ticks), 7, 11)
        };

        DateTimeOffset actual = id.Timestamp;

        Assert.Equal(expected.Ticks, actual.Ticks);
        Assert.Equal(TimeSpan.Zero, actual.Offset);
        Assert.Equal(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-METADATA", "public-wire-mutation-cannot-change-the-process-cache")]
    public void MutableWireDto_CannotChangeTheImmutableCachedProcessSnapshot()
    {
        HostInfo snapshot = HostMetadataCache.Host;
        HostInfo empty = HostMetadataCache.Empty;
        var cachedHost = Assert.IsType<BusHostInfo>(snapshot);
        var cachedEmpty = Assert.IsType<BusHostInfo>(empty);
        var mutations = new (Func<BusHostInfo, object?> Read, Action<BusHostInfo, object?> Write, object Marker)[]
        {
            (host => host.MachineName, (host, value) => host.MachineName = (string?)value, "changed-machine"),
            (host => host.ProcessName, (host, value) => host.ProcessName = (string?)value, "changed-process"),
            (host => host.ProcessId, (host, value) => host.ProcessId = (int)value!, int.MinValue),
            (host => host.Assembly, (host, value) => host.Assembly = (string?)value, "changed-assembly"),
            (host => host.AssemblyVersion, (host, value) => host.AssemblyVersion = (string?)value, "changed-assembly-version"),
            (host => host.FrameworkVersion, (host, value) => host.FrameworkVersion = (string?)value, "changed-framework"),
            (host => host.ViciOneServiceBusVersion, (host, value) => host.ViciOneServiceBusVersion = (string?)value, "changed-servicebus"),
            (host => host.OperatingSystemVersion, (host, value) => host.OperatingSystemVersion = (string?)value, "changed-os")
        };
        var wire = new BusHostInfo();
        foreach (var mutation in mutations)
        {
            mutation.Write(wire, mutation.Marker);
            Assert.Equal(mutation.Marker, mutation.Read(wire));
        }

        foreach (BusHostInfo cached in new[] { cachedHost, cachedEmpty })
        {
            foreach (var mutation in mutations)
            {
                object? original = mutation.Read(cached);
                Assert.NotEqual(mutation.Marker, original);
                try
                {
                    Exception? rejected = Record.Exception(() => mutation.Write(cached, mutation.Marker));
                    if (rejected != null)
                    {
                        InvalidOperationException error = Assert.IsType<InvalidOperationException>(rejected);
                        Assert.Equal("Cached host metadata is read-only.", error.Message);
                    }
                    Assert.Equal(original, mutation.Read(cached));
                    Assert.Same(snapshot, HostMetadataCache.Host);
                    Assert.Same(empty, HostMetadataCache.Empty);
                }
                finally
                {
                    // Original mutable mode changes the value and fails the finite equality;
                    // restored regardless. Frozen mode remains unchanged and needs no setter.
                    if (!Equals(original, mutation.Read(cached)))
                        mutation.Write(cached, original);
                }
            }
        }

        Assert.NotSame(snapshot, empty);
        Assert.Null(empty.MachineName);
        Assert.Equal(0, empty.ProcessId);
    }

    static string Alphabet(int first) => first == 0
        ? "765432zyxwvutsrqponmlkjihgfedcba"
        : new string(Enumerable.Range(first, 32).Select(value => (char)value).ToArray());

    public interface ICountTarget { int Count { get; set; } }
    public struct ValueTarget : ICountTarget { public int Count { get; set; } }
    public sealed class ReferenceTarget { public int Count { get; set; } }
}
