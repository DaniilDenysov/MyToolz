# MVP Notifications

A priority-driven notification queue system for Unity built on the Model-View-Presenter pattern.  
Notifications are displayed through an object pool, support priority ordering, deduplication, and configurable overflow behavior.

## Dependencies

| Package | ID |
|---|---|
| MVP | `com.mytoolz.mvp` |
| EventBus | `com.mytoolz.eventbus` |
| Object Pool | `com.mytoolz.objectpool` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Debug Utility | `com.mytoolz.debugutility` |
| DOTween | `com.demigiant.dotween` |
| TextMeshPro | `com.unity.textmeshpro` |

## Architecture

```
EventBus
   │
   ▼
┌──────────────────┐    intent    ┌───────────────────────┐
│    Presenter     │ ───────────► │         View           │
│                  │              │ (PlayerNotificationV.) │
└────────┬─────────┘              └───────────┬───────────┘
         │                                    │
         │ outcome                            │ pool / hierarchy
         │                                    │
┌────────▼─────────┐              ┌───────────▼───────────┐
│      Model       │              │   NotificationBase    │
│ (NotifQueueModel)│              │   ├ TextNotification  │
└──────────────────┘              │   ├ TimedNotification  │
                                  │   │  ├ KillNotif.     │
                                  │   │  └ AmmoNotif.     │
                                  │   └ NullNotification  │
                                  └───────────────────────┘
```

**Model** (`NotificationQueueModel`) — Owns all queue state: active entries, pending entries, key presence tracking. Handles overflow eviction, deduplication, and priority sorting. Uses integer IDs to reference notifications.

**View** (`PlayerNotificationView`) — Owns all visual orchestration: prefab mapping, object pool interaction, instance tracking, hierarchy reordering, spawning, replacing, and releasing. The presenter never touches instances directly.

**Presenter** (`NotificationPresenter`) — Thin orchestrator. Listens to EventBus events, asks the Model for outcomes, then forwards those outcomes to the View. Contains zero view-handling logic.

**NotificationBase hierarchy** — Each notification type is a concrete MonoBehaviour subclass. The type itself (`typeof(KillNotification)`) serves as the prefab key and the `MessageType` in requests.

## Notification Hierarchy

```
NotificationBase (abstract)
├── TextNotification        Fades in, stays until manually stopped, fades out.
├── TimedNotification       (abstract) Fades in with scale, holds, auto-fades out.
│   ├── KillNotification    Kill feeds, hit markers.
│   └── AmmoNotification    Ammo pickups, item acquisitions.
└── NullNotification        No-op, immediately hidden. For testing.
```

## Setup

1. Create notification prefabs. Each prefab is a GameObject with a concrete `NotificationBase` subclass:
   - Add the component (`KillNotification`, `AmmoNotification`, `TextNotification`, etc.)
   - Assign the `CanvasGroup` and `TMP_Text` references
   - Configure timing/animation fields in the inspector

2. Scene hierarchy:
   ```
   NotificationInstaller  (MonoInstaller on SceneContext)
   PlayerNotificationView (MonoBehaviour)
   └── Container          (Transform — notifications are spawned here)
   ```

3. On `PlayerNotificationView`, assign:
   - **Container** — the Transform where notification instances will be parented
   - **Notification Prefabs** — array of all `NotificationBase` prefabs

4. On `NotificationInstaller`, assign:
   - **Max Active** — maximum simultaneously visible notifications (1–10)
   - **View** — reference to the `PlayerNotificationView`

5. Add `NotificationInstaller` to your `SceneContext`'s installer list.

6. Add a `NotificationObjectPool` installer to your scene for pooling.

## Sending Notifications

```csharp
using MyToolz.DesignPatterns.EventBus;
using MyToolz.UI.Events;
using MyToolz.UI.Notifications.Model;
using MyToolz.UI.Notifications.View;

EventBus<NotificationRequest>.Raise(new NotificationRequest
{
    Key         = "player_kill",
    MessageType = typeof(KillNotification),
    Text        = "Headshot!",
    Priority    = NotificationPriority.High,
    Overflow    = OverflowPolicy.DropLowestPriority,
    Dedupe      = DedupePolicy.None
});
```

### Fields

| Field | Purpose |
|---|---|
| `Key` | Identifier for deduplication and targeted clearing. Defaults to `MessageType.FullName` if empty. |
| `MessageType` | The concrete `NotificationBase` subclass type. Determines which prefab is used. |
| `Text` | Message string displayed on the notification. |
| `Priority` | `Low (0)`, `Normal (10)`, `High (20)`, `Critical (30)`. Higher = displayed first, sorted to top. |
| `Overflow` | Behavior when `maxActive` is reached. |
| `Dedupe` | Behavior when a notification with the same `Key` already exists. |

## Clearing Notifications

```csharp
EventBus<NotificationClearRequest>.Raise(new NotificationClearRequest
{
    Key         = "ammo_low",
    MessageType = typeof(TextNotification)
});
```

## Overflow Policies

| Policy | Behavior |
|---|---|
| `None` | Dropped. |
| `DropNew` | Incoming notification is discarded. |
| `DropOldest` | Oldest active notification is force-released. |
| `DropLowestPriority` | Lowest priority active notification is force-released. Ties broken by age. |
| `ReplaceSameKeyOrDropNew` | Replaces matching key if exists, otherwise dropped. |

## Deduplication Policies

| Policy | Behavior |
|---|---|
| `None` | Duplicates allowed. |
| `IgnoreIfSameKeyExists` | Incoming silently dropped. |
| `ReplaceIfSameKeyExists` | Existing replaced in-place (preserving hierarchy position). |

## Custom Notifications

Subclass `NotificationBase` (or `TimedNotification` for auto-hiding):

```csharp
public class BounceNotification : TimedNotification
{
    // Override Play() and Stop() for custom animations.
    // Call NotifyHidden() when fully hidden.
    // Use KillTween / KillSequence helpers for cleanup.
}
```

Create a prefab with the new component, add it to the `PlayerNotificationView` prefabs array, and reference `typeof(BounceNotification)` in requests.

## Notification Lifecycle

```
NotificationRequest raised
        │
        ▼
  Presenter validates MessageType
        │
        ▼
  Model.TryAdd() ──► Dedupe / Overflow / Enqueue logic
        │
        ▼
  Outcome (Spawned / Replaced / Enqueued / Dropped)
        │
        ▼
  Presenter forwards outcome to View
        │
        ▼
  View.HandleAdded() ──► pool request, set message, register callback
  View.Reorder()     ──► sort container children by priority
        │
        ▼
  ... animation plays (managed by NotificationBase subclass) ...
        │
        ▼
  NotifyHidden() fires
        │
        ▼
  Presenter: Model.RemoveActiveById()
  Presenter: View.HandleEvicted() ──► release to pool
  Presenter: TryPromotePending()
```
