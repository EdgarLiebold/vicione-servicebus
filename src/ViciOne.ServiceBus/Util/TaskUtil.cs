#nullable enable
namespace ViciOne.ServiceBus.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;


    public static class TaskUtil
    {
        internal static Task Canceled => Cached<bool>.CanceledTask;
        public static Task Completed => Cached.CompletedTask;
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
        /// Returns a faulted task with the specified exception (creating using a <see cref="TaskCompletionSource{T}" />)
        /// </summary>
        /// <param name="exception"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<T> Faulted<T>(Exception exception)
        {
            TaskCompletionSource<T> source = GetTask<T>();
            source.TrySetException(exception);

            return source.Task;
        }

        /// <summary>
        /// Returns a cancelled task for the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Task<T> Cancelled<T>()
        {
            return Cached<T>.CanceledTask;
        }

        /// <summary>
        /// Creates a new <see cref="TaskCompletionSource{T}" />, and ensures the TaskCreationOptions.RunContinuationsAsynchronously
        /// flag is specified (if available).
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
        /// flag is specified (if available).
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
            source.TrySetResult(true);
        }

        /// <summary>
        /// Runs the task to completion on the calling thread and rethrows its exception unwrapped.
        /// <para>
        /// A continuation posted to the current <see cref="SynchronizationContext"/> is not waited for
        /// here: the awaiter itself blocks, and the context keeps whatever affinity it has. Windows
        /// Forms and WPF dispatchers, reached by reflection, and an STA specific single threaded
        /// context used to sit in front of this. None of them could ever run on this product's only
        /// platform, where every call already took exactly the path below.
        /// </para>
        /// </summary>
        public static void Await(Func<Task> taskFactory, CancellationToken cancellationToken = default)
        {
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));

            var task = taskFactory();
            if (task == null)
                throw new InvalidOperationException("The taskFactory must return a Task");

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            new TaskAwaitAdapter(task).GetResult();
        }

        public static void Await(Task task, CancellationToken cancellationToken = default)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            new TaskAwaitAdapter(task).GetResult();
        }

        public static T Await<T>(Func<Task<T>> taskFactory, CancellationToken cancellationToken = default)
        {
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));

            Task<T>? task = taskFactory();
            if (task == null)
                throw new InvalidOperationException("The taskFactory must return a Task");

            if (cancellationToken.CanBeCanceled)
                task = task.OrCanceled(cancellationToken);

            return new TaskAwaitAdapter<T>(task).GetResultOfT();
        }

        static class Cached
        {
            public static readonly Task CompletedTask = Task.FromResult(true);
            public static readonly Task<bool> TrueTask = Task.FromResult(true);
            public static readonly Task<bool> FalseTask = Task.FromResult(false);
        }


        static class Cached<T>
        {
            public static readonly Task<T?> DefaultValueTask = Task.FromResult<T?>(default);
            public static readonly Task<T> CanceledTask = GetCanceledTask();

            static Task<T> GetCanceledTask()
            {
                TaskCompletionSource<T> source = GetTask<T>();
                source.SetCanceled();
                return source.Task;
            }
        }




        abstract class AwaitAdapter
        {
            public abstract bool IsCompleted { get; }
            public abstract void OnCompleted(Action action);
            public abstract void GetResult();
        }


        sealed class TaskAwaitAdapter :
            AwaitAdapter
        {
            readonly TaskAwaiter _awaiter;

            public TaskAwaitAdapter(Task task)
            {
                _awaiter = task.GetAwaiter();
            }

            public override bool IsCompleted => _awaiter.IsCompleted;

            public override void OnCompleted(Action action)
            {
                _awaiter.UnsafeOnCompleted(action);
            }

            public override void GetResult()
            {
                _awaiter.GetResult();
            }
        }


        sealed class TaskAwaitAdapter<T> :
            AwaitAdapter
        {
            readonly TaskAwaiter<T> _awaiter;

            public TaskAwaitAdapter(Task<T> task)
            {
                _awaiter = task.GetAwaiter();
            }

            public override bool IsCompleted => _awaiter.IsCompleted;

            public override void OnCompleted(Action action)
            {
                _awaiter.UnsafeOnCompleted(action);
            }

            public override void GetResult()
            {
                _awaiter.GetResult();
            }

            public T GetResultOfT()
            {
                return _awaiter.GetResult();
            }
        }
    }
}
