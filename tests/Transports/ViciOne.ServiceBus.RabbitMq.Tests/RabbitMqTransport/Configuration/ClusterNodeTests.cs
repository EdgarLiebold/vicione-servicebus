using System.Globalization;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class ClusterNodeTests
{
    [Theory]
    [InlineData("node.example", "node.example", null, "node.example")]
    [InlineData("node.example:1", "node.example", 1, "node.example:1")]
    [InlineData("node.example:65535", "node.example", 65535, "node.example:65535")]
    [InlineData("127.0.0.1:5672", "127.0.0.1", 5672, "127.0.0.1:5672")]
    [InlineData("[2001:db8::1]", "2001:db8::1", null, "[2001:db8::1]")]
    [InlineData("[2001:db8::1]:5671", "2001:db8::1", 5671, "[2001:db8::1]:5671")]
    [InlineData("2001:db8::1", "2001:db8::1", null, "[2001:db8::1]")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CLUSTER-NODE", "canonical-dns-ipv4-ipv6-round-trip")]
    public void Parse_AcceptsSupportedNodesAndFormatsThemCanonically(
        string input,
        string expectedHost,
        int? expectedPort,
        string expectedText)
    {
        ClusterNode node = ClusterNode.Parse(input);

        Assert.Equal(expectedHost, node.HostName);
        Assert.Equal(expectedPort, node.Port);
        Assert.Equal(expectedText, node.ToString());
        Assert.Equal(node, ClusterNode.Parse(node.ToString()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(":5672")]
    [InlineData("node:")]
    [InlineData("node:0")]
    [InlineData("node:-1")]
    [InlineData("node:65536")]
    [InlineData("node:not-a-port")]
    [InlineData("[::1")]
    [InlineData("[::1]extra")]
    [InlineData("node:1:2")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CLUSTER-NODE", "invalid-node-and-port-boundaries")]
    public void Parse_RejectsInvalidNodeText(string input)
    {
        Assert.Throws<ArgumentException>(() => ClusterNode.Parse(input));
        Assert.False(ClusterNode.TryParse(input, out ClusterNode result));
        Assert.Equal(default, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CLUSTER-NODE", "standard-string-and-span-parsing")]
    public void StandardParsingContracts_AreCultureIndependentAndSpanCapable()
    {
        Assert.Contains(typeof(IParsable<ClusterNode>), typeof(ClusterNode).GetInterfaces());
        Assert.Contains(typeof(ISpanParsable<ClusterNode>), typeof(ClusterNode).GetInterfaces());

        Assert.True(ClusterNode.TryParse("node:5672", CultureInfo.GetCultureInfo("ar-SA"), out ClusterNode fromString));
        Assert.True(ClusterNode.TryParse("node:5672".AsSpan(), CultureInfo.GetCultureInfo("de-DE"), out ClusterNode fromSpan));
        Assert.Equal(fromString, fromSpan);
        Assert.Equal("node:5672", fromSpan.ToString());
        Assert.False(ClusterNode.TryParse((string?)null, CultureInfo.InvariantCulture, out _));
        Assert.Throws<ArgumentNullException>(() => ClusterNode.Parse(null!));
    }
}
