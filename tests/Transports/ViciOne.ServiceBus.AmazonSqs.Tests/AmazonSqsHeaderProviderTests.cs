using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-RECEIVE", "provider-sent-timestamp-parses-exact-unix-milliseconds")]
    public void ProviderSentTimestamp_ParsesExactUnixMillisecondsAsUtc()
    {
        var message = new Message
        {
            Body = "{}",
            Attributes = new Dictionary<string, string>
            {
                [MessageSystemAttributeName.SentTimestamp] = "2154060428123",
            },
        };
        var provider = new AmazonSqsHeaderProvider(message, new SqsMessageBody(message));

        Assert.True(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? value));
        DateTime actual = Assert.IsType<DateTime>(value);

        Assert.Equal(new DateTime(2038, 4, 5, 6, 7, 8, 123, DateTimeKind.Utc), actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}
