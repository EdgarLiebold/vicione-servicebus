using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Initializes message values.</summary>
public static class MessageInitializer
{
    static readonly InitializerConventionRegistry _conventions;

    static MessageInitializer()
    {
        _conventions = new InitializerConventionRegistry(new IInitializerConvention[]
        {
            new DefaultInitializerConvention(),
            new DictionaryInitializerConvention()
        });
    }

    /// <summary>Gets the conventions.</summary>
    public static IReadOnlyList<IInitializerConvention> Conventions => _conventions.Conventions;

    /// <summary>Adds convention to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static void AddConvention<T>()
        where T : IInitializerConvention, new()
    {
        _conventions.Add<T>();
    }
}


/// <summary>Initializes a message using the input, which can include message properties, headers, etc.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class MessageInitializer<TMessage, TInput> :
    IMessageInitializer<TMessage>
    where TMessage : class
    where TInput : class
{
    readonly IMessageFactory<TMessage> _factory;
    readonly IHeaderInitializer<TMessage, TInput>[] _headerInitializers;
    readonly IPropertyInitializer<TMessage, TInput>[] _initializers;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="initializers">The initializers.</param>
    /// <param name="headerInitializers">The header initializers.</param>
    public MessageInitializer(IMessageFactory<TMessage> factory, IEnumerable<IPropertyInitializer<TMessage, TInput>> initializers,
        IEnumerable<IHeaderInitializer<TMessage, TInput>> headerInitializers)
    {
        _factory = factory;
        _initializers = initializers.ToArray();
        _headerInitializers = headerInitializers.ToArray();
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public InitializeContext<TMessage> Create(PipeContext context)
    {
        var baseContext = new ScopeInitializeContext(context);

        return _factory.Create(baseContext);
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created instance.</returns>
    public InitializeContext<TMessage> Create(CancellationToken cancellationToken)
    {
        var baseContext = new BaseInitializeContext(cancellationToken);

        return _factory.Create(baseContext);
    }

    /// <summary>Initializes the target component.</summary>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the initialize outcome.</returns>
    public Task<InitializeContext<TMessage>> InitializeAsync(object input, CancellationToken cancellationToken)
    {
        return InitializeMessageAsync((TInput)input, cancellationToken);
    }

    /// <summary>Initializes the target component.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the initialize outcome.</returns>
    public Task<InitializeContext<TMessage>> InitializeAsync(InitializeContext<TMessage> context, object input, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Initializers.InitializeContext<TMessage>>(cancellationToken); return InitializeMessageAsync(context, (TInput)input);
    }

    /// <summary>Initializes message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the initialize message outcome.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, IPipe<SendContext<TMessage>>? pipe, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>>(cancellationToken); return PrepareInitializedMessageAsync(Create(context), (TInput)input, pipe);
    }

    /// <summary>Initializes message.</summary>
    /// <param name="input">The input.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the initialize message outcome.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object input, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken)
    {
        return PrepareInitializedMessageAsync(Create(cancellationToken), (TInput)input, pipe);
    }

    /// <summary>Initializes message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="moreInputs">The more inputs.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the initialize message outcome.</returns>
    public async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, object?[] moreInputs, IPipe<SendContext<TMessage>>? pipe, CancellationToken cancellationToken = default)
    {
        InitializeContext<TMessage> initializeContext = Create(context);

        for (var i = 0; i < moreInputs.Length; i++)
        {
            var moreInput = moreInputs[i];
            if (moreInput != null)
            {
                IMessageInitializer<TMessage> initializer = MessageInitializerCache<TMessage>.GetInitializer(moreInput.GetType());

                initializeContext = await initializer.InitializeAsync(initializeContext, moreInput, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        return await PrepareInitializedMessageAsync(initializeContext, (TInput)input, pipe).ConfigureAwait(false);
    }

    Task<InitializeContext<TMessage>> InitializeMessageAsync(TInput input, CancellationToken cancellationToken)
    {
        var context = new BaseInitializeContext(cancellationToken);

        InitializeContext<TMessage> messageContext = _factory.Create(context);

        return InitializeMessageAsync(messageContext, input);
    }

    async Task<InitializeContext<TMessage>> InitializeMessageAsync(InitializeContext<TMessage> messageContext, TInput input)
    {
        InitializeContext<TMessage, TInput> inputContext = messageContext.CreateInputContext(input);

        await Task.WhenAll(_initializers.Select(x => x.ApplyAsync(inputContext))).ConfigureAwait(false);

        return messageContext;
    }

    async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> PrepareInitializedMessageAsync(InitializeContext<TMessage> messageContext, TInput input, IPipe<SendContext<TMessage>>? pipe = null)
    {
        InitializeContext<TMessage, TInput> inputContext = messageContext.CreateInputContext(input);

        await Task.WhenAll(_initializers.Select(x => x.ApplyAsync(inputContext))).ConfigureAwait(false);

        return _headerInitializers.Length > 0
            ? new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>(inputContext.Message, new InitializerSendContextPipe(_headerInitializers, inputContext, pipe))
            : new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>(inputContext.Message, pipe);
    }


    class InitializerSendContextPipe :
        IPipe<SendContext<TMessage>>,
        ISendPipe
    {
        readonly InitializeContext<TMessage, TInput> _context;
        readonly IHeaderInitializer<TMessage, TInput>[] _initializers;
        readonly IPipe<SendContext<TMessage>>? _pipe;

        public InitializerSendContextPipe(IHeaderInitializer<TMessage, TInput>[] initializers, InitializeContext<TMessage, TInput> context,
            IPipe<SendContext<TMessage>>? pipe)
        {
            _initializers = initializers;
            _pipe = pipe;
            _context = context;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<TMessage> context)
        {
            await Task.WhenAll(_initializers.Select(x => x.ApplyAsync(_context, context))).ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
                await _pipe.SendAsync(context).ConfigureAwait(false);
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class
        {
            return _pipe is ISendContextPipe sendContextPipe
                ? sendContextPipe.SendAsync(context, cancellationToken)
                : Task.CompletedTask;
        }
    }
}
