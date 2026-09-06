using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Represents an instance of job type.</summary>
public class JobTypeInstance
{
    /// <summary>Gets or sets the updated.</summary>
    public DateTimeOffset? Updated { get; set; }
    /// <summary>Gets or sets the used.</summary>
    public DateTimeOffset? Used { get; set; }
    /// <summary>Gets or sets the properties.</summary>
    public Dictionary<string, object>? Properties { get; set; }
}
