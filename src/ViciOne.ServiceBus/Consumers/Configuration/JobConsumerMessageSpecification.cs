using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for job consumer message.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
public class JobConsumerMessageSpecification<TConsumer, TJob> :
    IConsumerMessageSpecification<TConsumer, TJob>
    where TJob : class
    where TConsumer : class, IJobConsumer<TJob>
{
    readonly ConsumerMessageSpecification<TConsumer, TJob> _consumerSpecification;

    /// <summary>Initializes a new instance.</summary>
    public JobConsumerMessageSpecification()
    {
        SubmitJobSpecification = ConsumerConnectorCache<SubmitJobConsumer<TJob>>.Connector.CreateConsumerSpecification<SubmitJobConsumer<TJob>>();
        StartJobSpecification = ConsumerConnectorCache<StartJobConsumer<TJob>>.Connector.CreateConsumerSpecification<StartJobConsumer<TJob>>();
        FinalizeJobSpecification = ConsumerConnectorCache<FinalizeJobConsumer<TJob>>.Connector.CreateConsumerSpecification<FinalizeJobConsumer<TJob>>();
        SuperviseJobSpecification = ConsumerConnectorCache<SuperviseJobConsumer>.Connector.CreateConsumerSpecification<SuperviseJobConsumer>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TJob>();
    }

    /// <summary>Gets the submit job specification.</summary>
    public IConsumerSpecification<SubmitJobConsumer<TJob>> SubmitJobSpecification { get; }
    /// <summary>Gets the start job specification.</summary>
    public IConsumerSpecification<StartJobConsumer<TJob>> StartJobSpecification { get; }
    /// <summary>Gets the finalize job specification.</summary>
    public IConsumerSpecification<FinalizeJobConsumer<TJob>> FinalizeJobSpecification { get; }
    /// <summary>Gets the supervise job specification.</summary>
    public IConsumerSpecification<SuperviseJobConsumer> SuperviseJobSpecification { get; }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Message(Action<IConsumerMessageConfigurator<TJob>> configure)
    {
        _consumerSpecification.Message(configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _consumerSpecification.Validate()
            .Concat(SuperviseJobSpecification.Validate())
            .Concat(StartJobSpecification.Validate())
            .Concat(FinalizeJobSpecification.Validate())
            .Concat(SubmitJobSpecification.Validate());
    }

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(TJob);

    /// <summary>Attempts to get message specification.</summary>
    /// <typeparam name="TC">The c type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="specification">Receives the specification produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null;
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Builds the configured component.</summary>
    /// <param name="consumeFilter">The consume filter.</param>
    /// <returns>The configured component.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TJob>> Build(IFilter<ConsumerConsumeContext<TConsumer, TJob>> consumeFilter)
    {
        return _consumerSpecification.Build(consumeFilter);
    }

    /// <summary>Builds message pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured message pipe.</returns>
    public IPipe<ConsumeContext<TJob>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TJob>>> configure)
    {
        return _consumerSpecification.BuildMessagePipe(configure);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _consumerSpecification.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TJob>(specification));
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return new MultipleConnectHandle(_consumerSpecification.ConnectConsumerConfigurationObserver(observer),
            SuperviseJobSpecification.ConnectConsumerConfigurationObserver(observer),
            StartJobSpecification.ConnectConsumerConfigurationObserver(observer),
            FinalizeJobSpecification.ConnectConsumerConfigurationObserver(observer),
            SubmitJobSpecification.ConnectConsumerConfigurationObserver(observer));
    }
}
