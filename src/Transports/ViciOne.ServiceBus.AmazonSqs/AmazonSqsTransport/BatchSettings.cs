using System;

namespace ViciOne.ServiceBus.AmazonSqs;

interface BatchSettings
{
    int MessageLimit { get; }
    int BatchLimit { get; }
    int SizeLimit { get; }
    TimeSpan Timeout { get; }
}
