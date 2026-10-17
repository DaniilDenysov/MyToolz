using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace MyToolz.Utilities.AutoLogger
{
    public static class LogFileWriter
    {
#if !UNITY_EDITOR
        private static StreamWriter logWriter;
        private static string logFilePath;
        private static bool initialized;
        private static LogFileWriterPreferences prefs;
        private static DateTime sessionStart;

        private static int countLog;
        private static int countWarning;
        private static int countError;
        private static int countException;

        private static readonly Dictionary<string, int> messageFrequency = new();
        private static readonly List<string> sceneHistory = new();

        private static float fpsMin = float.MaxValue;
        private static float fpsMax = float.MinValue;
        private static float fpsAccumulator;
        private static int fpsSampleCount;

        private static FpsSampler fpsSampler;

        // Bounds so a log storm (or a stalled disk) cannot grow memory without limit.
        private const int MaxQueuedLines = 10000;
        private const int MaxTrackedMessages = 1000;

        private static readonly ConcurrentQueue<string> writeQueue = new();
        // Serializes every StreamWriter access: the background drain and the final drain in Shutdown
        // must never touch the writer at the same time.
        private static readonly object writerLock = new();
        private static CancellationTokenSource writerCts;
        private static int queuedLines;
        private static int droppedLines;
        private static volatile bool writerFaulted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnLoad()
        {
            if (initialized) return;
            Init();
        }

        private static void Init()
        {
            try
            {
                initialized = true;
                sessionStart = DateTime.Now;

                prefs = LogFileWriterPreferences.Load();

                string logDirectory = prefs.ResolveLogDirectory();
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                EnforceLogRetention(logDirectory, prefs.maxLogFiles);

                string timestamp = sessionStart.ToString("yyyy-MM-dd_HH-mm-ss");
                logFilePath = Path.Combine(logDirectory, $"log_{timestamp}.txt");

                logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = false };

                writerCts = new CancellationTokenSource();
                RunWriterLoop(writerCts.Token).Forget();

                Application.logMessageReceived += HandleLog;

                if (prefs.trackSceneChanges)
                    SceneManager.sceneLoaded += OnSceneLoaded;

                if (prefs.trackFps)
                {
                    var go = new GameObject("[LogFileWriter_FpsSampler]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    fpsSampler = go.AddComponent<FpsSampler>();
                    fpsSampler.Initialize(prefs.fpsSampleIntervalSeconds, OnFpsSample);
                }

                Application.quitting += Shutdown;

                EnqueueHeader();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[LogFileWriter] Init failed: " + e.Message);
            }
        }

        private static async UniTaskVoid RunWriterLoop(CancellationToken token)
        {
#if !UNITY_WEBGL
            // WebGL has no worker threads; there the loop stays on the main thread.
            await UniTask.SwitchToThreadPool();
#endif

            while (!token.IsCancellationRequested)
            {
                DrainQueue();
                await UniTask.Delay(32, DelayType.Realtime, cancellationToken: token).SuppressCancellationThrow();
            }
        }

        private static void DrainQueue()
        {
            if (writeQueue.IsEmpty) return;

            string error = null;
            lock (writerLock)
            {
                if (logWriter == null || writerFaulted) return;

                try
                {
                    int dropped = Interlocked.Exchange(ref droppedLines, 0);
                    if (dropped > 0)
                        logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [WARN ] [LogFileWriter] {dropped} log line(s) dropped: the write queue was full.");

                    while (writeQueue.TryDequeue(out string line))
                    {
                        Interlocked.Decrement(ref queuedLines);
                        logWriter.WriteLine(line);
                    }

                    logWriter.Flush();
                }
                catch (Exception e)
                {
                    // Stop writing instead of retrying every frame: the error below goes through
                    // Application.logMessageReceived, which would feed it straight back into this queue.
                    writerFaulted = true;
                    error = e.Message;
                }
            }

            if (error != null)
            {
                UnityEngine.Debug.LogError("[LogFileWriter] Write error, file logging stopped: " + error);
            }
        }

        private static void Enqueue(string line)
        {
            if (writerFaulted) return;

            if (Interlocked.Increment(ref queuedLines) > MaxQueuedLines)
            {
                Interlocked.Decrement(ref queuedLines);
                Interlocked.Increment(ref droppedLines);
                return;
            }

            writeQueue.Enqueue(line);
        }

        private static void EnqueueHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine($"  Session Started : {sessionStart:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Application     : {Application.productName} v{Application.version}");
            sb.AppendLine($"  Platform        : {Application.platform}");
            sb.AppendLine($"  Unity Version   : {Application.unityVersion}");
            sb.AppendLine($"  Device          : {SystemInfo.deviceModel} ({SystemInfo.operatingSystem})");
            sb.AppendLine($"  CPU             : {SystemInfo.processorType} x{SystemInfo.processorCount}");
            sb.AppendLine($"  RAM             : {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"  GPU             : {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)");
            sb.AppendLine($"  Log file        : {logFilePath}");
            sb.Append(new string('=', 60));
            Enqueue(sb.ToString());
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Log:       countLog++;       break;
                case LogType.Warning:   countWarning++;   break;
                case LogType.Error:     countError++;     break;
                case LogType.Exception: countException++; break;
            }

            if (prefs.trackStatistics)
            {
                string key = condition.Length > 120 ? condition[..120] : condition;
                if (messageFrequency.TryGetValue(key, out int existing))
                    messageFrequency[key] = existing + 1;
                else if (messageFrequency.Count < MaxTrackedMessages)
                    messageFrequency[key] = 1;
            }

            string tag = type switch
            {
                LogType.Warning   => "WARN ",
                LogType.Error     => "ERROR",
                LogType.Exception => "EXCPT",
                LogType.Assert    => "ASSRT",
                _                 => "INFO "
            };

            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {condition}";

            if (type == LogType.Error || type == LogType.Exception)
                entry += Environment.NewLine + stackTrace;

            Enqueue(entry);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string previous = sceneHistory.Count > 0 ? sceneHistory[^1] : "None";
            sceneHistory.Add(scene.name);
            Enqueue($"[{DateTime.Now:HH:mm:ss.fff}] [SCENE] Transition: {previous} → {scene.name} (mode: {mode})");
        }

        private static void OnFpsSample(float fps)
        {
            if (fps < fpsMin) fpsMin = fps;
            if (fps > fpsMax) fpsMax = fps;
            fpsAccumulator += fps;
            fpsSampleCount++;
        }

        private static void EnforceLogRetention(string directory, int maxFiles)
        {
            if (maxFiles <= 0) return;

            var files = Directory.GetFiles(directory, "log_*.txt")
                                 .OrderBy(f => File.GetCreationTimeUtc(f))
                                 .ToArray();

            int toDelete = files.Length - maxFiles + 1;
            for (int i = 0; i < toDelete && i < files.Length; i++)
            {
                try { File.Delete(files[i]); }
                catch { }
            }
        }

        public static void Shutdown()
        {
            if (!initialized) return;

            Application.logMessageReceived -= HandleLog;
            Application.quitting           -= Shutdown;

            if (prefs.trackSceneChanges)
                SceneManager.sceneLoaded -= OnSceneLoaded;

            EnqueueSummary();

            writerCts?.Cancel();

            // Takes the writer lock, so it waits for a drain already running on the background thread.
            DrainQueue();

            lock (writerLock)
            {
                try
                {
                    logWriter?.Close();
                }
                catch (Exception)
                {
                    // Nothing more can be written; the session is ending.
                }

                logWriter = null;
            }

            writerCts?.Dispose();
            writerCts = null;
            initialized = false;
        }

        private static void EnqueueSummary()
        {
            var sessionDuration = DateTime.Now - sessionStart;
            float avgFps = fpsSampleCount > 0 ? fpsAccumulator / fpsSampleCount : 0f;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(new string('=', 60));
            sb.AppendLine("  SESSION SUMMARY");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"  Session ended   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Duration        : {sessionDuration:hh\\:mm\\:ss}");
            sb.AppendLine();
            sb.AppendLine("  LOG COUNTS");
            sb.AppendLine($"    Info          : {countLog}");
            sb.AppendLine($"    Warnings      : {countWarning}");
            sb.AppendLine($"    Errors        : {countError}");
            sb.AppendLine($"    Exceptions    : {countException}");
            sb.AppendLine($"    Total         : {countLog + countWarning + countError + countException}");

            if (prefs.trackFps && fpsSampleCount > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  FPS");
                sb.AppendLine($"    Min           : {fpsMin:F1}");
                sb.AppendLine($"    Avg           : {avgFps:F1}");
                sb.AppendLine($"    Max           : {fpsMax:F1}");
                sb.AppendLine($"    Samples       : {fpsSampleCount}");
            }

            if (prefs.trackSceneChanges && sceneHistory.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  SCENE HISTORY");
                sb.AppendLine($"    None → {sceneHistory[0]}");
                for (int i = 1; i < sceneHistory.Count; i++)
                    sb.AppendLine($"    {sceneHistory[i - 1]} → {sceneHistory[i]}");
            }

            if (prefs.trackStatistics && messageFrequency.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("  TOP 10 MOST FREQUENT MESSAGES");
                var top = messageFrequency.OrderByDescending(kv => kv.Value).Take(10);
                int rank = 1;
                foreach (var kv in top)
                    sb.AppendLine($"    #{rank++:D2} ({kv.Value}x) {kv.Key}");
            }

            sb.Append(new string('=', 60));
            Enqueue(sb.ToString());
        }
#endif
    }

    public class FpsSampler : MonoBehaviour
    {
        private float interval;
        private Action<float> onSample;
        private float timer;

        public void Initialize(float intervalSeconds, Action<float> callback)
        {
            interval = intervalSeconds;
            onSample = callback;
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < interval)
            {
                return;
            }

            timer -= interval;
            onSample?.Invoke(1f / Time.unscaledDeltaTime);
        }
    }
}
