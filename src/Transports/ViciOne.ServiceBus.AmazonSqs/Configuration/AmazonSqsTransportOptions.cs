namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines named AWS region and entity-scope options for the Amazon SQS transport.</summary>
public sealed class AmazonSqsTransportOptions
{
    /// <summary>Gets or sets the AWS region system name.</summary>
    public string? Region { get; set; }
    /// <summary>Gets or sets the entity-name prefix applied within the host.</summary>
    public string? Scope { get; set; }
}
