using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Clients;

public sealed class ResponseTests
{
    private static readonly Guid MessageId = Guid.Parse("b4e23586-8caa-478c-87a3-4eb970768239");
    private static readonly Guid RequestId = Guid.Parse("17490dc4-263b-427d-88ec-09f4608ea621");
    private static readonly Guid CorrelationId = Guid.Parse("b0b831dc-a034-4fac-a3f9-aef416094c86");
    private static readonly Guid ConversationId = Guid.Parse("62372c76-f793-4d24-a608-abd4f084bdca");
    private static readonly Guid InitiatorId = Guid.Parse("f11442d6-b7b6-4e29-9518-3be2f294ed2b");
    private static readonly DateTime ExpirationTime = new(2035, 6, 7, 8, 9, 10, DateTimeKind.Utc);
    private static readonly DateTime SentTime = new(2035, 6, 7, 8, 4, 10, DateTimeKind.Utc);
    private static readonly Uri SourceAddress = new("loopback://localhost/source");
    private static readonly Uri DestinationAddress = new("loopback://localhost/destination");
    private static readonly Uri ResponseAddress = new("loopback://localhost/response");
    private static readonly Uri FaultAddress = new("loopback://localhost/fault");
    private static readonly HostInfo Host = new StubHostInfo();

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-WRAPPER", "two-response-second-branch")]
    public async Task TwoResponseWrapper_DelegatesContextAndPreservesBothBranchTasks()
    {
        Task<Response<FirstResponse>> first = Canceled<FirstResponse>();
        var secondResponse = new StubResponse<SecondResponse>(
            new SecondResponse("second"),
            MessageId,
            RequestId,
            CorrelationId);
        Task<Response<SecondResponse>> second = Task.FromResult<Response<SecondResponse>>(secondResponse);
        Response<FirstResponse, SecondResponse> response = (first, second);

        Assert.False(response.Is(out Response<FirstResponse>? firstResult));
        Assert.Null(firstResult);
        Assert.True(response.Is(out Response<SecondResponse>? secondResult));
        Assert.Same(secondResponse, secondResult);
        Assert.True(response.Is<SecondResponse>(out Response<SecondResponse>? genericResult));
        Assert.Same(secondResponse, genericResult);
        Assert.False(response.Is<ThirdResponse>(out Response<ThirdResponse>? missing));
        Assert.Null(missing);
        Assert.Equal(MessageId, response.MessageId);
        Assert.Equal(RequestId, response.RequestId);
        Assert.Equal(CorrelationId, response.CorrelationId);
        Assert.Same(secondResponse.Message, response.Message);
        AssertDelegatesCompleteContext(response, secondResponse);

        (Task<Response<FirstResponse>> firstTask, Task<Response<SecondResponse>> secondTask) = response;
        Assert.Same(first, firstTask);
        Assert.Same(second, secondTask);
        await Assert.ThrowsAsync<TaskCanceledException>(() => firstTask);
        Assert.Same(secondResponse, await secondTask);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-WRAPPER", "three-response-middle-branch")]
    public async Task ThreeResponseWrapper_SelectsOnlyTheCompletedMiddleBranch()
    {
        Task<Response<FirstResponse>> first = Canceled<FirstResponse>();
        var secondResponse = new StubResponse<SecondResponse>(
            new SecondResponse("middle"),
            MessageId,
            RequestId,
            CorrelationId);
        Task<Response<SecondResponse>> second = Task.FromResult<Response<SecondResponse>>(secondResponse);
        Task<Response<ThirdResponse>> third = Canceled<ThirdResponse>();
        Response<FirstResponse, SecondResponse, ThirdResponse> response = (first, second, third);

        Assert.False(response.Is(out Response<FirstResponse>? firstResult));
        Assert.Null(firstResult);
        Assert.True(response.Is(out Response<SecondResponse>? secondResult));
        Assert.Same(secondResponse, secondResult);
        Assert.False(response.Is(out Response<ThirdResponse>? thirdResult));
        Assert.Null(thirdResult);
        Assert.True(response.Is<SecondResponse>(out Response<SecondResponse>? genericResult));
        Assert.Same(secondResponse, genericResult);

        (Task<Response<FirstResponse>> firstTask,
            Task<Response<SecondResponse>> secondTask,
            Task<Response<ThirdResponse>> thirdTask) = response;
        Assert.Same(first, firstTask);
        Assert.Same(second, secondTask);
        Assert.Same(third, thirdTask);
        await Assert.ThrowsAsync<TaskCanceledException>(() => firstTask);
        Assert.Same(secondResponse, await secondTask);
        await Assert.ThrowsAsync<TaskCanceledException>(() => thirdTask);
        AssertDelegatesCompleteContext(response, secondResponse);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-WRAPPER", "multiple-completed-branches-have-deterministic-priority")]
    public void MultipleCompletedBranches_UseTheFirstDeclaredBranchAsTheContextOwner()
    {
        var firstResponse = new StubResponse<FirstResponse>(
            new FirstResponse("first"),
            Guid.Parse("17db0d7b-1be4-45d4-8e30-274f178f7ed6"),
            Guid.Parse("9f6ab08f-f9e1-43fc-b66c-4a29df16c37f"),
            Guid.Parse("77882f37-121b-4624-bdb0-661c22bdff8b"));
        var secondResponse = new StubResponse<SecondResponse>(
            new SecondResponse("second"),
            MessageId,
            RequestId,
            CorrelationId);
        Response<FirstResponse, SecondResponse> response = (
            Task.FromResult<Response<FirstResponse>>(firstResponse),
            Task.FromResult<Response<SecondResponse>>(secondResponse));

        Assert.True(response.Is(out Response<FirstResponse>? first));
        Assert.Same(firstResponse, first);
        Assert.True(response.Is(out Response<SecondResponse>? second));
        Assert.Same(secondResponse, second);
        Assert.Same(firstResponse.Message, response.Message);
        Assert.Equal(firstResponse.MessageId, response.MessageId);
        Assert.NotEqual(secondResponse.MessageId, response.MessageId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-WRAPPER", "no-completed-branch-rejected")]
    public void ResponseWrapperWithoutACompletedBranch_IsRejected()
    {
        Task<Response<FirstResponse>> first = Canceled<FirstResponse>();
        Task<Response<SecondResponse>> second = Canceled<SecondResponse>();
        Task<Response<ThirdResponse>> third = Canceled<ThirdResponse>();

        ArgumentException two = Assert.Throws<ArgumentException>(() =>
            new Response<FirstResponse, SecondResponse>(first, second));
        ArgumentException three = Assert.Throws<ArgumentException>(() =>
            new Response<FirstResponse, SecondResponse, ThirdResponse>(first, second, third));

        Assert.Equal("At least one response must have completed", two.Message);
        Assert.Equal(two.Message, three.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RESPONSE-WRAPPER", "required-response-tasks")]
    public void MissingResponseTasks_AreRejectedAtThePublicConstructorBoundary()
    {
        Task<Response<FirstResponse>> first = Canceled<FirstResponse>();
        Task<Response<SecondResponse>> second = Canceled<SecondResponse>();
        Task<Response<ThirdResponse>> third = Canceled<ThirdResponse>();

        Assert.Equal("response1", Assert.Throws<ArgumentNullException>(() =>
            new Response<FirstResponse, SecondResponse>(null!, second)).ParamName);
        Assert.Equal("response2", Assert.Throws<ArgumentNullException>(() =>
            new Response<FirstResponse, SecondResponse>(first, null!)).ParamName);
        Assert.Equal("response1", Assert.Throws<ArgumentNullException>(() =>
            new Response<FirstResponse, SecondResponse, ThirdResponse>(null!, second, third)).ParamName);
        Assert.Equal("response2", Assert.Throws<ArgumentNullException>(() =>
            new Response<FirstResponse, SecondResponse, ThirdResponse>(first, null!, third)).ParamName);
        Assert.Equal("response3", Assert.Throws<ArgumentNullException>(() =>
            new Response<FirstResponse, SecondResponse, ThirdResponse>(first, second, null!)).ParamName);
    }

    private static void AssertDelegatesCompleteContext(Response actual, Response expected)
    {
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.ConversationId, actual.ConversationId);
        Assert.Equal(expected.InitiatorId, actual.InitiatorId);
        Assert.Equal(expected.ExpirationTime, actual.ExpirationTime);
        Assert.Same(expected.SourceAddress, actual.SourceAddress);
        Assert.Same(expected.DestinationAddress, actual.DestinationAddress);
        Assert.Same(expected.ResponseAddress, actual.ResponseAddress);
        Assert.Same(expected.FaultAddress, actual.FaultAddress);
        Assert.Equal(expected.SentTime, actual.SentTime);
        Assert.Same(expected.Headers, actual.Headers);
        Assert.Same(expected.Host, actual.Host);
        Assert.Same(expected.Message, actual.Message);
    }

    private static Task<Response<T>> Canceled<T>()
        where T : class => Task.FromCanceled<Response<T>>(new CancellationToken(true));

    private sealed record FirstResponse(string Value);

    private sealed record SecondResponse(string Value);

    private sealed record ThirdResponse(string Value);

    private sealed class StubResponse<T>(
        T message,
        Guid messageId,
        Guid requestId,
        Guid correlationId) : Response<T>
        where T : class
    {
        public Guid? MessageId { get; } = messageId;

        public Guid? RequestId { get; } = requestId;

        public Guid? CorrelationId { get; } = correlationId;

        public Guid? ConversationId => ResponseTests.ConversationId;

        public Guid? InitiatorId => ResponseTests.InitiatorId;

        public DateTime? ExpirationTime => ResponseTests.ExpirationTime;

        public Uri? SourceAddress => ResponseTests.SourceAddress;

        public Uri? DestinationAddress => ResponseTests.DestinationAddress;

        public Uri? ResponseAddress => ResponseTests.ResponseAddress;

        public Uri? FaultAddress => ResponseTests.FaultAddress;

        public DateTime? SentTime => ResponseTests.SentTime;

        public Headers Headers => EmptyHeaders.Instance;

        public HostInfo Host => ResponseTests.Host;

        public T Message { get; } = message;

        object Response.Message => Message;
    }

    private sealed class StubHostInfo : HostInfo
    {
        public string MachineName => "response-host";

        public string ProcessName => "response-tests";

        public int ProcessId => 17;

        public string Assembly => "ViciOne.ServiceBus.Abstractions.Tests";

        public string AssemblyVersion => "1.0.0";

        public string FrameworkVersion => ".NET 10";

        public string ViciOneServiceBusVersion => "10.0.0";

        public string OperatingSystemVersion => "test-os";
    }
}
