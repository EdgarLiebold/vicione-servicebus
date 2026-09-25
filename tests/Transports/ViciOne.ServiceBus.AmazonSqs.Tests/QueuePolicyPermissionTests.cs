using System.Net;
using Amazon.Auth.AccessControlPolicy;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class QueuePolicyPermissionTests
{
    private const string QueueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
    private const string QueueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
    private const string TopicArn = "arn:aws:sns:eu-central-1:123456789012:events";

    [Theory]
    [InlineData(false, "sqs:ReceiveMessage", "sns.amazonaws.com")]
    [InlineData(true, "sqs:ReceiveMessage", "sns.amazonaws.com")]
    [InlineData(true, "sqs:SendMessage", "other.amazonaws.com")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "unrelated-policy-condition-does-not-grant-sns-send")]
    public async Task UnrelatedMatchingCondition_StillAddsSnsSendAllowAsync(
        bool allow, string action, string principal)
    {
        Policy existing = PolicyWithStatement(allow, action, principal, TopicArn);
        string existingJson = existing.ToJson();
        string? submittedJson = null;
        var writes = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => CaptureWriteAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        await using var queue = NewQueue(client, existingJson);

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.True(changed);
        Assert.Equal(1, writes);
        Assert.NotNull(submittedJson);
        Assert.Equal(submittedJson, queue.Attributes[QueueAttributeName.Policy]);
        Policy submitted = Policy.FromJson(submittedJson);
        Assert.Equal(2, submitted.Statements.Count);
        Assert.Contains(submitted.Statements, statement =>
            statement.Effect == (allow ? Statement.StatementEffect.Allow : Statement.StatementEffect.Deny)
            && statement.Actions.Any(item => item.ActionName == action)
            && statement.Principals.Any(item => item.Id == principal));
        AssertDedicatedSnsAllow(submitted.Statements[1]);

        Task<SetQueueAttributesResponse> CaptureWriteAsync(object?[]? args)
        {
            writes++;
            Assert.NotNull(args);
            Assert.Equal(QueueUrl, Assert.IsType<string>(args[0]));
            var attributes = Assert.IsType<Dictionary<string, string>>(args[1]);
            submittedJson = attributes[QueueAttributeName.Policy];
            return Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }

    [Theory]
    [InlineData("sqs:SendMessage", "ArnLike", TopicArn)]
    [InlineData("sqs:*", "ArnLike", TopicArn)]
    [InlineData("*", "ArnLike", TopicArn)]
    [InlineData("sqs:SendMessage", "ArnNotEquals", "arn:aws:sns:eu-central-1:123456789012:other")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "explicit-deny-rejects-ineffective-allow")]
    public async Task ExplicitSnsSendDeny_RejectsAnIneffectivePolicyUpdateAsync(
        string action, string conditionType, string conditionArn)
    {
        Policy policy = PolicyWithStatement(false, action, "sns.amazonaws.com", conditionArn);
        policy.Statements[0].Conditions[0].Type = conditionType;
        string existingJson = policy.ToJson();
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            throw new NotSupportedException($"Unexpected policy rewrite: {method.Name}"));
        await using var queue = NewQueue(client, existingJson);

        AmazonSqsTransportException error = await Assert.ThrowsAsync<AmazonSqsTransportException>(
            () => queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken));

        Assert.Contains("explicit Deny", error.Message, StringComparison.Ordinal);
        Assert.Contains("may block", error.Message, StringComparison.Ordinal);
        Assert.Contains(TopicArn, error.Message, StringComparison.Ordinal);
        Assert.Equal(existingJson, queue.Attributes[QueueAttributeName.Policy]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "negative-source-arn-deny-excludes-target-topic")]
    public async Task DenyForEveryOtherTopic_DoesNotBlockThisTopicAsync()
    {
        Policy policy = PolicyWithStatement(false, "sqs:SendMessage", "sns.amazonaws.com", TopicArn);
        policy.Statements[0].Conditions[0].Type = "ArnNotEquals";
        string? submittedJson = null;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => CaptureWriteAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        await using var queue = NewQueue(client, policy.ToJson());

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.True(changed);
        Policy submitted = Policy.FromJson(Assert.IsType<string>(submittedJson));
        Assert.Equal(2, submitted.Statements.Count);
        Assert.Equal(Statement.StatementEffect.Deny, submitted.Statements[0].Effect);
        Assert.Equal("ArnNotEquals", Assert.Single(submitted.Statements[0].Conditions).Type);
        AssertDedicatedSnsAllow(submitted.Statements[1]);

        Task<SetQueueAttributesResponse> CaptureWriteAsync(object?[]? args)
        {
            Assert.NotNull(args);
            Assert.Equal(QueueUrl, Assert.IsType<string>(args[0]));
            submittedJson = Assert.IsType<Dictionary<string, string>>(args[1])[QueueAttributeName.Policy];
            return Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }

    [Theory]
    [InlineData(TopicArn)]
    [InlineData("arn:aws:sns:eu-central-1:123456789012:*")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "existing-sns-send-allow-is-idempotent")]
    public async Task ExistingSnsSendAllow_DoesNotRewritePolicyAsync(string sourceArn)
    {
        string existingJson = PolicyWithStatement(true, "sqs:SendMessage", "sns.amazonaws.com", sourceArn).ToJson();
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            throw new NotSupportedException($"Unexpected policy rewrite: {method.Name}"));
        await using var queue = NewQueue(client, existingJson);

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.Equal(existingJson, queue.Attributes[QueueAttributeName.Policy]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "unrestricted-existing-allow-is-preserved")]
    public async Task UnrestrictedExistingAllow_IsNotNarrowedByAddingAConditionAsync()
    {
        Policy policy = PolicyWithStatement(true, "sqs:SendMessage", "sns.amazonaws.com", TopicArn);
        policy.Statements[0].Conditions.Clear();
        string existingJson = policy.ToJson();
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            throw new NotSupportedException($"Unexpected policy rewrite: {method.Name}"));
        await using var queue = NewQueue(client, existingJson);

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.Equal(existingJson, queue.Attributes[QueueAttributeName.Policy]);
        Assert.Empty(Policy.FromJson(queue.Attributes[QueueAttributeName.Policy]).Statements[0].Conditions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "other-policy-conditions-remain-isolated")]
    public async Task AdditionalRestriction_DoesNotLeakIntoTheNewSnsAllowAsync()
    {
        Policy existing = PolicyWithStatement(true, "sqs:SendMessage", "sns.amazonaws.com", TopicArn);
        existing.Statements[0].Conditions.Add(new Condition
        {
            Type = "StringEquals",
            ConditionKey = "aws:SourceAccount",
            Values = ["000000000000"]
        });
        string? submittedJson = null;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => CaptureWriteAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        await using var queue = NewQueue(client, existing.ToJson());

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.True(changed);
        Policy submitted = Policy.FromJson(Assert.IsType<string>(submittedJson));
        Assert.Equal(2, submitted.Statements.Count);
        Assert.Equal(2, submitted.Statements[0].Conditions.Count);
        AssertDedicatedSnsAllow(submitted.Statements[1]);
        Assert.Equal(submittedJson, queue.Attributes[QueueAttributeName.Policy]);

        Task<SetQueueAttributesResponse> CaptureWriteAsync(object?[]? args)
        {
            Assert.NotNull(args);
            Assert.Equal(QueueUrl, Assert.IsType<string>(args[0]));
            submittedJson = Assert.IsType<Dictionary<string, string>>(args[1])[QueueAttributeName.Policy];
            return Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "arn-equals-sns-allow-is-idempotent")]
    public async Task ExistingArnEqualsAllow_DoesNotRewritePolicyAsync()
    {
        Policy policy = PolicyWithStatement(true, "sqs:SendMessage", "sns.amazonaws.com", TopicArn);
        policy.Statements[0].Conditions[0].Type = "ArnEquals";
        string existingJson = policy.ToJson();
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            throw new NotSupportedException($"Unexpected policy rewrite: {method.Name}"));
        await using var queue = NewQueue(client, existingJson);

        bool changed = await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken);

        Assert.False(changed);
        Assert.Equal(existingJson, queue.Attributes[QueueAttributeName.Policy]);
    }

    [Theory]
    [InlineData("ArnLike")]
    [InlineData("ArnEquals")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "dedicated-allow-reuses-one-statement-for-multiple-topics")]
    public async Task DedicatedSnsAllow_AddsAnotherTopicWithoutGrowingStatementCountAsync(string conditionType)
    {
        const string secondTopicArn = "arn:aws:sns:eu-central-1:123456789012:billing";
        Policy existing = PolicyWithStatement(true, "sqs:SendMessage", "sns.amazonaws.com", TopicArn);
        existing.Statements[0].Conditions[0].Type = conditionType;
        string existingJson = existing.ToJson();
        var writes = 0;
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => CaptureWriteAsync(args),
            _ => throw new NotSupportedException(method.Name)
        });
        await using var queue = NewQueue(client, existingJson);

        Assert.True(await queue.UpdatePolicyAsync(QueueArn, secondTopicArn, TestContext.Current.CancellationToken));
        Assert.False(await queue.UpdatePolicyAsync(QueueArn, secondTopicArn, TestContext.Current.CancellationToken));
        Assert.False(await queue.UpdatePolicyAsync(QueueArn, TopicArn, TestContext.Current.CancellationToken));

        Assert.Equal(1, writes);
        Statement onlyStatement = Assert.Single(Policy.FromJson(queue.Attributes[QueueAttributeName.Policy]).Statements);
        Assert.Equal(Statement.StatementEffect.Allow, onlyStatement.Effect);
        Assert.Equal("sqs:SendMessage", Assert.Single(onlyStatement.Actions).ActionName);
        Assert.Equal(QueueArn, Assert.Single(onlyStatement.Resources).Id);
        Principal principal = Assert.Single(onlyStatement.Principals);
        Assert.Equal("Service", principal.Provider);
        Assert.Equal("sns.amazonaws.com", principal.Id);
        Condition condition = Assert.Single(onlyStatement.Conditions);
        Assert.Equal(conditionType, condition.Type);
        Assert.Equal(ConditionFactory.SOURCE_ARN_CONDITION_KEY, condition.ConditionKey);
        Assert.Equal([TopicArn, secondTopicArn], condition.Values);

        Task<SetQueueAttributesResponse> CaptureWriteAsync(object?[]? args)
        {
            writes++;
            Assert.NotNull(args);
            Assert.Equal(QueueUrl, Assert.IsType<string>(args[0]));
            Assert.Contains(secondTopicArn, Assert.IsType<Dictionary<string, string>>(args[1])[QueueAttributeName.Policy], StringComparison.Ordinal);
            return Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }

    private static QueueInfo NewQueue(IAmazonSQS client, string policy) => new(
        "orders", QueueUrl,
        new Dictionary<string, string>
        {
            [QueueAttributeName.QueueArn] = QueueArn,
            [QueueAttributeName.Policy] = policy
        },
        client, CancellationToken.None, true);

    private static void AssertDedicatedSnsAllow(Statement statement)
    {
        Assert.Equal(Statement.StatementEffect.Allow, statement.Effect);
        Assert.Equal("sqs:SendMessage", Assert.Single(statement.Actions).ActionName);
        Assert.Equal(QueueArn, Assert.Single(statement.Resources).Id);
        Principal principal = Assert.Single(statement.Principals);
        Assert.Equal("Service", principal.Provider);
        Assert.Equal("sns.amazonaws.com", principal.Id);
        Condition condition = Assert.Single(statement.Conditions);
        Assert.Equal(ConditionFactory.ArnComparisonType.ArnLike.ToString(), condition.Type);
        Assert.Equal(ConditionFactory.SOURCE_ARN_CONDITION_KEY, condition.ConditionKey);
        Assert.Equal(TopicArn, Assert.Single(condition.Values));
    }

    private static Policy PolicyWithStatement(bool allow, string action, string principal, string sourceArn)
    {
        var policy = new Policy();
        var statement = new Statement(allow ? Statement.StatementEffect.Allow : Statement.StatementEffect.Deny);
        statement.Actions.Add(action);
        statement.Resources.Add(new Resource(QueueArn));
        statement.Principals.Add(new Principal("Service", principal));
        statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(sourceArn));
        policy.Statements.Add(statement);
        return policy;
    }
}
