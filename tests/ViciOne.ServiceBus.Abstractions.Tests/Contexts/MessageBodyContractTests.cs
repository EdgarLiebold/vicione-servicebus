using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

/// <summary>Verifies the public body contract and every implementation owned by Abstractions.</summary>
public sealed class MessageBodyContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "greenfield-readable-shape")]
    public void PublicContract_IsAlwaysReadableAndHasExplicitRepresentations()
    {
        Type contract = typeof(MessageBody);

        Assert.Equal(typeof(long), contract.GetProperty(nameof(MessageBody.Length))?.PropertyType);
        Assert.Equal(typeof(byte[]), contract.GetMethod(nameof(MessageBody.ToArray), Type.EmptyTypes)?.ReturnType);
        Assert.Equal(typeof(Stream), contract.GetMethod("OpenReadStream", Type.EmptyTypes)?.ReturnType);
        MethodInfo transportTextMethod = Assert.Single(contract.GetMethods(), method =>
            method.Name == nameof(MessageBody.TryGetTransportText));
        Assert.Equal(typeof(bool), transportTextMethod.ReturnType);
        ParameterInfo transportTextParameter = Assert.Single(transportTextMethod.GetParameters());
        Assert.True(transportTextParameter.IsOut);
        Assert.Equal(typeof(string).MakeByRefType(), transportTextParameter.ParameterType);
        Assert.Null(contract.GetMethod("GetTransportText", Type.EmptyTypes));
        Assert.Null(contract.GetProperty("Content"));
        Assert.Null(contract.GetMethod("GetBytes", Type.EmptyTypes));
        Assert.Null(contract.GetMethod("GetString", Type.EmptyTypes));
        Assert.Null(contract.GetMethod("GetStream", Type.EmptyTypes));

        Type textContract = typeof(TransportTextMessageBody);
        Assert.True(textContract.IsInterface);
        Assert.Contains(typeof(MessageBody), textContract.GetInterfaces());
        MethodInfo payloadMethod = Assert.Single(textContract.GetMethods());
        Assert.Equal(nameof(TransportTextMessageBody.TryGetPayloadText), payloadMethod.Name);
        Assert.Equal(typeof(bool), payloadMethod.ReturnType);
        ParameterInfo payloadParameter = Assert.Single(payloadMethod.GetParameters());
        Assert.True(payloadParameter.IsOut);
        Assert.Equal(typeof(string).MakeByRefType(), payloadParameter.ParameterType);

        MethodInfo requiredTextMethod = typeof(MessageBodyExtensions).GetMethod(
            nameof(MessageBodyExtensions.GetRequiredTransportText),
            [typeof(MessageBody)])!;
        Assert.Equal(typeof(string), requiredTextMethod.ReturnType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "required-transport-text-rejects-missing-body")]
    public void RequiredTransportText_RejectsAMissingBodyAtTheExtensionBoundary()
    {
        MessageBody? body = null;

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => body!.GetRequiredTransportText());

        Assert.Equal("body", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "one-public-binary-body")]
    public void Abstractions_ExportsOneSealedBinaryBodyAndNoRedundantLegacyBodies()
    {
        Assembly assembly = typeof(MessageBody).Assembly;

        Type? binaryBody = assembly.GetType("ViciOne.ServiceBus.Advanced.Serialization.BinaryMessageBody");
        Assert.NotNull(binaryBody);
        Assert.True(binaryBody.IsSealed);
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Advanced.Serialization.ArrayMessageBody"));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Advanced.Serialization.BytesMessageBody"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-BODY-CONTRACT", "abstractions-concrete-type-set")]
    public void EveryConcreteAbstractionsMessageBody_IsInTheExplicitContractSet()
    {
        // Compile-time anchors make a removed or renamed contract fail before the runtime census.
        var contract = new[]
        {
            IdentityOf(typeof(Base64MessageBody)),
            IdentityOf(typeof(BinaryMessageBody)),
            IdentityOf(typeof(EmptyMessageBody)),
            IdentityOf(typeof(StringMessageBody)),
        }.Order(StringComparer.Ordinal);

        // Internal implementations participate because their behavior is still part of this assembly.
        var declared = typeof(MessageBody).Assembly.GetTypes()
            .Where(type => !type.IsInterface && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
            .Select(IdentityOf)
            .Order(StringComparer.Ordinal);

        Assert.Equal(contract, declared);
    }

    private static string IdentityOf(Type type) =>
        type.FullName ?? throw new InvalidOperationException($"Type '{type.Name}' has no full name.");
}
