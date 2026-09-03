#nullable enable

namespace ViciOne.ServiceBus.MessageData;

using System;
using System.Threading;

/// <summary>
/// Per-send proof that the existing bus-owned MessageData transform carried a stored reference. It is
/// deliberately not a repository or policy owner.
/// </summary>
internal sealed class MessageDataAdmissionEvidence
{
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;
    int _storedReferenceCount;

    public MessageDataAdmissionEvidence(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public bool HasStoredReference => Volatile.Read(ref _storedReferenceCount) > 0;

    public void Observe(IMessageDataRepository repository, MessageDataPolicy policy)
    {
        if (!ReferenceEquals(_repository, repository) || !ReferenceEquals(_policy, policy))
            throw new ConfigurationException("A send cannot combine multiple MessageData owners.");

        Interlocked.Increment(ref _storedReferenceCount);
    }
}
