using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus;

public class JobTypeInstance
{
    public DateTimeOffset? Updated { get; set; }
    public DateTimeOffset? Used { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
