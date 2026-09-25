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

/// <summary>Owns resolved Amazon SQS queue metadata, policy updates, and message batchers.</summary>
public class QueueInfo :
    IAsyncDisposable,
    ViciOne.ServiceBus.Caching.IResourceUsageSource
{
    readonly Lazy<IBatcher<DeleteMessageBatchRequestEntry>> _batchDeleter;
    readonly Lazy<IBatcher<SendMessageBatchRequestEntry>> _batchSender;
    readonly IAmazonSQS _client;
    readonly object _lifecycleLock = new();
    readonly SemaphoreSlim _updateSemaphore;
    TaskCompletionSource? _policyUpdatesDrained;
    int _activePolicyUpdates;
    Task? _disposeTask;
    bool _disposed;

    const string SendMessageIAMActionName = "sqs:SendMessage";

    /// <summary>Initializes resolved queue metadata and lazy send and delete batchers.</summary>
    /// <param name="entityName">The logical queue name.</param>
    /// <param name="url">The Amazon SQS queue URL.</param>
    /// <param name="attributes">The queue attributes, including <c>QueueArn</c>.</param>
    /// <param name="client">The Amazon SQS client used by policy and batch operations.</param>
    /// <param name="cancellationToken">The token used to cancel provider requests issued by lazy batchers.</param>
    /// <param name="existing">Whether the queue existed before it was resolved.</param>
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

    /// <summary>Gets the logical queue name.</summary>
    public string EntityName { get; }
    /// <summary>Gets the Amazon SQS queue URL.</summary>
    public string Url { get; }
    /// <summary>Gets the Amazon SQS queue ARN.</summary>
    public string Arn { get; }
    /// <summary>Gets the queue attributes returned by Amazon SQS.</summary>
    public IDictionary<string, string> Attributes { get; }
    /// <summary>Gets the Amazon SNS subscription ARNs associated with the queue in this context.</summary>
    public IList<string> SubscriptionArns { get; }
    /// <summary>Gets whether the queue existed before it was resolved.</summary>
    public bool Existing { get; }

    /// <summary>Occurs when an operation uses this queue metadata resource.</summary>
    public event Action? Used;

    /// <summary>Disposes the policy-update semaphore and any initialized message batchers.</summary>
    /// <returns>A task that completes when initialized batchers have drained and stopped.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_lifecycleLock)
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    async Task DisposeCoreAsync()
    {
        _disposed = true;

        if (_activePolicyUpdates > 0)
        {
            _policyUpdatesDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await _policyUpdatesDrained.Task.ConfigureAwait(false);
        }

        _updateSemaphore.Dispose();

        if (_batchSender.IsValueCreated)
            await _batchSender.Value.DisposeAsync().ConfigureAwait(false);
        if (_batchDeleter.IsValueCreated)
            await _batchDeleter.Value.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Queues an Amazon SQS send entry and waits for its batch result.</summary>
    /// <param name="entry">The send-message batch entry.</param>
    /// <param name="cancellationToken">The token used to cancel admission to the batch queue.</param>
    /// <returns>A task that completes when Amazon SQS reports the entry result.</returns>
    public Task SendAsync(SendMessageBatchRequestEntry entry, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _batchSender.Value.ExecuteAsync(entry, cancellationToken);
        }
    }

    /// <summary>Queues deletion of a received message and waits for its batch result.</summary>
    /// <param name="receiptHandle">The receipt handle returned for the received message.</param>
    /// <param name="cancellationToken">The token used to cancel admission to the batch queue.</param>
    /// <returns>A task that completes when Amazon SQS reports the deletion result.</returns>
    public Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var entry = new DeleteMessageBatchRequestEntry("", receiptHandle);
            return _batchDeleter.Value.ExecuteAsync(entry, cancellationToken);
        }
    }

    /// <summary>Ensures that the queue policy permits an Amazon SNS topic to send messages.</summary>
    /// <param name="sqsQueueArn">The target Amazon SQS queue ARN.</param>
    /// <param name="topicArn">The permitted Amazon SNS topic ARN.</param>
    /// <param name="cancellationToken">The token used to cancel policy serialization.</param>
    /// <returns><see langword="true"/> when the policy was changed; <see langword="false"/> when permission already existed.</returns>
    public async Task<bool> UpdatePolicyAsync(string sqsQueueArn, string topicArn, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activePolicyUpdates++;
        }

        try
        {
            await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await UpdatePolicyCoreAsync(sqsQueueArn, topicArn, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }
        finally
        {
            lock (_lifecycleLock)
            {
                if (--_activePolicyUpdates == 0)
                    _policyUpdatesDrained?.TrySetResult();
            }
        }
    }

    async Task<bool> UpdatePolicyCoreAsync(string sqsQueueArn, string topicArn, CancellationToken cancellationToken)
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
