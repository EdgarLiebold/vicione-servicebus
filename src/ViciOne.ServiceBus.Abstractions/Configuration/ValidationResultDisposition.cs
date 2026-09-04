using System;

namespace ViciOne.ServiceBus;

[Serializable]
public enum ValidationResultDisposition
{
    Success,
    Warning,
    Failure,
}
