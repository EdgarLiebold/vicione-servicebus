using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a job consumer message specification implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TJob">The t job type.</typeparam>
public class JobConsumerMessageSpecification<TConsumer, TJob> :
    IConsumerMessageSpecification<TConsumer, TJob>
    where TJob : class
    where TConsumer : class, IJobConsumer<TJob>
{
    readonly ConsumerMessageSpecification<TConsumer, TJob> _consumerSpecification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobConsumerMessageSpecification()
    {
        SubmitJobSpecification = ConsumerConnectorCache<SubmitJobConsumer<TJob>>.Connector.CreateConsumerSpecification<SubmitJobConsumer<TJob>>();
        StartJobSpecification = ConsumerConnectorCache<StartJobConsumer<TJob>>.Connector.CreateConsumerSpecification<StartJobConsumer<TJob>>();
        FinalizeJobSpecification = ConsumerConnectorCache<FinalizeJobConsumer<TJob>>.Connector.CreateConsumerSpecification<FinalizeJobConsumer<TJob>>();
        SuperviseJobSpecification = ConsumerConnectorCache<SuperviseJobConsumer>.Connector.CreateConsumerSpecification<SuperviseJobConsumer>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TJob>();
    }

    /// <summary>
    /// Gets the submit job specification value.
    /// </summary>
    public IConsumerSpecification<SubmitJobConsumer<TJob>> SubmitJobSpecification { get; }
    /// <summary>
    /// Gets the start job specification value.
    /// </summary>
    public IConsumerSpecification<StartJobConsumer<TJob>> StartJobSpecification { get; }
    /// <summary>
    /// Gets the finalize job specification value.
    /// </summary>
    public IConsumerSpecification<FinalizeJobConsumer<TJob>> FinalizeJobSpecification { get; }
    /// <summary>
    /// Gets the supervise job specification value.
    /// </summary>
    public IConsumerSpecification<SuperviseJobConsumer> SuperviseJobSpecification { get; }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Message(Action<IConsumerMessageConfigurator<TJob>> configure)
    {
        _consumerSpecification.Message(configure);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _consumerSpecification.Validate()
            .Concat(SuperviseJobSpecification.Validate())
            .Concat(StartJobSpecification.Validate())
            .Concat(FinalizeJobSpecification.Validate())
            .Concat(SubmitJobSpecification.Validate());
    }

    /// <summary>
    /// Gets the message type value.
    /// </summary>
    public Type MessageType => typeof(TJob);

    /// <summary>
    /// Attempts to get message specification.
    /// </summary>
    /// <typeparam name="TC">The tc type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="specification">The specification value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null;
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="consumeFilter">The consume filter value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TJob>> Build(IFilter<ConsumerConsumeContext<TConsumer, TJob>> consumeFilter)
    {
        return _consumerSpecification.Build(consumeFilter);
    }

    /// <summary>
    /// Performs the build message pipe operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IPipe<ConsumeContext<TJob>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TJob>>> configure)
    {
        return _consumerSpecification.BuildMessagePipe(configure);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _consumerSpecification.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TJob>(specification));
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return new MultipleConnectHandle(_consumerSpecification.ConnectConsumerConfigurationObserver(observer),
            SuperviseJobSpecification.ConnectConsumerConfigurationObserver(observer),
            StartJobSpecification.ConnectConsumerConfigurationObserver(observer),
            FinalizeJobSpecification.ConnectConsumerConfigurationObserver(observer),
            SubmitJobSpecification.ConnectConsumerConfigurationObserver(observer));
    }
}
