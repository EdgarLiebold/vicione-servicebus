using System.Net.Mime;
using ViciOne.ServiceBus.SqlTransport.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Serialization;

public sealed class SqlMessageBodyStorageTests
{
    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.vicione.servicebus+json")]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "json-bodies-remain-queryable-text")]
    public void Create_PreservesJsonAsQueryableText(string mediaType)
    {
        const string json = "{\"message\":42}";

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new StringMessageBody(json),
            new ContentType(mediaType));

        Assert.Equal(json, storage.Text);
        Assert.Null(storage.Binary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "non-json-bodies-use-lossless-binary-storage")]
    public void Create_PreservesNonJsonBytesWithoutUsingATextCarrier()
    {
        byte[] expected = [0xc1, 0x00, 0xff, 0x2a];

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new BinaryMessageBody(expected),
            new ContentType("application/vnd.vicione.servicebus+msgpack"));

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
        Assert.NotSame(expected, storage.Binary);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "missing-body-and-content-type-boundaries")]
    public void Create_RejectsAMissingBodyAndUsesBinaryStorageWithoutAContentType()
    {
        byte[] expected = [0x10, 0x20, 0x30];

        Assert.Equal(
            "body",
            Assert.Throws<ArgumentNullException>(() => SqlMessageBodyStorage.Create(null!, null)).ParamName);
        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(new BinaryMessageBody(expected), null);

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.vicione.servicebus+json")]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "json-media-type-without-text-carrier-remains-binary")]
    public void Create_DoesNotDecodeAnOpaqueBodySolelyBecauseItsMediaTypeIsJson(string mediaType)
    {
        byte[] expected = "{\"message\":42}"u8.ToArray();

        SqlMessageBodyStorage storage = SqlMessageBodyStorage.Create(
            new BinaryMessageBody(expected),
            new ContentType(mediaType));

        Assert.Null(storage.Text);
        Assert.Equal(expected, storage.Binary);
        Assert.NotSame(expected, storage.Binary);
    }
}
