# Audio (SFX & adaptive music)

Package: `Assets/Packages/Audio` · Namespaces: `MyToolz.Audio`, events in `MyToolz.Audio.Events`

An **event-driven, pooled** audio system. You never `Instantiate` an `AudioSource` or call
`PlayOneShot` yourself — you raise an event, and a manager pulls a pooled source, configures
it from a ScriptableObject, plays, and returns it. Two independent halves: **SFX**
(`AudioManager`) and **music** (`MXManager`). Both are `PrivateSingleton`s that self-register
to the `EventBus` — so you talk to them through events, not a static `Instance` (see
[singleton.md](singleton.md), [event-bus.md](event-bus.md)).

## Scene setup (once)

1. Add an **`AudioSourceObjectPool`** (a `DefaultObjectPoolInstaller<AudioSourceWrapper>`) to
   the scene and list an `AudioSourceWrapper` prefab — this is the pool both managers draw
   from. Without it, play requests log **"No pool found"** and nothing sounds (see
   [object-pool.md](object-pool.md)).
2. Add **`AudioManager`** and assign its `defaultPrefab` (the same `AudioSourceWrapper` prefab).
3. For music, add **`MXManager`** and assign `audioSourcePrefab` (+ optional `defaultSong`,
   `playOnAwake`).

## The assets

- **`AudioSourceConfigSO`** (`MyToolz/AudioSystem/AudioSourceConfig`) — *how* a source plays:
  mixer group, `isGlobal` (forces 2D — `spatialBlend` 0), volume (+ optional randomized range),
  `spatialBlend`, `playOnAwake`, `loop`, `pitch` (+ optional randomized range), `bypassEffects`.
- **`AudioClipSO`** (`MyToolz/AudioSystem/AudioClipSO`) — *what* to play: a single clip, or
  `randomize` over an array, or `useAudioConfigPerClip` for a per-clip clip+config list. Carries
  a `minimalInterval` cooldown and an optional `globalSoundConfig`. `GetClipAndConfig()` picks
  the clip/config; `IsOnCooldown` / `MarkPlayed` / `ResetCooldown` gate repeats.
- **`SongSO`** (`MyToolz/Audio/SongSO`) — a music track as **intensity layers**: an ordered
  `intensityClips` list (layer 0 = calmest), a `reverbTail`, and an `AudioSourceConfigSO`.

## Playing SFX

Raise `PlayAudioClipSO` — `AudioManager` throttles per-`AudioClipSO` (using `IntervalOverload`,
or the SO's `MinimalInterval` when `IntervalOverload < 0`), then pools a source and plays. A
non-looping source **auto-releases** back to the pool when the clip finishes.

```csharp
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;

EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO
{
    AudioClipSO      = hitClip,          // an AudioClipSO asset
    Position         = transform.position,
    IntervalOverload = -1f,              // -1 = use the SO's MinimalInterval
});
```

Or the convenience wrapper (raises the same event):

```csharp
using MyToolz.Extensions;   // AudioSourceExtensions
AudioSourceExtensions.Play(transform.position, hitClip);
```

## Playing music

```csharp
using MyToolz.Audio.Events;
using MyToolz.DesignPatterns.EventBus;

EventBus<PlaySong>.Raise(PlaySong.Default(bossTheme));            // blend in at default durations
EventBus<SetIntensity>.Raise(SetIntensity.Immediate(0.7f));      // 0..1 across the song's layers
EventBus<SetIntensity>.Raise(new SetIntensity { Intensity = 0f, BlendDuration = 3f });
EventBus<StopSong>.Raise(new StopSong { FadeOutDuration = 2f });
```

`MXManager` blends the outgoing song out and the new one in, drives the layered intensity
(fading between `intensityClips`), and loops. `PlaySong` fields (`Intensity`, `StartTime`,
`BlendInDuration`, `BlendOutDuration`) each treat a negative value as "use the manager default";
`PlaySong.Default(song)` / `SetIntensity.Immediate(x)` are the shorthand constructors.

## When you own an `AudioSource` directly

For a source that lives on a specific object (a looping engine hum, a per-object emitter),
`MyToolz.Extensions.AudioSourceExtensions` extends `AudioSource` so you still drive it from the
same `AudioClipSO`/`AudioSourceConfigSO` assets: `Configure(config)`, `Play(clipSO)`,
`PlayWithCooldown(clipSO)`, `PlayLoop`/`StopLoop`, and DOTween helpers `FadeIn`/`FadeOut`/
`CrossFade`. Use these only when a component genuinely owns its source — one-shots still belong
on the pooled event path.

## Multiple `AudioListener`s

`PriorityAudioListener` (on a GameObject with an `AudioListener`) keeps only the
highest-`priority` active listener enabled across the scene — use it when several cameras each
carry a listener (split views, camera swaps) to avoid Unity's "multiple listeners" warning.

## Do / Don't

- ✅ Raise `PlayAudioClipSO` for one-shots — the manager pools and auto-releases the source.
- ✅ Throttle spammy sounds with `AudioClipSO.minimalInterval` (or `IntervalOverload`).
- ✅ Model adaptive music as `SongSO` intensity layers driven by `SetIntensity`.
- ✅ Put an `AudioSourceObjectPool` in every scene that plays audio.
- ❌ Don't `Instantiate` audio objects or call `PlayOneShot` for pooled SFX.
- ❌ Don't reach for `AudioManager.Instance` / `MXManager.Instance` — they're `PrivateSingleton`s;
  drive them through events.
- ❌ Don't hand-author `AudioSource` settings in the scene — put them in an `AudioSourceConfigSO`.
