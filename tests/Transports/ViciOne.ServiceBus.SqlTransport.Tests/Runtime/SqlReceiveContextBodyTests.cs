using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlReceiveContextBodyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "receive-selects-text-binary-or-empty-without-loss")]
    public void CreateBody_SelectsTheSinglePersistedRepresentation()
    {
        const string text = "{\"message\":42}";
        byte[] binary = [0xc1, 0x00, 0xff, 0x2a];

        MessageBody textBody = SqlReceiveContext.CreateBody(new SqlTransportMessage { Body = text });
        MessageBody binaryBody = SqlReceiveContext.CreateBody(new SqlTransportMessage { BinaryBody = binary });
        MessageBody emptyBody = SqlReceiveContext.CreateBody(new SqlTransportMessage());
        binary.AsSpan().Clear();

        Assert.IsType<StringMessageBody>(textBody);
        Assert.Equal(text, textBody.GetRequiredTransportText());
        Assert.IsType<BinaryMessageBody>(binaryBody);
        Assert.Equal(new byte[] { 0xc1, 0x00, 0xff, 0x2a }, binaryBody.ToArray());
        Assert.Same(EmptyMessageBody.Instance, emptyBody);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-BODY-STORAGE", "receive-rejects-ambiguous-dual-representation")]
    public void CreateBody_RejectsMissingOrAmbiguousRecords()
    {
        Assert.Equal(
            "message",
            Assert.Throws<ArgumentNullException>(() => SqlReceiveContext.CreateBody(null!)).ParamName);
        Assert.Throws<InvalidDataException>(() => SqlReceiveContext.CreateBody(new SqlTransportMessage
        {
            Body = string.Empty,
            BinaryBody = [],
        }));
    }
}
