using System;
using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the submit, start, finalize, and consumer pipelines for one job contract.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class JobConsumerMessageConnector<TConsumer, TJob> :
    IConsumerMessageConnector<TConsumer>
    where TConsumer : class, IJobConsumer<TJob>
    where TJob : class
{
    readonly IConsumerConnector _finalizeJobConsumerConnector;
    readonly IConsumerConnector _startJobConsumerConnector;
    readonly IConsumerConnector _submitJobConsumerConnector;

    /// <summary>Initializes a new instance.</summary>
    public JobConsumerMessageConnector()
    {
        _submitJobConsumerConnector = ConsumerConnectorCache<SubmitJobConsumer<TJob>>.Connector;
        _startJobConsumerConnector = ConsumerConnectorCache<StartJobConsumer<TJob>>.Connector;
        _finalizeJobConsumerConnector = ConsumerConnectorCache<FinalizeJobConsumer<TJob>>.Connector;
    }

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(TJob);

    /// <summary>Creates consumer message specification.</summary>
    /// <returns>The created consumer message specification.</returns>
    public IConsumerMessageSpecification<TConsumer> CreateConsumerMessageSpecification()
    {
        return new JobConsumerMessageSpecification<TConsumer, TJob>();
    }

    /// <summary>Connects consumer.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumer(IConsumePipeConnector consumePipe, IConsumerFactory<TConsumer> consumerFactory,
        IConsumerSpecification<TConsumer> specification)
    {
        if (!specification.TryGetOptions(out JobServiceSettings settings) || settings.InstanceEndpoint == null)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Job Consumer Message Connector", "unknown", "The job service must be configured when adding job consumers. See https://github.com/EdgarLiebold/vicione-servicebus/documentation/patterns/job-consumers", "Correct the named configuration before starting the host"));
        }

        var options = specification.Options<JobOptions<TJob>>();

        var jobTypeId = settings.Runtime.GetJobTypeId<TJob>();

        IConsumerMessageSpecification<TConsumer, TJob> messageSpecification = specification.GetMessageSpecification<TJob>();

        if (!(messageSpecification is JobConsumerMessageSpecification<TConsumer, TJob> jobSpecification))
            throw new ArgumentException("The consumer specification did not match the message specification type");

        var submitJobHandle = ConnectSubmitJobConsumer(consumePipe, jobSpecification.SubmitJobSpecification, options, jobTypeId);

        IPipe<ConsumeContext<TJob>> jobPipe = CreateJobPipe(consumerFactory, specification);
        var startJobHandle = ConnectStartJobConsumer(consumePipe, jobSpecification.StartJobSpecification, options, jobTypeId, settings.Runtime, jobPipe);

        ConfigureInstanceStartJobConsumer(settings.InstanceEndpoint, options, jobTypeId, settings.Runtime, jobPipe);

        var finalizeJobHandle = ConnectFinalizeJobConsumer(consumePipe, jobSpecification.FinalizeJobSpecification, jobTypeId);

        return new MultipleConnectHandle(submitJobHandle, startJobHandle, finalizeJobHandle);
    }

    static IPipe<ConsumeContext<TJob>> CreateJobPipe(IConsumerFactory<TConsumer> consumerFactory, IConsumerSpecification<TConsumer> specification)
    {
        IConsumerMessageSpecification<TConsumer, TJob> messageSpecification = specification.GetMessageSpecification<TJob>();

        var options = specification.Options<JobOptions<TJob>>();

        var jobFilter = new JobConsumerMessageFilter<TConsumer, TJob>(options.RetryPolicy);

        IPipe<ConsumerConsumeContext<TConsumer, TJob>> consumerPipe = messageSpecification.Build(jobFilter);

        IPipe<ConsumeContext<TJob>> messagePipe = messageSpecification.BuildMessagePipe(x =>
        {
            specification.ConfigureMessagePipe(x);

            x.UseFilter(new ConsumerMessageFilter<TConsumer, TJob>(consumerFactory, consumerPipe));
        });

        return messagePipe;
    }

    ConnectHandle ConnectSubmitJobConsumer(IConsumePipeConnector consumePipe,
        IConsumerSpecification<SubmitJobConsumer<TJob>> specification, JobOptions<TJob> options, Guid jobTypeId)
    {
        var consumerFactory = new DelegateConsumerFactory<SubmitJobConsumer<TJob>>(() => new SubmitJobConsumer<TJob>(options, jobTypeId));

        return _submitJobConsumerConnector.ConnectConsumer(consumePipe, consumerFactory, specification);
    }

    ConnectHandle ConnectStartJobConsumer(IConsumePipeConnector consumePipe, IConsumerSpecification<StartJobConsumer<TJob>> specification,
        JobOptions<TJob> options, Guid jobTypeId, IJobService jobService, IPipe<ConsumeContext<TJob>> pipe)
    {
        var consumerFactory = new DelegateConsumerFactory<StartJobConsumer<TJob>>(() => new StartJobConsumer<TJob>(jobService, options, jobTypeId, pipe));

        return _startJobConsumerConnector.ConnectConsumer(consumePipe, consumerFactory, specification);
    }

    ConnectHandle ConnectFinalizeJobConsumer(IConsumePipeConnector consumePipe, IConsumerSpecification<FinalizeJobConsumer<TJob>> specification,
        Guid jobTypeId)
    {
        var consumerFactory = new DelegateConsumerFactory<FinalizeJobConsumer<TJob>>(() => new FinalizeJobConsumer<TJob>(jobTypeId));

        return _finalizeJobConsumerConnector.ConnectConsumer(consumePipe, consumerFactory, specification);
    }

    static void ConfigureInstanceStartJobConsumer(IReceiveEndpointConfigurator configurator, JobOptions<TJob> options, Guid jobTypeId,
        IJobService jobService, IPipe<ConsumeContext<TJob>> pipe)
    {
        var consumerFactory = new DelegateConsumerFactory<StartJobConsumer<TJob>>(() => new StartJobConsumer<TJob>(jobService, options, jobTypeId, pipe));

        var consumerConfigurator = new ConsumerConfigurator<StartJobConsumer<TJob>>(consumerFactory, configurator);

        configurator.AddEndpointSpecification(consumerConfigurator);
    }
}
