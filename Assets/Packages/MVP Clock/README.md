# MVP Clock

A clock system with stopwatch and timer functionalities built on the MVP pattern. Integrates with the UI Management System for screen lifecycle.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| MVP UI Management System | `com.mytoolz.mvpuimanagementsystem` |
| MVP | `com.mytoolz.mvp` |
| Prototype | `com.mytoolz.prototype` |

External: UniTask (`Cysharp.Threading.Tasks`), TextMeshPro.

## Structure

```
Runtime/
├── ClockInterfaces.cs    IClock, IStopwatch, ITimer contracts
├── ClockModel.cs         Data model holding time state
├── ClockPresenter.cs     Presenter driving clock tick and UI updates
└── ClockView.cs          View rendering time display via TextMeshPro
Optional/
└── ClockConfiguration.cs Example configuration setup
```
