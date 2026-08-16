#nullable enable
namespace ViciOne.ServiceBus;

using System;
using System.Collections.Generic;


public class JobTypeInstance
{
    public DateTime? Updated { get; set; }
    public DateTime? Used { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
