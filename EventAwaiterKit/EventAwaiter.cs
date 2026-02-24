using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventAwaiterKit
{
    /// <summary>
    /// Pure, stateless helpers to “await” any event or delegate callback, with timeout + cancellation.
    /// </summary>
    public static class EventAwaiter
    {
        // —— Action —— //

        public static async Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var timeoutCts = new CancellationTokenSource(timeout))
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                void handler()
                {
                    removeHandler(handler);
                    tcs.TrySetResult(true);
                }

                addHandler(handler);

                using (timeoutCts.Token.Register(() =>
                {
                    removeHandler(handler);
                    tcs.TrySetResult(false);
                }))
                using (cancellationToken.Register(() =>
                {
                    removeHandler(handler);
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    try
                    {
                        return await tcs.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        removeHandler(handler);
                    }
                }
            }
        }

        public static Task<bool> WaitForEventAsync(Action<Action> addHandler, Action<Action> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);


        // —— EventHandler —— //

        public static async Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var timeoutCts = new CancellationTokenSource(timeout))
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                void handler(object s, EventArgs e)
                {
                    removeHandler(handler);
                    tcs.TrySetResult(true);
                }

                addHandler(handler);

                using (timeoutCts.Token.Register(() =>
                {
                    removeHandler(handler);
                    tcs.TrySetResult(false);
                }))
                using (cancellationToken.Register(() =>
                {
                    removeHandler(handler);
                    tcs.TrySetCanceled(cancellationToken);
                }))
                {
                    try
                    {
                        return await tcs.Task.ConfigureAwait(false);
                    }
                    finally
                    {
                        removeHandler(handler);
                    }
                }
            }
        }

        public static Task<bool> WaitForEventAsync(Action<EventHandler> addHandler, Action<EventHandler> removeHandler, CancellationToken cancellationToken = default)
            => WaitForEventAsync(addHandler, removeHandler, Timeout.InfiniteTimeSpan, cancellationToken);
    }
}