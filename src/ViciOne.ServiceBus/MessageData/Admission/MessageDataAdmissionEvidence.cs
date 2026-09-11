using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageData.Admission;

/// <summary>Records that a send produced repository-backed message data under one repository and policy owner.</summary>
internal sealed class MessageDataAdmissionEvidence
{
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;
    int _storedReferenceCount;

    /// <summary>Creates evidence owned by one repository and policy pair.</summary>
    /// <param name="repository">The repository that owns stored references.</param>
    /// <param name="policy">The policy applied when the references were stored.</param>
    public MessageDataAdmissionEvidence(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>Gets whether at least one stored reference has been observed.</summary>
    public bool HasStoredReference => Volatile.Read(ref _storedReferenceCount) > 0;

    /// <summary>Records a stored reference after verifying that it has the same owner.</summary>
    /// <param name="repository">The repository that produced the reference.</param>
    /// <param name="policy">The policy used to produce the reference.</param>
    public void Observe(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);

        if (!ReferenceEquals(_repository, repository) || !ReferenceEquals(_policy, policy))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Message data", "unknown", "A send cannot combine multiple MessageData owners.", "Correct the named configuration before starting the host"));

        Interlocked.Increment(ref _storedReferenceCount);
    }
}
