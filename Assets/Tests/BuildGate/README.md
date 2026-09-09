# Core Tests Build Gate

Runs the MyToolz core-systems tests **before** the build pipeline, so a player build is only
produced when the reusable packages still pass their tests.

## What it does

- `CoreTestsBuildGate.CI_TestAndBuild` runs the test suite via the Test Framework `TestRunnerApi`
  and, only if everything passes, calls `BuildPipeline.BuildPlayer`. In batchmode it sets the
  process exit code (`0` pass/build ok, `1` otherwise).
- `CoreTestsBuildPreprocessor` (an `IPreprocessBuildWithReport`) blocks **headless/batchmode**
  builds started by any other route unless the tests passed this editor session. Interactive
  editor builds are never blocked.

## CI usage

```bash
# Do NOT pass -quit: the gate calls EditorApplication.Exit itself when the async run finishes.
Unity -batchmode -projectPath "$PROJECT_PATH" \
      -executeMethod MyToolz.BuildGate.CoreTestsBuildGate.CI_TestAndBuild
```

## Environment variables

| Variable | Values | Default | Effect |
|---|---|---|---|
| `MYTOOLZ_TEST_MODES` | `EditMode` / `PlayMode` / `All` | `EditMode` | Which test modes gate the build. |
| `MYTOOLZ_SKIP_TEST_GATE` | `1` | unset | Bypass the build-time enforcement. |
| `-buildOutput <path>` (CLI arg) | any path | `Builds/<target>/<product>` | Build output location. |

`EditMode` is the default because it is fast, deterministic, and free of the play-mode
domain-reload hazards that make gating a build on PlayMode runs unreliable in headless CI.
The PlayMode suites (Object Pool, Singleton) are best run as their own Test Runner / CI step.
