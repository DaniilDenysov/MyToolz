using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace MyToolz.BuildGate
{
    /// <summary>
    /// Gates player builds on the MyToolz core-systems test suite: the tests run first, and a build
    /// is only produced when they pass. This protects the reusable packages (EventBus, DebugUtility,
    /// IO, MVP, EditorToolz, StateMachine, InputManagement, SceneManagement, Audio, Tweener,
    /// ObjectPool, Singleton) from silently regressing in a shipped build.
    ///
    /// CI usage (do NOT pass -quit; the gate calls EditorApplication.Exit itself once the
    /// asynchronous test run finishes):
    ///   Unity -batchmode -projectPath &lt;path&gt; \
    ///         -executeMethod MyToolz.BuildGate.CoreTestsBuildGate.CI_TestAndBuild
    ///
    /// Environment variables:
    ///   MYTOOLZ_TEST_MODES = EditMode | PlayMode | All   (default: EditMode)
    ///   MYTOOLZ_SKIP_TEST_GATE = 1                        (bypass the build-time enforcement)
    ///   -buildOutput &lt;path&gt;                            (CLI arg: override the build output path)
    ///
    /// <see cref="CoreTestsBuildPreprocessor"/> enforces the gate for headless builds started by any
    /// other route, so a build cannot be produced in CI without the tests having passed this session.
    /// </summary>
    public static class CoreTestsBuildGate
    {
        /// <summary>SessionState flag (survives domain reloads within one editor session).</summary>
        public const string TestsPassedKey = "MyToolz.BuildGate.TestsPassedThisSession";
        public const string SkipEnvVar = "MYTOOLZ_SKIP_TEST_GATE";
        private const string TestModesEnvVar = "MYTOOLZ_TEST_MODES";

        // Kept alive across the asynchronous run so the API object and callbacks are not collected.
        private static TestRunnerApi _api;
        private static Queue<TestMode> _pending;
        private static bool _allPassed;
        private static Action<bool> _onAllComplete;

        [MenuItem("MyToolz/Tests/Run Core Tests")]
        public static void RunCoreTestsMenu()
        {
            RunCoreTests(passed => Debug.Log(passed
                ? "[BuildGate] Core tests PASSED."
                : "[BuildGate] Core tests FAILED — see the Test Runner window."));
        }

        /// <summary>CI entry point: run the core tests, then build the player only if they pass.</summary>
        public static void CI_TestAndBuild()
        {
            SessionState.SetBool(TestsPassedKey, false);

            RunCoreTests(passed =>
            {
                if (!passed)
                {
                    Debug.LogError("[BuildGate] Core tests FAILED — build aborted.");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }

                SessionState.SetBool(TestsPassedKey, true);
                try
                {
                    BuildResult result = PerformBuild();
                    Debug.Log($"[BuildGate] Build finished: {result}.");
                    if (Application.isBatchMode) EditorApplication.Exit(result == BuildResult.Succeeded ? 0 : 1);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BuildGate] Build threw: {e}");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                }
            });
        }

        /// <summary>Runs the configured test modes sequentially and reports whether all passed.</summary>
        public static void RunCoreTests(Action<bool> onComplete)
        {
            _onAllComplete = onComplete;
            _allPassed = true;
            _pending = new Queue<TestMode>(ResolveModes());
            RunNextMode();
        }

        private static void RunNextMode()
        {
            if (_pending == null || _pending.Count == 0)
            {
                var done = _onAllComplete;
                _onAllComplete = null;
                done?.Invoke(_allPassed);
                return;
            }

            TestMode mode = _pending.Dequeue();
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(new ResultWatcher(mode, passed =>
            {
                _allPassed &= passed;
                RunNextMode();
            }));
            _api.Execute(new ExecutionSettings(new Filter { testMode = mode }));
        }

        private static IEnumerable<TestMode> ResolveModes()
        {
            string value = Environment.GetEnvironmentVariable(TestModesEnvVar);
            if (string.Equals(value, "PlayMode", StringComparison.OrdinalIgnoreCase))
                return new[] { TestMode.PlayMode };
            if (string.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
                return new[] { TestMode.EditMode, TestMode.PlayMode };
            // Default: EditMode only — fast, deterministic, and free of play-mode domain-reload
            // hazards that make build orchestration across PlayMode runs unreliable in headless CI.
            return new[] { TestMode.EditMode };
        }

        private static BuildResult PerformBuild()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                throw new Exception("[BuildGate] No enabled scenes in Build Settings; nothing to build.");

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                locationPathName = ResolveBuildPath(target),
                options = BuildOptions.None,
            };

            return BuildPipeline.BuildPlayer(options).summary.result;
        }

        private static string ResolveBuildPath(BuildTarget target)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-buildOutput")
                    return args[i + 1];

            string ext = (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows) ? ".exe"
                       : target == BuildTarget.StandaloneOSX ? ".app"
                       : string.Empty;
            return $"Builds/{target}/{Application.productName}{ext}";
        }

        private sealed class ResultWatcher : ICallbacks
        {
            private readonly TestMode _mode;
            private readonly Action<bool> _onComplete;

            public ResultWatcher(TestMode mode, Action<bool> onComplete)
            {
                _mode = mode;
                _onComplete = onComplete;
            }

            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log($"[BuildGate] {_mode} tests: {result.PassCount} passed, " +
                          $"{result.FailCount} failed, {result.SkipCount} skipped.");
                _onComplete?.Invoke(result.FailCount == 0 && result.TestStatus != TestStatus.Failed);
            }
        }
    }

    /// <summary>
    /// Blocks headless (batchmode) builds unless the core tests have passed this editor session via
    /// <see cref="CoreTestsBuildGate"/>. Interactive editor builds are left alone so day-to-day
    /// iteration is not interrupted. Set <c>MYTOOLZ_SKIP_TEST_GATE=1</c> to bypass.
    /// </summary>
    public sealed class CoreTestsBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -10000; // run before other preprocessors

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CoreTestsBuildGate.SkipEnvVar)))
                return;

            if (!Application.isBatchMode)
                return; // don't gate interactive editor builds

            if (SessionState.GetBool(CoreTestsBuildGate.TestsPassedKey, false))
                return;

            throw new BuildFailedException(
                "[BuildGate] Core tests have not passed in this session. Build via " +
                "'CoreTestsBuildGate.CI_TestAndBuild' (which runs the tests first), or set " +
                $"{CoreTestsBuildGate.SkipEnvVar}=1 to bypass.");
        }
    }
}
