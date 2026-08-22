#nullable enable
namespace ViciOne.ServiceBus.Util
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;


    public static class TaskUtil
    {
        public static Task Completed => Task.CompletedTask;
        public static Task<bool> False => Cached.FalseTask;
        public static Task<bool> True => Cached.TrueTask;

        /// <summary>
        /// Returns a completed task with the default value for <typeparamref name="T" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<T?> Default<T>()
        {
            return Cached<T>.DefaultValueTask;
        }

        /// <summary>
        /// Returns a faulted task with the specified exception.
        /// </summary>
        /// <param name="exception"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<T> Faulted<T>(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return Task.FromException<T>(exception);
        }

        /// <summary>
        /// Returns a canceled task for the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<T> Canceled<T>()
        {
            return Cached<T>.CanceledTask;
        }

        /// <summary>
        /// Creates a new <see cref="TaskCompletionSource{T}" /> and ensures the TaskCreationOptions.RunContinuationsAsynchronously
        /// flag is specified.
        /// </summary>
        /// <param name="options"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static TaskCompletionSource<T> GetTask<T>(TaskCreationOptions options = TaskCreationOptions.None)
        {
            return new TaskCompletionSource<T>(options | TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Creates a new TaskCompletionSource and ensures the TaskCreationOptions.RunContinuationsAsynchronously
        /// flag is specified.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static TaskCompletionSource<bool> GetTask(TaskCreationOptions options = TaskCreationOptions.None)
        {
            return new TaskCompletionSource<bool>(options | TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// Register a callback on the <paramref name="cancellationToken" /> which completes the resulting task.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="cancelTask"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static CancellationTokenRegistration RegisterTask(this CancellationToken cancellationToken, out Task cancelTask)
        {
            if (!cancellationToken.CanBeCanceled)
                throw new ArgumentException("The cancellationToken must support cancellation", nameof(cancellationToken));

            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            cancelTask = source.Task;

            return cancellationToken.Register(SetCompleted, source);
        }

        static void SetCompleted(object? obj)
        {
            if (obj is TaskCompletionSource<bool> source)
                source.SetCompleted();
        }

        public static CancellationTokenRegistration RegisterIfCanBeCanceled(this CancellationToken cancellationToken, CancellationTokenSource source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (cancellationToken.CanBeCanceled)
                return cancellationToken.Register(Cancel, source);

            return default;
        }

        static void Cancel(object? obj)
        {
            if (obj is CancellationTokenSource source)
                source.Cancel();
        }

        /// <summary>
        /// Sets the source to completed using TrySetResult
        /// </summary>
        /// <param name="source"></param>
        public static void SetCompleted(this TaskCompletionSource<bool> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            source.TrySetResult(true);
        }

        /// <summary>
        /// Blocks the calling thread until the task finishes and rethrows its exception unwrapped.
        /// <para>
        /// The task is not run here and does not run on the calling thread: it runs wherever it was
        /// started, and this call waits. The current <see cref="SynchronizationContext"/> is neither
        /// read, captured, installed nor replaced, and nothing is posted to it.
        /// </para>
        /// <para>
        /// This method does not pump the caller's synchronization context. The caller must therefore
        /// ensure that the task does not depend on that blocked context for its own completion.
        /// </para>
        /// </summary>
        public static void Await(Func<Task> taskFactory, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskFactory);

            var task = taskFactory();
            if (task == null)
                throw new InvalidOperationException("The taskFactory must return a Task");

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            task.GetAwaiter().GetResult();
        }

        public static void Await(Task task, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(task);

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            task.GetAwaiter().GetResult();
        }

        public static T Await<T>(Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(taskFactory);

            Task<T>? task = taskFactory();
            if (task == null)
                throw new InvalidOperationException("The taskFactory must return a Task");

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            return task.GetAwaiter().GetResult();
        }

        static class Cached
        {
            public static readonly Task<bool> TrueTask = Task.FromResult(true);
            public static readonly Task<bool> FalseTask = Task.FromResult(false);
        }


        static class Cached<T>
        {
            public static readonly Task<T?> DefaultValueTask = Task.FromResult<T?>(default);
            public static readonly Task<T> CanceledTask = GetCanceledTask();

            static Task<T> GetCanceledTask()
            {
                return Task.FromCanceled<T>(new CancellationToken(canceled: true));
            }
        }
    }
}
