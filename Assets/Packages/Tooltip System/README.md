# Tooltip System

An event-driven tooltip system using the EventBus and a Singleton tooltip controller. Supports configurable tooltip areas that trigger display on pointer enter/exit.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Event Bus | `com.mytoolz.eventbus` |
| Singleton | `com.mytoolz.singleton` |
| Editor Toolz | `com.mytoolz.editortoolz` |

External: Unity Input System, TextMeshPro.

## Structure

```
Runtime/
├── TooltipArea.cs     MonoBehaviour that detects pointer enter/exit and raises tooltip events
└── TooltipSystem.cs   Singleton that listens for tooltip events and manages the tooltip UI
```

## Setup

Add `TooltipSystem` to a persistent GameObject in your scene. Place `TooltipArea` components on any UI element or world object that should trigger a tooltip. Configure the tooltip content and positioning on each `TooltipArea`.
