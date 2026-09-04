using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Initializers.Factories;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transformation;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a transform specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public abstract class TransformSpecification<TMessage> :
    ITransformConfigurator<TMessage>
    where TMessage : class
{
    readonly MessageTransformConvention<TMessage> _convention;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected TransformSpecification()
    {
        _convention = new MessageTransformConvention<TMessage>();
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _convention.Count;

    /// <summary>
    /// Gets or sets the replace value.
    /// </summary>
    public bool Replace { get; set; }

    /// <summary>
    /// Performs the default operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    public void Default<TProperty>(Expression<Func<TMessage, TProperty>> propertyExpression)
    {
        Set(propertyExpression, (TProperty?)default);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="value">The value.</param>
    public void Set<TProperty>(Expression<Func<TMessage, TProperty>> propertyExpression, TProperty? value)
    {
        var propertyInfo = propertyExpression.GetPropertyInfo();

        var valueProvider = new ConstantPropertyProvider<TMessage, TProperty>(value);

        var initializer = new ProviderPropertyInitializer<TMessage, TMessage, TProperty>(valueProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="valueProvider">The value provider value.</param>
    public void Set<TProperty>(Expression<Func<TMessage, TProperty>> propertyExpression,
        Func<TransformPropertyContext<TProperty, TMessage>, TProperty> valueProvider)
    {
        var propertyInfo = propertyExpression.GetPropertyInfo();

        var inputValueProvider = new InputPropertyProvider<TMessage, TProperty>(propertyInfo);

        Task<TProperty> PropertyProviderAsync(TransformPropertyContext<TProperty, TMessage> context)
        {
            return Task.FromResult(valueProvider(context));
        }

        var propertyProvider = new DelegatePropertyProvider<TMessage, TProperty>(inputValueProvider, PropertyProviderAsync);

        var initializer = new ProviderPropertyInitializer<TMessage, TMessage, TProperty>(propertyProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="propertyProvider">The property provider value.</param>
    public void Set<TProperty>(PropertyInfo propertyInfo, IPropertyProvider<TMessage, TProperty> propertyProvider)
    {
        var initializer = new ProviderPropertyInitializer<TMessage, TMessage, TProperty>(propertyProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>
    /// Performs the transform operation.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="propertyProvider">The property provider value.</param>
    public void Transform<TProperty>(PropertyInfo propertyInfo, IPropertyProvider<TMessage, TProperty> propertyProvider)
    {
        var initializer = new TransformPropertyInitializer<TMessage, TMessage, TProperty>(propertyProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected IMessageInitializer<TMessage> Build()
    {
        IMessageFactory<TMessage>? messageFactory = null;
        IEnumerable<IInitializerConvention> conventions = Enumerable.Repeat<IInitializerConvention>(_convention, 1);
        if (Replace)
            messageFactory = new ReplaceMessageFactory<TMessage>();
        else
            conventions = conventions.Concat(MessageInitializer.Conventions);


        var initializerFactory = new MessageInitializerFactory<TMessage, TMessage>(messageFactory, conventions.ToArray());

        IMessageInitializer<TMessage> messageInitializer = initializerFactory.CreateMessageInitializer();

        return messageInitializer;
    }
}
