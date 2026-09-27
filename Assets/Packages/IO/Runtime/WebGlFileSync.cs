using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MyToolz.IO
{
    /// <summary>
    /// Coordinates WebGL persistence operations and flushes Unity's IDBFS-backed
    /// storage to IndexedDB. The queue is deliberately single-threaded so WebGL does
    /// not depend on SemaphoreSlim/System.Threading support.
    /// </summary>
    internal static class WebGlFileSync
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private delegate void SyncCallback(int statusCode);

        private sealed class QueuedOperation
        {
            public readonly Func<Task> Operation;
            public readonly TaskCompletionSource<bool> Completion;

            public QueuedOperation(Func<Task> operation)
            {
                Operation = operation;
                Completion = new TaskCompletionSource<bool>();
            }
        }

        private static readonly Queue<QueuedOperation> OperationQueue = new Queue<QueuedOperation>();
        private static readonly SyncCallback NativeCallback = OnNativeSyncCompleted;

        private static TaskCompletionSource<bool> pendingSync;
        private static bool queueRunning;
        private static bool persistenceStateUncertain;

        [DllImport("__Internal")]
        private static extern void MyToolz_SyncFsToIndexedDb(SyncCallback callback);

        /// <summary>
        /// Runs one complete virtual-storage mutation + flush cycle at a time. This
        /// prevents a later mutation from entering IDBFS while an earlier syncfs call
        /// is still committing the previous state.
        /// </summary>
        public static Task RunExclusiveAsync(Func<Task> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (persistenceStateUncertain)
            {
                return Task.FromException(new IOException(
                    "WebGL persistence is in an uncertain state because a previous IndexedDB synchronization timed out. " +
                    "Further writes are blocked for this session to avoid overlapping an unknown syncfs operation. Reload the page before saving again."));
            }

            var item = new QueuedOperation(operation);
            OperationQueue.Enqueue(item);
            StartPump();
            return item.Completion.Task;
        }

        // Not async void: the pump is a Task whose unexpected faults are logged instead of being
        // thrown onto the synchronization context where nothing would observe them.
        private static void StartPump()
        {
            Task pump = PumpQueueAsync();
            if (!pump.IsCompleted || pump.IsFaulted)
            {
                pump.ContinueWith(
                    t => UnityEngine.Debug.LogException(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        private static async Task PumpQueueAsync()
        {
            if (queueRunning)
            {
                return;
            }

            queueRunning = true;
            try
            {
                while (OperationQueue.Count > 0)
                {
                    QueuedOperation item = OperationQueue.Dequeue();

                    if (persistenceStateUncertain)
                    {
                        item.Completion.TrySetException(new IOException(
                            "WebGL persistence is in an uncertain state after an IndexedDB synchronization timeout. Reload the page before saving again."));
                        continue;
                    }

                    try
                    {
                        await item.Operation();
                        item.Completion.TrySetResult(true);
                    }
                    catch (Exception e)
                    {
                        item.Completion.TrySetException(e);
                    }
                }
            }
            finally
            {
                queueRunning = false;

                // A completion continuation can enqueue new work while the pump is
                // unwinding. Start it immediately instead of leaving it stranded.
                if (OperationQueue.Count > 0)
                {
                    StartPump();
                }
            }
        }

        /// <summary>
        /// Flushes the currently-mutated WebGL filesystem. Call this only from inside
        /// RunExclusiveAsync so there can be at most one native syncfs request pending.
        /// JavaScript performs bounded retries for explicitly transient IndexedDB
        /// errors and reports a timeout separately.
        /// </summary>
        public static Task FlushAsync()
        {
            if (persistenceStateUncertain)
            {
                return Task.FromException(new IOException(
                    "WebGL IndexedDB persistence is blocked because a previous sync timed out."));
            }

            if (pendingSync != null)
            {
                return Task.FromException(new InvalidOperationException(
                    "A WebGL IndexedDB synchronization is already pending. Persistence operations must be serialized through RunExclusiveAsync."));
            }

            var completion = new TaskCompletionSource<bool>();
            pendingSync = completion;

            try
            {
                MyToolz_SyncFsToIndexedDb(NativeCallback);
            }
            catch (Exception e)
            {
                pendingSync = null;
                completion.TrySetException(e);
            }

            return completion.Task;
        }

        [AOT.MonoPInvokeCallback(typeof(SyncCallback))]
        private static void OnNativeSyncCompleted(int statusCode)
        {
            TaskCompletionSource<bool> completion = pendingSync;
            if (completion == null)
            {
                return;
            }

            pendingSync = null;

            if (statusCode > 0)
            {
                completion.TrySetResult(true);
                return;
            }

            if (statusCode == -2)
            {
                // We cannot cancel an in-flight browser syncfs call. Do not start a
                // later write that could overlap it; fail future requests fast until
                // the page is reloaded.
                persistenceStateUncertain = true;
                completion.TrySetException(new IOException(
                    "WebGL IndexedDB synchronization timed out. The browser did not confirm whether the write committed. " +
                    "Further writes are blocked for this session; reload the page before saving again."));
                return;
            }

            completion.TrySetException(new IOException(
                "WebGL IndexedDB synchronization failed after bounded retry. Check the browser console for the underlying FS.syncfs/IndexedDB error."));
        }
#else
        public static Task RunExclusiveAsync(Func<Task> operation) => operation();
        public static Task FlushAsync() => Task.CompletedTask;
#endif
    }
}
