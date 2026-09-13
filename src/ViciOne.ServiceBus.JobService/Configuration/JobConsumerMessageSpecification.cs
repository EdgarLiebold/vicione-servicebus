using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Validates and applies middleware for one configured job contract.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class JobConsumerMessageSpecification<TConsumer, TJob> :
    IConsumerMessageSpecification<TConsumer, TJob>
    where TJob : class
    where TConsumer : class, IJobConsumer<TJob>
{
    readonly ConsumerMessageSpecification<TConsumer, TJob> _consumerSpecification;

    /// <summary>Creates user-consumer middleware and the four job lifecycle consumer specifications.</summary>
    public JobConsumerMessageSpecification()
    {
        SubmitJobSpecification = ConsumerConnectorCache<SubmitJobConsumer<TJob>>.Connector.CreateConsumerSpecification<SubmitJobConsumer<TJob>>();
        StartJobSpecification = ConsumerConnectorCache<StartJobConsumer<TJob>>.Connector.CreateConsumerSpecification<StartJobConsumer<TJob>>();
        FinalizeJobSpecification = ConsumerConnectorCache<FinalizeJobConsumer<TJob>>.Connector.CreateConsumerSpecification<FinalizeJobConsumer<TJob>>();
        SuperviseJobSpecification = ConsumerConnectorCache<SuperviseJobConsumer>.Connector.CreateConsumerSpecification<SuperviseJobConsumer>();

        _consumerSpecification = new ConsumerMessageSpecification<TConsumer, TJob>();
    }

    /// <summary>Gets the specification that validates and acknowledges job submissions.</summary>
    public IConsumerSpecification<SubmitJobConsumer<TJob>> SubmitJobSpecification { get; }
    /// <summary>Gets the specification that starts a locally assigned job attempt.</summary>
    public IConsumerSpecification<StartJobConsumer<TJob>> StartJobSpecification { get; }
    /// <summary>Gets the specification that acknowledges terminal job delivery.</summary>
    public IConsumerSpecification<FinalizeJobConsumer<TJob>> FinalizeJobSpecification { get; }
    /// <summary>Gets the specification that handles attempt cancellation and liveness requests.</summary>
    public IConsumerSpecification<SuperviseJobConsumer> SuperviseJobSpecification { get; }

    /// <summary>Adds middleware around the resolved user consumer and job message.</summary>
    /// <param name="specification">The consumer-message pipeline specification to append.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer, TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Applies configuration to the job message pipeline.</summary>
    /// <param name="configure">The job-message configuration callback.</param>
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

    /// <summary>Gets the configured job contract type.</summary>
    public Type MessageType => typeof(TJob);

    /// <summary>Returns this specification when both requested generic types match its consumer and job types.</summary>
    /// <typeparam name="TC">The requested consumer type.</typeparam>
    /// <typeparam name="T">The requested job contract type.</typeparam>
    /// <param name="specification">Receives the specification produced by the operation.</param>
    /// <returns><see langword="true" /> when both generic types match; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSpecification<TC, T>([NotNullWhen(true)] out IConsumerMessageSpecification<TC, T>? specification)
        where T : class
        where TC : class
    {
        specification = this as IConsumerMessageSpecification<TC, T>;
        return specification != null;
    }

    /// <summary>Adds middleware around the job message before consumer resolution.</summary>
    /// <param name="specification">The message pipeline specification to append.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TJob>> specification)
    {
        _consumerSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Builds the user-consumer pipeline with the job lifecycle filter as its terminal stage.</summary>
    /// <param name="consumeFilter">The filter that executes and reports the job attempt.</param>
    /// <returns>The configured consumer pipeline.</returns>
    public IPipe<ConsumerConsumeContext<TConsumer, TJob>> Build(IFilter<ConsumerConsumeContext<TConsumer, TJob>> consumeFilter)
    {
        return _consumerSpecification.Build(consumeFilter);
    }

    /// <summary>Builds the job message pipeline used before resolving the user consumer.</summary>
    /// <param name="configure">The callback that adds registration-level middleware.</param>
    /// <returns>The configured message pipe.</returns>
    public IPipe<ConsumeContext<TJob>> BuildMessagePipe(Action<IPipeConfigurator<ConsumeContext<TJob>>> configure)
    {
        return _consumerSpecification.BuildMessagePipe(configure);
    }

    /// <summary>Adapts consumer-only middleware to the consumer-and-job pipeline.</summary>
    /// <param name="specification">The consumer pipeline specification to adapt.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _consumerSpecification.AddPipeSpecification(new ConsumerPipeSpecificationProxy<TConsumer, TJob>(specification));
    }

    /// <summary>Connects an observer to the user consumer and every auxiliary job consumer.</summary>
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
