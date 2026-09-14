using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers.Contexts;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Owns the ordered conventions used to construct messages from runtime input objects.</summary>
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

    /// <summary>Gets the immutable snapshot of initializer conventions in evaluation order.</summary>
    public static IReadOnlyList<IInitializerConvention> Conventions => _conventions.Conventions;

    /// <summary>Registers an initializer convention before the convention snapshot is first read.</summary>
    /// <typeparam name="T">The convention type to create and register.</typeparam>
    /// <exception cref="InvalidOperationException">The convention snapshot has already been read.</exception>
    public static void AddConvention<T>()
        where T : IInitializerConvention, new()
    {
        _conventions.Add<T>();
    }
}


/// <summary>Creates a message and populates its properties and outgoing headers from a typed input object.</summary>
/// <typeparam name="TMessage">The message contract produced by the initializer.</typeparam>
/// <typeparam name="TInput">The input-object type consumed by the initializer.</typeparam>
internal sealed class MessageInitializer<TMessage, TInput> :
    IMessageInitializer<TMessage>
    where TMessage : class
    where TInput : class
{
    readonly IMessageFactory<TMessage> _factory;
    readonly IHeaderInitializer<TMessage, TInput>[] _headerInitializers;
    readonly IPropertyInitializer<TMessage, TInput>[] _initializers;

    /// <summary>Creates an initializer from a message factory and its property and header mappings.</summary>
    /// <param name="factory">The factory that creates each message context.</param>
    /// <param name="initializers">The mappings that populate message properties.</param>
    /// <param name="headerInitializers">The mappings applied when the initialized message enters a send context.</param>
    public MessageInitializer(IMessageFactory<TMessage> factory, IEnumerable<IPropertyInitializer<TMessage, TInput>> initializers,
        IEnumerable<IHeaderInitializer<TMessage, TInput>> headerInitializers)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _initializers = SnapshotInitializers(initializers, nameof(initializers));
        _headerInitializers = SnapshotInitializers(headerInitializers, nameof(headerInitializers));
    }

    /// <summary>Creates an unpopulated message context that inherits an existing pipeline context.</summary>
    /// <param name="context">The pipeline context that supplies payloads and cancellation state.</param>
    /// <returns>A context containing a newly created message.</returns>
    public InitializeContext<TMessage> Create(PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var baseContext = new ScopeInitializeContext(context);

        return _factory.Create(baseContext)
            ?? throw new InvalidOperationException($"The message factory for '{typeof(TMessage)}' returned null.");
    }

    /// <summary>Creates an unpopulated message context with explicit cancellation state.</summary>
    /// <param name="cancellationToken">The token exposed by the new context.</param>
    /// <returns>A context containing a newly created message.</returns>
    public InitializeContext<TMessage> Create(CancellationToken cancellationToken)
    {
        var baseContext = new BaseInitializeContext(cancellationToken);

        return _factory.Create(baseContext)
            ?? throw new InvalidOperationException($"The message factory for '{typeof(TMessage)}' returned null.");
    }

    /// <summary>Creates and populates a message from an input object.</summary>
    /// <param name="input">The input object, which must be assignable to <typeparamref name="TInput"/>.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    public Task<InitializeContext<TMessage>> InitializeAsync(object input, CancellationToken cancellationToken)
    {
        return InitializeMessageAsync(RequireInput(input), cancellationToken);
    }

    /// <summary>Applies an input object to an existing message context.</summary>
    /// <param name="context">The message context to populate.</param>
    /// <param name="input">The input object, which must be assignable to <typeparamref name="TInput"/>.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task containing the populated message context.</returns>
    public Task<InitializeContext<TMessage>> InitializeAsync(InitializeContext<TMessage> context, object input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return InitializeMessageAsync(context, RequireInput(input), cancellationToken);
    }

    /// <summary>Creates a message that inherits a pipeline context and prepares its initialized-header pipe.</summary>
    /// <param name="context">The pipeline context inherited by message initialization.</param>
    /// <param name="input">The input object, which must be assignable to <typeparamref name="TInput"/>.</param>
    /// <param name="pipe">Additional send-pipeline stages associated with the initialized message.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, IPipe<SendContext<TMessage>>? pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return PrepareInitializedMessageAsync(Create(context), RequireInput(input), pipe, cancellationToken);
    }

    /// <summary>Creates a message and prepares its initialized-header pipe without inheriting another context.</summary>
    /// <param name="input">The input object, which must be assignable to <typeparamref name="TInput"/>.</param>
    /// <param name="pipe">The send-pipeline stages associated with the initialized message.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(object input, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return PrepareInitializedMessageAsync(Create(cancellationToken), RequireInput(input), pipe, cancellationToken);
    }

    /// <summary>Creates a message, applies multiple input objects, and prepares its initialized-header pipe.</summary>
    /// <param name="context">The pipeline context inherited by message initialization.</param>
    /// <param name="input">The primary input object applied after the additional inputs.</param>
    /// <param name="moreInputs">Additional input objects applied in array order; null entries are ignored.</param>
    /// <param name="pipe">Additional send-pipeline stages associated with the initialized message.</param>
    /// <param name="cancellationToken">The token that cancels property initialization.</param>
    /// <returns>A task containing the populated message and its send pipe.</returns>
    public async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> InitializeMessageAsync(PipeContext context, object input, object?[] moreInputs, IPipe<SendContext<TMessage>>? pipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        TInput primaryInput = RequireInput(input);
        ArgumentNullException.ThrowIfNull(moreInputs);
        object?[] additionalInputs = [.. moreInputs];
        cancellationToken.ThrowIfCancellationRequested();

        InitializeContext<TMessage> initializeContext = Create(context);

        for (var i = 0; i < additionalInputs.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? moreInput = additionalInputs[i];
            if (moreInput != null)
            {
                IMessageInitializer<TMessage> initializer = MessageInitializerCache<TMessage>.GetInitializer(moreInput.GetType());

                initializeContext = await initializer.InitializeAsync(initializeContext, moreInput, cancellationToken: cancellationToken)
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await PrepareInitializedMessageAsync(initializeContext, primaryInput, pipe, cancellationToken)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<InitializeContext<TMessage>> InitializeMessageAsync(TInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = new BaseInitializeContext(cancellationToken);

        InitializeContext<TMessage> messageContext = _factory.Create(context)
            ?? throw new InvalidOperationException($"The message factory for '{typeof(TMessage)}' returned null.");

        return await InitializeMessageAsync(messageContext, input, cancellationToken)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    async Task<InitializeContext<TMessage>> InitializeMessageAsync(
        InitializeContext<TMessage> messageContext,
        TInput input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InitializeContext<TMessage, TInput> inputContext = messageContext.CreateInputContext(input);

        await Task.WhenAll(_initializers.Select(x => RequireInitializerTaskAsync(
                x.ApplyAsync(inputContext, cancellationToken),
                "property")))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return messageContext;
    }

    async Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>> PrepareInitializedMessageAsync(
        InitializeContext<TMessage> messageContext,
        TInput input,
        IPipe<SendContext<TMessage>>? pipe,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InitializeContext<TMessage, TInput> inputContext = messageContext.CreateInputContext(input);

        await Task.WhenAll(_initializers.Select(x => RequireInitializerTaskAsync(
                x.ApplyAsync(inputContext, cancellationToken),
                "property")))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return _headerInitializers.Length > 0
            ? new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>(inputContext.Message, new InitializerSendContextPipe(_headerInitializers, inputContext, pipe))
            : new global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TMessage>(inputContext.Message, pipe);
    }

    static TInput RequireInput(object input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input is TInput typedInput)
            return typedInput;

        throw new ArgumentException(
            $"The input must be assignable to '{typeof(TInput)}'.",
            nameof(input));
    }

    static TInitializer[] SnapshotInitializers<TInitializer>(
        IEnumerable<TInitializer> initializers,
        string parameterName)
        where TInitializer : class
    {
        ArgumentNullException.ThrowIfNull(initializers, parameterName);
        TInitializer[] snapshot = initializers.ToArray();
        if (Array.Exists(snapshot, static initializer => initializer is null))
            throw new ArgumentException("The initializer collection cannot contain null elements.", parameterName);

        return snapshot;
    }

    static Task RequireInitializerTaskAsync(Task? task, string initializerKind)
    {
        return task ?? Task.FromException(
            new InvalidOperationException($"A {initializerKind} initializer returned a null task."));
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
            _initializers = initializers ?? throw new ArgumentNullException(nameof(initializers));
            _pipe = pipe;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.CancellationToken.ThrowIfCancellationRequested();
            await Task.WhenAll(_initializers.Select(x => RequireInitializerTaskAsync(
                    x.ApplyAsync(_context, context, context.CancellationToken),
                    "header")))
                .WaitAsync(context.CancellationToken)
                .ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
                await _pipe.SendAsync(context).WaitAsync(context.CancellationToken).ConfigureAwait(false);
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _pipe is ISendContextPipe sendContextPipe
                ? sendContextPipe.SendAsync(context, cancellationToken)
                : Task.CompletedTask;
        }
    }
}
