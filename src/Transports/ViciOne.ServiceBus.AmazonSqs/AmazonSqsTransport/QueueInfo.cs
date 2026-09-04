using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using Amazon.Auth.AccessControlPolicy;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a queue info implementation.
/// </summary>
public class QueueInfo :
    IAsyncDisposable,
    ViciOne.ServiceBus.Caching.IResourceUsageSource
{
    readonly Lazy<IBatcher<DeleteMessageBatchRequestEntry>> _batchDeleter;
    readonly Lazy<IBatcher<SendMessageBatchRequestEntry>> _batchSender;
    readonly IAmazonSQS _client;
    readonly SemaphoreSlim _updateSemaphore;
    bool _disposed;

    const string SendMessageIAMActionName = "sqs:SendMessage";

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityName">The entity name value.</param>
    /// <param name="url">The url value.</param>
    /// <param name="attributes">The attributes value.</param>
    /// <param name="client">The client value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="existing">The existing value.</param>
    public QueueInfo(string entityName, string url, IDictionary<string, string> attributes, IAmazonSQS client, CancellationToken cancellationToken,
        bool existing)
    {
        _client = client;
        Attributes = attributes;
        Existing = existing;
        EntityName = entityName;
        Url = url;

        Arn = attributes.TryGetValue(QueueAttributeName.QueueArn, out var queueArn)
            ? queueArn
            : throw new ArgumentException($"The queueArn was not found: {url}", nameof(attributes));

        _updateSemaphore = new SemaphoreSlim(1);

        _batchSender = new Lazy<IBatcher<SendMessageBatchRequestEntry>>(() => new SendBatcher(client, url, cancellationToken));
        _batchDeleter = new Lazy<IBatcher<DeleteMessageBatchRequestEntry>>(() => new DeleteBatcher(client, url, cancellationToken));

        SubscriptionArns = new List<string>();
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public string EntityName { get; }
    /// <summary>
    /// Gets the url value.
    /// </summary>
    public string Url { get; }
    /// <summary>
    /// Gets the arn value.
    /// </summary>
    public string Arn { get; }
    /// <summary>
    /// Gets the attributes value.
    /// </summary>
    public IDictionary<string, string> Attributes { get; }
    /// <summary>
    /// Gets the subscription arns value.
    /// </summary>
    public IList<string> SubscriptionArns { get; }
    /// <summary>
    /// Gets the existing value.
    /// </summary>
    public bool Existing { get; }

    /// <summary>
    /// Occurs when used.
    /// </summary>
    public event Action? Used;

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _updateSemaphore.Dispose();

        if (_batchSender.IsValueCreated)
            await _batchSender.Value.DisposeAsync().ConfigureAwait(false);
        if (_batchDeleter.IsValueCreated)
            await _batchDeleter.Value.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendMessageBatchRequestEntry entry, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        return _batchSender.Value.ExecuteAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        var entry = new DeleteMessageBatchRequestEntry("", receiptHandle);

        return _batchDeleter.Value.ExecuteAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Performs the update policy operation.
    /// </summary>
    /// <param name="sqsQueueArn">The sqs queue arn value.</param>
    /// <param name="topicArn">The topic arn value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<bool> UpdatePolicyAsync(string sqsQueueArn, string topicArn, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Attributes.TryGetValue(QueueAttributeName.Policy, out var policyValue);
            var policy = string.IsNullOrEmpty(policyValue)
                ? new Policy()
                : Policy.FromJson(policyValue);

            if (QueueHasTopicPermission(policy, topicArn, sqsQueueArn))
                return false;

            var statement = policy.Statements.FirstOrDefault(x => x.Effect == Statement.StatementEffect.Allow
                && x.Actions.Any(a => a.ActionName.Equals(SendMessageIAMActionName, StringComparison.Ordinal))
                && x.Resources.Any(a => a.Id.Equals(sqsQueueArn, StringComparison.OrdinalIgnoreCase))
                && x.Principals.Any(a => string.Equals(a.Provider, "Service", StringComparison.OrdinalIgnoreCase)
                    && a.Id.Equals("sns.amazonaws.com", StringComparison.OrdinalIgnoreCase)));

            if (statement is null)
            {
                statement = new Statement(Statement.StatementEffect.Allow);
                statement.Actions.Add(SendMessageIAMActionName);
                statement.Resources.Add(new Resource(sqsQueueArn));
                statement.Principals.Add(new Principal("Service", "sns.amazonaws.com"));
                policy.Statements.Add(statement);
            }
            var condition = statement.Conditions.FirstOrDefault(x =>
                string.Equals(ConditionFactory.SOURCE_ARN_CONDITION_KEY, x.ConditionKey, StringComparison.Ordinal) &&
                x.Type.Equals(ConditionFactory.ArnComparisonType.ArnLike.ToString(), StringComparison.Ordinal));

            if (condition is not null && condition.Values.Any(x => x.Equals(topicArn, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (condition is null)
                statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(topicArn));
            else
            {
                condition.Values = condition
                    .Values
                    .Append(topicArn)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            var jsonPolicy = policy.ToJson();

            var setAttributes = new Dictionary<string, string> { { QueueAttributeName.Policy, jsonPolicy } };
            var setAttributesResponse = await _client.SetQueueAttributesAsync(Url, setAttributes, cancellationToken).ConfigureAwait(false);

            setAttributesResponse.EnsureSuccessfulResponse();

            Attributes[QueueAttributeName.Policy] = jsonPolicy;

            return true;
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    static bool QueueHasTopicPermission(Policy policy, string topicArn, string sqsQueueArn)
    {
        var topicArnPattern = topicArn.Substring(0, topicArn.LastIndexOf(':') + 1) + "*";

        IEnumerable<Condition> conditions = policy.Statements
            .Where(s => s.Resources.Any(r => r.Id.Equals(sqsQueueArn)))
            .SelectMany(s => s.Conditions);

        return conditions.Any(c =>
            string.Equals(c.Type, ConditionFactory.ArnComparisonType.ArnLike.ToString(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ConditionKey, ConditionFactory.SOURCE_ARN_CONDITION_KEY, StringComparison.OrdinalIgnoreCase) &&
            c.Values.Any(v => v == topicArnPattern || v == topicArn));
    }
}
