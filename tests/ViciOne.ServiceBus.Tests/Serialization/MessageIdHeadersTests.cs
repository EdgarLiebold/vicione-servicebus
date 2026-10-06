using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class MessageIdHeadersTests
{
    private static readonly Guid MessageId = Guid.Parse("82d041f3-ed51-4692-8151-6fc684d14583");

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ID-HEADER", "case-insensitive-complete-contract")]
    public void EveryAccessPath_ExposesTheSingleIdentifierCaseInsensitively()
    {
        Headers headers = new MessageIdHeaders(MessageId);

        Assert.True(headers.TryGetHeader("messageid", out object? raw));
        Assert.Equal(MessageId, raw);
        Assert.Equal(MessageId, headers.Get<Guid>("MESSAGEID"));
        Assert.Equal("fallback", headers.Get("other", "fallback"));
        Assert.Equal([new KeyValuePair<string, object>(nameof(MessageContext.MessageId), MessageId)], headers.GetAll());
        Assert.Equal([new HeaderValue(nameof(MessageContext.MessageId), MessageId)], headers.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ID-HEADER", "incompatible-present-value-header-uses-caller-fallback")]
    public void PresentIdentifier_WithAnIncompatibleValueTypeReturnsTheCallerFallback()
    {
        var headers = new MessageIdHeaders(MessageId);
        Assert.True(headers.TryGetHeader("messageid", out object? raw));
        Assert.Equal(MessageId, raw);
        Assert.Equal(MessageId, headers.Get<Guid>("MESSAGEID", Guid.Empty));
        Assert.Equal(42, headers.Get<int>("other", 42));
        Assert.Null(headers.Get<int>("MessageId"));

        Assert.Equal(42, headers.Get<int>("mEsSaGeId", 42));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-ID-HEADER", "incompatible-present-reference-header-uses-exact-caller-fallback")]
    public void PresentIdentifier_WithAnIncompatibleReferenceTypeReturnsTheExactCallerFallback()
    {
        Headers headers = new MessageIdHeaders(MessageId);
        var fallback = new Uri("loopback://fallback");
        Assert.True(headers.TryGetHeader("MESSAGEID", out object? raw));
        Assert.Equal(MessageId, raw);
        Assert.Equal(MessageId, headers.Get<Guid>("messageid"));
        Assert.Same(fallback, headers.Get<Uri>("other", fallback));
        Assert.Null(headers.Get<Uri>("MessageId"));

        Assert.Same(fallback, headers.Get<Uri>("mEsSaGeId", fallback));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-MESSAGE-ID-HEADER", "non-empty-header-name")]
    public void EveryKeyedAccessPath_RejectsAnInvalidHeaderName(string? key)
    {
        Headers headers = new MessageIdHeaders(MessageId);

        Assert.ThrowsAny<ArgumentException>(() => headers.TryGetHeader(key!, out _));
        Assert.ThrowsAny<ArgumentException>(() => headers.Get<Guid>(key!));
        Assert.ThrowsAny<ArgumentException>(() => headers.Get(key!, "fallback"));
    }
}
