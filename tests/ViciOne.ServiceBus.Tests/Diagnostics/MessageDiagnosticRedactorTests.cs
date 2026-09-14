using System.Collections.Frozen;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Diagnostics;

public sealed class MessageDiagnosticRedactorTests
{
    private readonly MessageDiagnosticRedactor _redactor =
        new(new MessageSensitivityInspector(), maximumStringLength: 8);

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "payload-and-member-classification")]
    public void PayloadAndMemberClassification_RedactsOnlyTheDeclaredScope()
    {
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(SensitiveMessage), nameof(SensitiveMessage.Count), 42));
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(PartiallySensitiveMessage), nameof(PartiallySensitiveMessage.Secret), "secret"));
        Assert.Equal("7",
            _redactor.RenderValue(typeof(PartiallySensitiveMessage), nameof(PartiallySensitiveMessage.Count), 7));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "base-interface-and-override-inheritance")]
    public void Classification_IsConservativeAcrossBaseInterfacesAndOverrides()
    {
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(DerivedSensitiveMessage), nameof(DerivedSensitiveMessage.Count), 1));
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(InterfaceSensitiveMessage), nameof(InterfaceSensitiveMessage.Count), 1));
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(InterfaceMemberMessage), nameof(InterfaceMemberMessage.Secret), "secret"));
        Assert.Equal(MessageDiagnosticRedactor.Redacted,
            _redactor.RenderValue(typeof(OverrideSensitiveMemberMessage), nameof(OverrideSensitiveMemberMessage.Secret), "secret"));

        MessageSensitivityDescriptor fieldDescriptor = new MessageSensitivityInspector().Inspect(typeof(FieldSensitiveMessage));
        Assert.True(fieldDescriptor.IsMemberSensitive(nameof(FieldSensitiveMessage.DirectSecret)));
        Assert.True(fieldDescriptor.IsMemberSensitive(nameof(FieldSensitiveBase.BaseSecret)));
        Assert.False(fieldDescriptor.IsMemberSensitive(nameof(FieldSensitiveMessage.Visible)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "bounded-control-safe-strings")]
    public void Strings_AreBoundedAndCannotInjectControlCharacters()
    {
        Assert.Equal("12345678…",
            _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Text), "1234567890"));
        Assert.Equal("first��s…",
            _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Text), "first\r\nsecond\tvalue"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "unicode-scalar-boundary-and-invalid-surrogate")]
    public void StringBounds_NeverSplitUnicodeScalarsAndSanitizeInvalidSurrogates()
    {
        var twoCharacterLimit = new MessageDiagnosticRedactor(
            new MessageSensitivityInspector(),
            maximumStringLength: 2);
        var threeCharacterLimit = new MessageDiagnosticRedactor(
            new MessageSensitivityInspector(),
            maximumStringLength: 3);

        Assert.Equal("A…", twoCharacterLimit.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "A😀B"));
        Assert.Equal("A😀…", threeCharacterLimit.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "A😀B"));
        Assert.Equal("A😀", threeCharacterLimit.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "A😀"));
        Assert.Equal("A�B", _redactor.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "A\uD800B"));
        Assert.Equal("�A", _redactor.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "\uDC00A"));
        Assert.Equal("A�", _redactor.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Text), "A\uD800"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "relative-and-absolute-uri")]
    public void UriRendering_IsBoundedAndSupportsRelativeAddresses()
    {
        var redactor = new MessageDiagnosticRedactor(new MessageSensitivityInspector(), maximumStringLength: 256);

        Assert.Equal("relative/path", redactor.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Value), new Uri("relative/path", UriKind.Relative)));
        Assert.Equal("https://example.test/a", redactor.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Value), new Uri("https://example.test/a")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "never-application-tostring")]
    public void ArbitraryApplicationObjects_NeverExecuteToString()
    {
        var value = new ExplosiveToString();

        string rendered = _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), value);

        Assert.Equal(MessageDiagnosticRedactor.ComplexValue, rendered);
        Assert.False(value.WasCalled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "safe-primitive-canonical-format")]
    public void SafePrimitives_UseCultureIndependentBoundedFormats()
    {
        Guid guid = Guid.Parse("f44a3ca3-7f6f-4485-b81c-9bc50eaaee29");
        DateTime dateTime = new(2026, 9, 3, 12, 34, 56, DateTimeKind.Utc);
        DateTimeOffset timestamp = DateTimeOffset.Parse("2026-09-03T12:34:56+00:00");
        var complete = new MessageDiagnosticRedactor(new MessageSensitivityInspector(), maximumStringLength: 256);

        Assert.Equal("true", _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), true));
        Assert.Equal("false", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), false));
        Assert.Equal("A", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 'A'));
        Assert.Equal("�", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), '\n'));
        Assert.Equal("1", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), (byte)1));
        Assert.Equal("-2", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), (sbyte)-2));
        Assert.Equal("-3", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), (short)-3));
        Assert.Equal("4", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), (ushort)4));
        Assert.Equal("-5", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), -5));
        Assert.Equal("6", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 6U));
        Assert.Equal("-7", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), -7L));
        Assert.Equal("8", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 8UL));
        Assert.Equal("1.25", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 1.25F));
        Assert.Equal("2.5", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 2.5D));
        Assert.Equal("12.5", _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), 12.5m));
        Assert.Equal("f44a3ca3…", _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), guid));
        Assert.Equal("2026-09-03T12:34:56.0000000Z",
            complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), dateTime));
        Assert.Equal("2026-09-…", _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), timestamp));
        Assert.Equal("01:02:03", complete.RenderValue(
            typeof(NormalMessage), nameof(NormalMessage.Value), new TimeSpan(1, 2, 3)));
        Assert.Equal("Monday", complete.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), DayOfWeek.Monday));
        Assert.Equal("<null>", _redactor.RenderValue(typeof(NormalMessage), nameof(NormalMessage.Value), null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "descriptor-and-rendering-boundaries")]
    public void DescriptorAndRendering_EnforceOrdinalMemberNamesAndRejectInvalidInput()
    {
        FrozenSet<string> caseInsensitiveNames = new[] { "Secret" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        var descriptor = new MessageSensitivityDescriptor(MessagePayloadSensitivity.Normal, caseInsensitiveNames);

        Assert.True(descriptor.IsMemberSensitive("Secret"));
        Assert.False(descriptor.IsMemberSensitive("secret"));
        Assert.Equal("memberName", Assert.Throws<ArgumentNullException>(() => descriptor.IsMemberSensitive(null!)).ParamName);
        Assert.Equal("memberName", Assert.Throws<ArgumentException>(() => descriptor.IsMemberSensitive(" ")).ParamName);
        Assert.Equal("sensitiveMembers", Assert.Throws<ArgumentException>(() => new MessageSensitivityDescriptor(
            MessagePayloadSensitivity.Normal,
            new[] { " " }.ToFrozenSet(StringComparer.Ordinal))).ParamName);
        Assert.Equal("memberName", Assert.Throws<ArgumentException>(() => _redactor.RenderValue(
            typeof(SensitiveMessage),
            " ",
            "secret")).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            _redactor.RenderValue(null!, null, null)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(() =>
            new MessageSensitivityInspector().Inspect(null!)).ParamName);
        Assert.Equal("inspector", Assert.Throws<ArgumentNullException>(() =>
            new MessageDiagnosticRedactor(null!)).ParamName);
        Assert.Equal("maximumStringLength", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDiagnosticRedactor(new MessageSensitivityInspector(), 0)).ParamName);
        Assert.Equal("maximumStringLength", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageDiagnosticRedactor(new MessageSensitivityInspector(), -1)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "collectible-type-cache-does-not-pin")]
    public void SensitivityCache_DoesNotKeepACollectibleTypeAlive()
    {
        var inspector = new MessageSensitivityInspector();
        WeakReference reference = InspectCollectibleType(inspector);

        for (var attempt = 0; attempt < 20 && reference.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.False(reference.IsAlive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DIAGNOSTIC-REDACTION", "idempotent-di-registration")]
    public void Registration_IsIdempotentAndPreservesApplicationOverrides()
    {
        var custom = new CustomRedactor();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageDiagnosticRedactor>(custom);
        services.AddViciOneMessageDiagnosticRedaction();
        services.AddViciOneMessageDiagnosticRedaction();
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(custom, provider.GetRequiredService<IMessageDiagnosticRedactor>());
        Assert.IsType<MessageSensitivityInspector>(provider.GetRequiredService<IMessageSensitivityInspector>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMessageSensitivityInspector));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InspectCollectibleType(MessageSensitivityInspector inspector)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"CollectibleDiagnosticContract_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule("Contracts");
        TypeBuilder builder = module.DefineType("CollectibleMessage", TypeAttributes.Public | TypeAttributes.Class);
        Type type = builder.CreateType()!;

        Assert.False(inspector.Inspect(type).IsSensitive);
        return new WeakReference(type);
    }

    [SensitivePayload]
    private sealed record SensitiveMessage(int Count);

    [SensitivePayload]
    private abstract record SensitiveBase(int Count);

    private sealed record DerivedSensitiveMessage(int Count) : SensitiveBase(Count);

    [SensitivePayload]
    private interface ISensitiveContract
    {
        int Count { get; }
    }

    private sealed record InterfaceSensitiveMessage(int Count) : ISensitiveContract;

    private interface IPartiallySensitiveContract
    {
        [SensitiveMember]
        string Secret { get; }
    }

    private sealed record InterfaceMemberMessage(string Secret) : IPartiallySensitiveContract;

    private sealed record PartiallySensitiveMessage([property: SensitiveMember] string Secret, int Count);

    private sealed record NormalMessage(string Text, object Value);

    private abstract class SensitiveMemberBase
    {
        [SensitiveMember]
        public virtual string Secret { get; init; } = string.Empty;
    }

    private sealed class OverrideSensitiveMemberMessage : SensitiveMemberBase
    {
        public override string Secret { get; init; } = string.Empty;
    }

    private class FieldSensitiveBase
    {
        [SensitiveMember]
        public string BaseSecret = string.Empty;
    }

    private sealed class FieldSensitiveMessage : FieldSensitiveBase
    {
        [SensitiveMember]
        public string DirectSecret = string.Empty;

        public string Visible = string.Empty;
    }

    private sealed class ExplosiveToString
    {
        public bool WasCalled { get; private set; }

        public override string ToString()
        {
            WasCalled = true;
            throw new InvalidOperationException("Diagnostic rendering must not invoke application formatting.");
        }
    }

    private sealed class CustomRedactor : IMessageDiagnosticRedactor
    {
        public string RenderValue(Type messageType, string? memberName, object? value) => "custom";
    }
}
