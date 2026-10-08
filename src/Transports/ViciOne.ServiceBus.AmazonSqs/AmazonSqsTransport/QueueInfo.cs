using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        : this(entityName, url, attributes, client, cancellationToken, existing, TimeProvider.System)
    {
    }

    internal QueueInfo(string entityName, string url, IDictionary<string, string> attributes, IAmazonSQS client,
        CancellationToken cancellationToken, bool existing, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _client = client;
        Attributes = attributes;
        Existing = existing;
        EntityName = entityName;
        Url = url;

        Arn = attributes.TryGetValue(QueueAttributeName.QueueArn, out var queueArn)
            ? queueArn
            : throw new ArgumentException($"The queueArn was not found: {url}", nameof(attributes));

        _updateSemaphore = new SemaphoreSlim(1);

        _batchSender = new Lazy<IBatcher<SendMessageBatchRequestEntry>>(() => new SendBatcher(client, url, cancellationToken, timeProvider));
        _batchDeleter = new Lazy<IBatcher<DeleteMessageBatchRequestEntry>>(() => new DeleteBatcher(client, url, cancellationToken, timeProvider));

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
        => TrySendAsync(entry, cancellationToken) ?? throw new ObjectDisposedException(nameof(QueueInfo));

    internal Task? TrySendAsync(SendMessageBatchRequestEntry entry, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        lock (_lifecycleLock)
        {
            return _disposed ? null : _batchSender.Value.ExecuteAsync(entry, cancellationToken);
        }
    }

    /// <summary>Queues deletion of a received message and waits for its batch result.</summary>
    /// <param name="receiptHandle">The receipt handle returned for the received message.</param>
    /// <param name="cancellationToken">The token used to cancel admission to the batch queue.</param>
    /// <returns>A task that completes when Amazon SQS reports the deletion result.</returns>
    public Task DeleteAsync(string receiptHandle, CancellationToken cancellationToken)
        => TryDeleteAsync(receiptHandle, cancellationToken) ?? throw new ObjectDisposedException(nameof(QueueInfo));

    internal Task? TryDeleteAsync(string receiptHandle, CancellationToken cancellationToken)
    {
        Used?.Invoke();
        lock (_lifecycleLock)
        {
            if (_disposed)
                return null;

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

        if (policy.Statements.Any(statement => statement.Effect == Statement.StatementEffect.Deny
            && DenyMayBlockSnsSend(statement, sqsQueueArn, topicArn)))
            throw new AmazonSqsTransportException($"The queue policy contains an explicit Deny that may block Amazon SNS topic '{topicArn}' from sending to '{sqsQueueArn}'.");

        if (QueueHasTopicPermission(policy, topicArn, sqsQueueArn))
            return false;

        var reusable = policy.Statements.FirstOrDefault(statement => IsDedicatedSnsAllow(statement, sqsQueueArn));
        if (reusable is not null)
        {
            var sourceArns = reusable.Conditions[0].Values;
            reusable.Conditions[0].Values = sourceArns.Append(topicArn).Distinct(StringComparer.Ordinal).ToArray();
        }
        else
        {
            // Never carry unrelated conditions or additional permissions into the new statement.
            var statement = new Statement(Statement.StatementEffect.Allow);
            statement.Actions.Add(SendMessageIAMActionName);
            statement.Resources.Add(new Resource(sqsQueueArn));
            statement.Principals.Add(new Principal("Service", "sns.amazonaws.com"));
            statement.Conditions.Add(ConditionFactory.NewSourceArnCondition(topicArn));
            policy.Statements.Add(statement);
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
        return policy.Statements.Any(statement =>
            statement.Effect == Statement.StatementEffect.Allow
            && StatementAppliesToSnsSend(statement, sqsQueueArn)
            && SourceArnConditionMatches(statement, topicArn));
    }

    static bool StatementAppliesToSnsSend(Statement statement, string sqsQueueArn) =>
        statement.Actions.Any(action => action.ActionName.Equals(SendMessageIAMActionName, StringComparison.Ordinal))
        && statement.Resources.Any(resource => resource.Id.Equals(sqsQueueArn, StringComparison.Ordinal))
        && statement.Principals.Any(principal =>
            string.Equals(principal.Provider, "Service", StringComparison.OrdinalIgnoreCase)
            && string.Equals(principal.Id, "sns.amazonaws.com", StringComparison.OrdinalIgnoreCase));

    static bool IsDedicatedSnsAllow(Statement statement, string sqsQueueArn) =>
        statement.Effect == Statement.StatementEffect.Allow
        && statement.Actions.Count == 1
        && statement.Actions[0].ActionName == SendMessageIAMActionName
        && statement.Resources.Count == 1
        && statement.Resources[0].Id == sqsQueueArn
        && statement.Principals.Count == 1
        && string.Equals(statement.Principals[0].Provider, "Service", StringComparison.OrdinalIgnoreCase)
        && string.Equals(statement.Principals[0].Id, "sns.amazonaws.com", StringComparison.OrdinalIgnoreCase)
        && statement.Conditions.Count == 1
        && string.Equals(statement.Conditions[0].ConditionKey, ConditionFactory.SOURCE_ARN_CONDITION_KEY, StringComparison.OrdinalIgnoreCase)
        && IsPositiveArnComparison(statement.Conditions[0].Type);

    static bool DenyMayBlockSnsSend(Statement statement, string sqsQueueArn, string topicArn) =>
        statement.Actions.Any(action => PolicyPatternMatches(action.ActionName, SendMessageIAMActionName, true))
        && statement.Resources.Any(resource => PolicyPatternMatches(resource.Id, sqsQueueArn, false))
        && statement.Principals.Any(principal =>
            principal.Id == "*"
            || (string.Equals(principal.Provider, "Service", StringComparison.OrdinalIgnoreCase)
                && PolicyPatternMatches(principal.Id, "sns.amazonaws.com", true)))
        && DenyConditionsMayMatch(statement, topicArn);

    static bool DenyConditionsMayMatch(Statement statement, string topicArn)
    {
        if (statement.Conditions.Count == 0)
            return true;
        if (statement.Conditions.Count != 1)
            return true; // Unknown combinations must not be reported as an effective permission.

        var condition = statement.Conditions[0];
        if (!string.Equals(condition.ConditionKey, ConditionFactory.SOURCE_ARN_CONDITION_KEY, StringComparison.OrdinalIgnoreCase))
            return true;

        if (IsPositiveArnComparison(condition.Type))
            return condition.Values.Any(value => PolicyPatternMatches(value, topicArn, false));

        if (string.Equals(condition.Type, "ArnNotEquals", StringComparison.OrdinalIgnoreCase)
            || string.Equals(condition.Type, "ArnNotLike", StringComparison.OrdinalIgnoreCase))
            return !condition.Values.Any(value => PolicyPatternMatches(value, topicArn, false));

        return true;
    }

    static bool PolicyPatternMatches(string pattern, string value, bool ignoreCase)
    {
        var expression = "\\A" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "\\z";
        return Regex.IsMatch(value, expression,
            RegexOptions.CultureInvariant | RegexOptions.Singleline | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None),
            TimeSpan.FromMilliseconds(100));
    }

    static bool IsPositiveArnComparison(string type) =>
        string.Equals(type, ConditionFactory.ArnComparisonType.ArnLike.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "ArnEquals", StringComparison.OrdinalIgnoreCase);

    static bool SourceArnConditionMatches(Statement statement, string topicArn)
    {
        if (statement.Conditions.Count == 0)
            return true;
        if (statement.Conditions.Count != 1)
            return false;

        var condition = statement.Conditions[0];
        var topicArnPattern = topicArn[..(topicArn.LastIndexOf(':') + 1)] + "*";
        return string.Equals(condition.ConditionKey, ConditionFactory.SOURCE_ARN_CONDITION_KEY, StringComparison.OrdinalIgnoreCase)
            && IsPositiveArnComparison(condition.Type)
            && condition.Values.Any(value => value == topicArnPattern || value == topicArn);
    }
}
