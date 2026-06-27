# Bootstrap

A minimal application entry point plus an editor utility for always launching
play mode from your boot scene.

## Dependencies

- `MyToolz.Singleton`

## Structure

```
Runtime/
└── Bootstrapper.cs      PrivateSingleton<Bootstrapper> entry point
Editor/
└── BootstrapButton.cs   Adds a "Boot" button to the main toolbar
```

## Usage

`Bootstrapper` is a `PrivateSingleton<Bootstrapper>` intended to live in a scene
named `Bootstrapper` that is the first scene in your build. Extend it (or attach
it) to perform one-time application setup. Put cross-cutting initialization
(service installers, persistent managers) on that scene.

### Editor "Boot" button

`BootstrapButton` registers a **Boot** button in the main editor toolbar. Pressing
it saves the current scene, opens the scene named `Bootstrapper`, and enters play
mode; on exit it restores the scene you were editing. This lets you press Play
from anywhere while always booting through the real entry point.

> If the button is not visible, right-click the toolbar and enable
> **MyToolz -> BootLauncher** (one-time setup).
