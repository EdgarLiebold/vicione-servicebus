using System;
using System.Collections.Generic;
using System.Globalization;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

internal sealed class SerializedEnvelopeHeaderSnapshot
{
    private readonly Dictionary<string, HeaderValue> _headers;
    private readonly Dictionary<string, HeaderValue> _projectedHeaders;
    private readonly IReadOnlyDictionary<string, object?> _projectedSource;

    public SerializedEnvelopeHeaderSnapshot(
        IReadOnlyDictionary<string, object?> projectedHeaders,
        DictionarySendHeaders contextHeaders,
        Func<object?, Type, byte[]> encode,
        Func<Type, byte[], object?> decode)
    {
        _projectedSource = projectedHeaders;
        _headers = new Dictionary<string, HeaderValue>(StringComparer.OrdinalIgnoreCase);
        _projectedHeaders = new Dictionary<string, HeaderValue>(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, object value) in contextHeaders.GetAll())
        {
            if (!MayChangeAfterSerialization(key))
                _headers.Add(key, HeaderValue.Capture(value, encode, decode));
        }
        foreach ((string key, object? value) in projectedHeaders)
        {
            if (!MayChangeAfterSerialization(key))
                _projectedHeaders.Add(key, HeaderValue.Capture(value, encode, decode));
        }
    }

    public string? ChangedHeader(DictionarySendHeaders current)
    {
        foreach ((string key, HeaderValue value) in _headers)
        {
            if (!current.TryGetHeader(key, out object? actual) || !value.Matches(actual))
                return key;
        }

        foreach ((string key, _) in current.GetAll())
        {
            if (!MayChangeAfterSerialization(key) && !_headers.ContainsKey(key))
                return key;
        }

        foreach ((string key, HeaderValue value) in _projectedHeaders)
        {
            if (!_projectedSource.TryGetValue(key, out object? actual) || !value.Matches(actual))
                return key;
        }
        foreach ((string key, _) in _projectedSource)
        {
            if (!MayChangeAfterSerialization(key) && !_projectedHeaders.ContainsKey(key))
                return key;
        }

        return null;
    }

    public void Restore(DictionarySendHeaders current)
    {
        var added = new List<string>();
        foreach ((string key, _) in current.GetAll())
        {
            if (!MayChangeAfterSerialization(key) && !_headers.ContainsKey(key))
                added.Add(key);
        }
        foreach (string key in added)
            current.Set(key, null);

        foreach ((string key, HeaderValue value) in _headers)
        {
            if (current.TryGetHeader(key, out object? actual) && value.Matches(actual))
                continue;
            current.Set(key, value.Restore(actual));
        }
    }

    public Dictionary<string, string> SnapshotJournalHeaders(Headers current)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, HeaderValue value) in _projectedHeaders)
        {
            string? text = value.GetJournalText();
            if (text is not null)
                result[key] = text;
        }
        foreach ((string key, object value) in current.GetAll())
        {
            if (!MayChangeAfterSerialization(key))
                continue;
            string? text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (text is not null)
                result[key] = text;
        }

        return result;
    }

    private static bool MayChangeAfterSerialization(string key) =>
        key.Equals(DiagnosticPropagationHeaders.ActivityId, StringComparison.OrdinalIgnoreCase)
        || key.Equals(DiagnosticPropagationHeaders.TraceState, StringComparison.OrdinalIgnoreCase)
        || key.Equals(DiagnosticPropagationHeaders.Baggage, StringComparison.OrdinalIgnoreCase)
        || key.Equals(DiagnosticPropagationHeaders.ParentMode, StringComparison.OrdinalIgnoreCase)
        || key.Equals(DiagnosticPropagationHeaders.AzureDiagnosticId, StringComparison.OrdinalIgnoreCase)
        || key.Equals(MessageHeaders.SchedulingTokenId, StringComparison.OrdinalIgnoreCase);

    private sealed class HeaderValue
    {
        private readonly Type _type;
        private readonly byte[] _encoded;
        private readonly Func<object?, Type, byte[]> _encode;
        private readonly Func<Type, byte[], object?> _decode;
        private readonly object? _originalValue;

        private HeaderValue(Type type, byte[] encoded, object? originalValue,
            Func<object?, Type, byte[]> encode, Func<Type, byte[], object?> decode)
        {
            _type = type;
            _encoded = encoded;
            _originalValue = originalValue;
            _encode = encode;
            _decode = decode;
        }

        public string? GetJournalText() => Convert.ToString(_originalValue, CultureInfo.InvariantCulture);

        public static HeaderValue Capture(object? value,
            Func<object?, Type, byte[]> encode, Func<Type, byte[], object?> decode)
        {
            Type type = value?.GetType() ?? typeof(object);
            return new HeaderValue(type, encode(value, type), value, encode, decode);
        }

        public bool Matches(object? value)
        {
            if ((value?.GetType() ?? typeof(object)) != _type)
                return false;
            try
            {
                return _encode(value, _type).AsSpan().SequenceEqual(_encoded);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object? Restore(object? current)
        {
            try
            {
                return _decode(_type, _encoded);
            }
            catch (Exception)
            {
                if (!ReferenceEquals(current, _originalValue))
                    return _originalValue;

                // A write-only converter cannot recreate a mutated instance. Preserve the
                // original wire value in an owned form and keep the send rejected.
                return (byte[])_encoded.Clone();
            }
        }
    }
}
