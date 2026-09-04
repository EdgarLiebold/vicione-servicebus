using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a job type instance implementation.
/// </summary>
public class JobTypeInstance
{
    /// <summary>
    /// Gets or sets the updated value.
    /// </summary>
    public DateTimeOffset? Updated { get; set; }
    /// <summary>
    /// Gets or sets the used value.
    /// </summary>
    public DateTimeOffset? Used { get; set; }
    /// <summary>
    /// Gets or sets the properties value.
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
}
