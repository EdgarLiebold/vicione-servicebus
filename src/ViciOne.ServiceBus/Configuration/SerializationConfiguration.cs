using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates serialization configuration.</summary>
public class SerializationConfiguration :
    ISerializationConfiguration
{
    readonly Lazy<ISerialization> _collection;
    readonly IDictionary<string, ISerializerFactory> _deserializers;
    readonly JsonSerializerConstraints _jsonConstraints;
    readonly List<Func<JsonSerializerOptions, JsonSerializerOptions>> _jsonOptionsConfigurators;
    readonly IDictionary<string, ISerializerFactory> _serializers;
    ContentType? _defaultContentType;
    ContentType? _serializerContentType;
    SerializationConfiguration? _source;

    /// <summary>Initializes a new instance.</summary>
    public SerializationConfiguration()
    {
        _serializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _deserializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _jsonConstraints = new JsonSerializerConstraints();
        _jsonOptionsConfigurators = [];
        _collection = new Lazy<ISerialization>(CreateCollection);

        AddSystemTextJson();
    }

    SerializationConfiguration(SerializationConfiguration source)
    {
        _serializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _deserializers = new Dictionary<string, ISerializerFactory>(StringComparer.OrdinalIgnoreCase);
        _jsonConstraints = source._jsonConstraints;
        _jsonOptionsConfigurators = [];
        _collection = new Lazy<ISerialization>(CreateCollection);

        _source = source;
    }

    /// <summary>Gets or sets the default content type.</summary>
    public ContentType DefaultContentType
    {
        set
        {
            EnsureMutable();
            _defaultContentType = value;
        }
    }

    /// <summary>Gets or sets the serializer content type.</summary>
    public ContentType SerializerContentType
    {
        set
        {
            EnsureMutable();
            _serializerContentType = value;
        }
    }

    /// <summary>Removes every item from the current collection.</summary>
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

    /// <summary>Adds serializer to the configuration.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isSerializer">The is serializer.</param>
    public void AddSerializer(ISerializerFactory factory, bool isSerializer = true)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureMutable();

        _serializers[factory.ContentType.MediaType] = factory;

        if (isSerializer)
            _serializerContentType = factory.ContentType;
    }

    /// <summary>Adds deserializer to the configuration.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="isDefault">The is default.</param>
    public void AddDeserializer(ISerializerFactory factory, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(factory);
        EnsureMutable();

        _deserializers[factory.ContentType.MediaType] = factory;

        if (isDefault)
            _defaultContentType = factory.ContentType;
    }

    /// <summary>Configures system text json serializer options.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConfigureSystemTextJsonSerializerOptions(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureMutable();
        _jsonOptionsConfigurators.Add(configure);
    }

    internal void SetMaximumJsonDepth(int maximumDepth)
    {
        EnsureMutable();
        _jsonConstraints.MaximumDepth = maximumDepth;
    }

    /// <summary>Creates serialization configuration.</summary>
    /// <returns>The created serialization configuration.</returns>
    public ISerializationConfiguration CreateSerializationConfiguration()
    {
        return new SerializationConfiguration(this);
    }

    /// <summary>Creates serializer collection.</summary>
    /// <returns>The created serializer collection.</returns>
    public ISerialization CreateSerializerCollection()
    {
        return _collection.Value;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (ValidationResult failure in ValidateSerializers())
            yield return failure;

        foreach (ValidationResult failure in ValidateDeserializers())
            yield return failure;
    }

    IEnumerable<ValidationResult> ValidateSerializers()
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
    }

    IEnumerable<ValidationResult> ValidateDeserializers()
    {
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
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "No serializer content type specified and more than one serializer was configured", "Correct the named configuration before starting the host"));

        var messageDeserializers = deserializers.Values.Select(x => Bind(x).CreateDeserializer()).ToArray();
        var defaultContentType = ResolveDefaultContentType()
            ?? (messageDeserializers.Length == 1 ? messageDeserializers[0].ContentType : null)
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "No default content type specified and more than one deserializer was configured", "Correct the named configuration before starting the host"));

        return new SerializerCollection(messageSerializers, serializerContentType, messageDeserializers, defaultContentType);
    }

    JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = _source?.CreateJsonSerializerOptions()
            ?? SystemTextJsonSerializerOptions.Freeze(SystemTextJsonSerializerOptions.CreateDefault());

        foreach (var configure in _jsonOptionsConfigurators)
        {
            var candidate = configure(new JsonSerializerOptions(options))
                ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "The ConfigureJsonSerializerOptions callback returned null. It must return the options to use.", "Correct the named configuration before starting the host"));

            options = SystemTextJsonSerializerOptions.Freeze(candidate);
        }

        if (_jsonConstraints.MaximumDepth is { } maximumDepth)
        {
            options = SystemTextJsonSerializerOptions.Freeze(new JsonSerializerOptions(options)
            {
                MaxDepth = maximumDepth,
            });
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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "The serializer collection was already created.", "Correct the named configuration before starting the host"));
    }

    void AddSystemTextJson()
    {
        var factory = new SystemTextJsonMessageSerializerFactory();

        AddSerializer(factory);
        AddDeserializer(factory, true);
    }

    sealed class JsonSerializerConstraints
    {
        public int? MaximumDepth { get; set; }
    }
}
