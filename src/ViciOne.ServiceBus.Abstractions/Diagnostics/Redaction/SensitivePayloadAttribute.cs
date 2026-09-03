namespace ViciOne.ServiceBus.Diagnostics;

using System;

/// <summary>
/// Marks a contract whose payload content must be redacted from ServiceBus-owned diagnostics.
/// This metadata does not grant authorization or replace application data classification.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class SensitivePayloadAttribute : Attribute;
