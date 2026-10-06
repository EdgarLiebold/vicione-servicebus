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

/// <summary>Describes requirements for transform.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public abstract class TransformSpecification<TMessage> :
    ITransformConfigurator<TMessage>
    where TMessage : class
{
    readonly MessageTransformConvention<TMessage> _convention;

    /// <summary>Initializes a new instance.</summary>
    protected TransformSpecification()
    {
        _convention = new MessageTransformConvention<TMessage>();
    }

    /// <summary>Gets the count.</summary>
    public int Count => _convention.Count;

    /// <summary>Gets or sets the replace.</summary>
    public bool Replace { get; set; }

    /// <summary>Configures the selected property to use its type's default value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    public void Default<TProperty>(Expression<Func<TMessage, TProperty>> propertyExpression)
    {
        Set(propertyExpression, (TProperty?)default);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="value">The value to process.</param>
    public void Set<TProperty>(Expression<Func<TMessage, TProperty>> propertyExpression, TProperty? value)
    {
        var propertyInfo = propertyExpression.GetPropertyInfo();

        var valueProvider = new ConstantPropertyProvider<TMessage, TProperty>(value);

        var initializer = new ProviderPropertyInitializer<TMessage, TMessage, TProperty>(valueProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="valueProvider">The value provider.</param>
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

    /// <summary>Updates the target with the supplied value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="propertyProvider">The property provider.</param>
    public void Set<TProperty>(PropertyInfo propertyInfo, IPropertyProvider<TMessage, TProperty> propertyProvider)
    {
        var initializer = new ProviderPropertyInitializer<TMessage, TMessage, TProperty>(propertyProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>Transforms the supplied value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="propertyProvider">The property provider.</param>
    public void Transform<TProperty>(PropertyInfo propertyInfo, IPropertyProvider<TMessage, TProperty> propertyProvider)
    {
        var initializer = new TransformPropertyInitializer<TMessage, TMessage, TProperty>(propertyProvider, propertyInfo);

        _convention.Add(propertyInfo.Name, initializer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
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
