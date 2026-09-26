using System.Net;
using global::Amazon.Runtime;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsBatchIdentityTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] Bodies = ["first", "second", "third", "fourth"];
    private const string QueueUrl = "http://127.0.0.1/queue/orders";

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-BATCH", "mixed-reordered-outcomes-preserve-caller-identity")]
    public async Task MixedReorderedResponse_PreservesEveryCallersOwnOutcomeAsync()
    {
        using var owner = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        using var client = new ResponseClient(ResponseKind.Mixed);
        var batcher = new SendBatcher(client, QueueUrl, owner.Token, new FourEntrySettings());
        try
        {
            Task[] calls = Bodies.Select(body => batcher.ExecuteAsync(new SendMessageBatchRequestEntry("", body), caller.Token)).ToArray();

            await calls[0].WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
            await calls[2].WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
            AmazonSqsTransportException second = await Assert.ThrowsAsync<AmazonSqsTransportException>(() =>
                calls[1].WaitAsync(WaitTimeout, TestContext.Current.CancellationToken));
            AmazonSqsTransportException fourth = await Assert.ThrowsAsync<AmazonSqsTransportException>(() =>
                calls[3].WaitAsync(WaitTimeout, TestContext.Current.CancellationToken));

            Assert.Equal("Send failed: SecondCode-second was rejected", second.Message);
            Assert.Equal("Send failed: FourthCode-fourth was rejected", fourth.Message);
            Assert.NotSame(second, fourth);
            Assert.True(calls[0].IsCompletedSuccessfully);
            Assert.True(calls[2].IsCompletedSuccessfully);
            Assert.True(calls[1].IsFaulted);
            Assert.True(calls[3].IsFaulted);
            AssertRequest(client, owner.Token, caller.Token);
        }
        finally
        {
            await batcher.DisposeAsync().AsTask().WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(ResponseKind.DuplicateSuccess)]
    [InlineData(ResponseKind.DuplicateFailure)]
    [InlineData(ResponseKind.OverlappingOutcome)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-BATCH", "duplicate-outcome-rejects-whole-batch-before-completion")]
    public async Task DuplicateResponseIdentity_FaultsEveryCallerBeforeAnyPartialCompletionAsync(ResponseKind kind)
    {
        using var owner = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        using var client = new ResponseClient(kind);
        var batcher = new SendBatcher(client, QueueUrl, owner.Token, new FourEntrySettings());
        try
        {
            Task[] calls = Bodies.Select(body => batcher.ExecuteAsync(new SendMessageBatchRequestEntry("", body), caller.Token)).ToArray();
            AmazonSqsTransportException? first = null;

            foreach (Task call in calls)
            {
                AmazonSqsTransportException failure = await Assert.ThrowsAsync<AmazonSqsTransportException>(() =>
                    call.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken));
                Assert.Equal($"The AWS batch response contains entry id '{client.DuplicateId}' more than once.", failure.Message);
                Assert.True(call.IsFaulted);
                if (first is null)
                    first = failure;
                else
                    Assert.Same(first, failure);
            }

            Assert.NotNull(client.DuplicateId);
            AssertRequest(client, owner.Token, caller.Token);
        }
        finally
        {
            await batcher.DisposeAsync().AsTask().WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        }
    }

    private static void AssertRequest(ResponseClient client, CancellationToken owner, CancellationToken caller)
    {
        Assert.Equal(1, client.CallCount);
        Assert.Equal(QueueUrl, client.Queue);
        Assert.Equal(Bodies, client.Entries.Select(entry => entry.Body));
        Assert.Equal(4, client.Entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(client.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Id)));
        Assert.NotEqual(owner, caller);
        Assert.Equal(owner, client.Token);
    }

    public enum ResponseKind
    {
        Mixed,
        DuplicateSuccess,
        DuplicateFailure,
        OverlappingOutcome,
    }

    private sealed class FourEntrySettings : BatchSettings
    {
        public int MessageLimit => 4;
        public int BatchLimit => 1;
        public int SizeLimit => 256 * 1024;
        public TimeSpan Timeout => TimeSpan.FromMinutes(1);
    }

    private sealed class ResponseClient(ResponseKind kind) : AmazonSQSClient(
        new AnonymousAWSCredentials(),
        new AmazonSQSConfig { ServiceURL = "http://127.0.0.1:1", AuthenticationRegion = "eu-central-1" })
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);
        public string? Queue { get; private set; }
        public CancellationToken Token { get; private set; }
        public (string Id, string Body)[] Entries { get; private set; } = [];
        public string? DuplicateId { get; private set; }

        public override Task<SendMessageBatchResponse> SendMessageBatchAsync(
            SendMessageBatchRequest request, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            Queue = request.QueueUrl;
            Token = cancellationToken;
            Entries = request.Entries.Select(entry => (entry.Id, entry.MessageBody)).ToArray();
            string Id(string body) => request.Entries.Single(entry => entry.MessageBody == body).Id;
            var response = new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = [new() { Id = Id("third") }, new() { Id = Id("first") }],
                Failed =
                [
                    new() { Id = Id("fourth"), Code = "FourthCode", Message = "fourth was rejected" },
                    new() { Id = Id("second"), Code = "SecondCode", Message = "second was rejected" },
                ],
            };
            if (kind == ResponseKind.DuplicateSuccess)
            {
                DuplicateId = Id("first");
                response.Successful.Add(new SendMessageBatchResultEntry { Id = DuplicateId });
            }
            else if (kind is ResponseKind.DuplicateFailure or ResponseKind.OverlappingOutcome)
            {
                DuplicateId = Id(kind == ResponseKind.DuplicateFailure ? "fourth" : "first");
                response.Failed.Add(new BatchResultErrorEntry { Id = DuplicateId, Code = "Duplicate", Message = "contradictory outcome" });
            }

            return Task.FromResult(response);
        }
    }
}
