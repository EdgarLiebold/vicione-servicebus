using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

public class SerializationConfiguration :
    ISerializationConfiguration
{
    readonly Lazy<ISerialization> _collection;
    readonly IDictionary<string, ISerializerFactory> _deserializers;
    readonly List<Func<JsonSerializerOptions, JsonSerializerOptions>> _jsonOptionsConfigurators;
    readonly IDictionary<string, ISerializerFactory> _serializers;
    ContentType? _defaultContentType;
    ContentType? _serializerContentType;
    SerializationConfiguration? _source;

    public SerializationConfiguration()
    {
        _serializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _deserializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _jsonOptionsConfigurators = [];
        _collection = new Lazy<ISerialization>(CreateCollection);

        AddSystemTextJson();
    }

    SerializationConfiguration(SerializationConfiguration source)
    {
        _serializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _deserializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _jsonOptionsConfigurators = [];
        _collection = new Lazy<ISerialization>(CreateCollection);

        _source = source;
    }

    public ContentType DefaultContentType
    {
        set
        {
            EnsureMutable();
            _defaultContentType = value;
        }
    }

    public ContentType SerializerContentType
    {
        set
        {
            EnsureMutable();
            _serializerContentType = value;
        }
    }

    public void Clear()
    {
        EnsureMutable();

        _serializers.Clear();
        _deserializers.Clear();
        _jsonOptionsConfigurators.Clear();

        _defaultContentType = null;
        _serializerContentType = null;

        _source = null;
    }

    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureMutable();

        _serializers[factory.ContentType.MediaType] = factory;

        if (isSerializer)
            _serializerContentType = factory.ContentType;
    }

    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureMutable();

        _deserializers[factory.ContentType.MediaType] = factory;

        if (isDefault)
            _defaultContentType = factory.ContentType;
    }

    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureMutable();
        _jsonOptionsConfigurators.Add(configure);
    }

    public ISerializationConfiguration CreateSerializationConfiguration()
    {
        return new SerializationConfiguration(this);
    }

    public ISerialization CreateSerializerCollection()
    {
        return _collection.Value;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        var serializers = ResolveFactories(static x => x._serializers);
        var serializerMediaTypes = serializers.Keys.ToArray();
        if (serializerMediaTypes.Length == 0)
            yield return this.Failure("Serializers", "must specify at least one serializer");

        var serializerMediaType = ResolveSerializerContentType()?.MediaType
            ?? (serializerMediaTypes.Length == 1 ? serializerMediaTypes[0] : null);

        if (serializerMediaType == null && serializerMediaTypes.Length > 1)
            yield return this.Failure("SerializerContentType", "must be specified when more than one serializer is supported");
        else if (serializerMediaType != null && !serializers.ContainsKey(serializerMediaType))
            yield return this.Failure("SerializerContentType", "matching serializer was not added");

        var deserializers = ResolveFactories(static x => x._deserializers);
        var deserializerMediaTypes = deserializers.Keys.ToArray();
        if (deserializerMediaTypes.Length == 0)
            yield return this.Failure("Deserializers", "must specify at least one deserializer");

        var defaultMediaType = ResolveDefaultContentType()?.MediaType
            ?? (deserializerMediaTypes.Length == 1 ? deserializerMediaTypes[0] : null);

        if (defaultMediaType == null && deserializerMediaTypes.Length > 1)
            yield return this.Failure("DefaultContentType", "must be specified when more than one deserializer is supported");
        else if (defaultMediaType != null && !deserializers.ContainsKey(defaultMediaType))
            yield return this.Failure("DefaultContentType", "matching deserializer was not added");
    }

    ISerialization CreateCollection()
    {
        var serializers = ResolveFactories(static x => x._serializers);
        var deserializers = ResolveFactories(static x => x._deserializers);
        var jsonOptions = CreateJsonSerializerOptions();
        var boundFactories = new Dictionary<ISerializerFactory, ISerializerFactory>(ReferenceEqualityComparer.Instance);

        ISerializerFactory Bind(ISerializerFactory factory)
        {
            if (factory is not IJsonSerializerFactory jsonFactory)
                return factory;

            if (boundFactories.TryGetValue(factory, out var boundFactory))
                return boundFactory;

            boundFactory = jsonFactory.Bind(jsonOptions);
            boundFactories.Add(factory, boundFactory);
            return boundFactory;
        }

        var messageSerializers = serializers.Values.Select(x => Bind(x).CreateSerializer()).ToArray();
        var serializerContentType = ResolveSerializerContentType()
            ?? (messageSerializers.Length == 1 ? messageSerializers[0].ContentType : null)
            ?? throw new ConfigurationException("No serializer content type specified and more than one serializer was configured");

        var messageDeserializers = deserializers.Values.Select(x => Bind(x).CreateDeserializer()).ToArray();
        var defaultContentType = ResolveDefaultContentType()
            ?? (messageDeserializers.Length == 1 ? messageDeserializers[0].ContentType : null)
            ?? throw new ConfigurationException("No default content type specified and more than one deserializer was configured");

        return new ViciOne.ServiceBus.Serialization.Serialization(messageSerializers, serializerContentType, messageDeserializers, defaultContentType);
    }

    JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = _source?.CreateJsonSerializerOptions()
            ?? SystemTextJsonSerializerOptions.Freeze(SystemTextJsonSerializerOptions.CreateDefault());

        foreach (var configure in _jsonOptionsConfigurators)
        {
            var candidate = configure(new JsonSerializerOptions(options))
                ?? throw new ConfigurationException(
                    "The ConfigureJsonSerializerOptions callback returned null. It must return the options to use.");

            options = SystemTextJsonSerializerOptions.Freeze(candidate);
        }

        return options;
    }

    Dictionary<string, ISerializerFactory> ResolveFactories(
        Func<SerializationConfiguration, IDictionary<string, ISerializerFactory>> selector)
    {
        var result = _source?.ResolveFactories(selector)
            ?? new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in selector(this))
            result[pair.Key] = pair.Value;

        return result;
    }

    ContentType? ResolveSerializerContentType()
    {
        return _serializerContentType ?? _source?.ResolveSerializerContentType();
    }

    ContentType? ResolveDefaultContentType()
    {
        return _defaultContentType ?? _source?.ResolveDefaultContentType();
    }

    void EnsureMutable()
    {
        if (_collection.IsValueCreated)
            throw new ConfigurationException("The serializer collection was already created.");
    }

    void AddSystemTextJson()
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        AddSerializer(factory);
        AddDeserializer(factory, true);
    }
}
