namespace ViciOne.ServiceBus.Tests.Infrastructure.Requirements;

/// <summary>
/// Identifies the single requirement variant proved by a test method.
/// </summary>
/// <remarks>
/// This attribute is passive metadata. It does not observe execution, collect results, or own a
/// verdict; xUnit and Microsoft Testing Platform remain the only test and process verdict owners.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RequirementCoverageAttribute : Attribute
{
    public RequirementCoverageAttribute(string requirementId, string variantKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variantKey);

        RequirementId = requirementId;
        VariantKey = variantKey;
    }

    public string RequirementId { get; }

    public string VariantKey { get; }
}
