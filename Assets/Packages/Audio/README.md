# Audio

ScriptableObject-based audio system covering both SFX and music: configurable AudioSource settings, randomized clip selection, cooldown-based playback, object-pooled one-shot sources, DOTween-powered fade/crossfade extension methods, an intensity-layered music manager, and priority-based AudioListener arbitration.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |
| Event Bus | `com.mytoolz.eventbus` |
| Object Pool | `com.mytoolz.objectpool` |
| Extensions | `com.mytoolz.extensions` |
| Singleton | `com.mytoolz.singleton` |

External: DOTween (`DG.Tweening`), UniTask (`Cysharp.Threading.Tasks`).

## Structure

```
Runtime/
├── AudioClipSO.cs              ScriptableObject defining clips with randomization, per-clip configs, and cooldown
├── AudioSourceConfigSO.cs      ScriptableObject for AudioSource settings (mixer, volume, pitch, spatial blend, etc.)
├── AudioSourceExtensions.cs    Extension methods: Configure, Play, PlayWithCooldown, PlayLoop, StopLoop, FadeOut, FadeIn, CrossFade
├── AudioSourceWrapper.cs       Poolable MonoBehaviour wrapping AudioSource with auto-release on clip completion
├── AudioSourceObjectPool.cs    Object pool installer for pooled AudioSourceWrapper instances
├── AudioManager.cs             Singleton for fire-and-forget SFX: listens for PlayAudioClipSO, enforces per-clip play intervals, spawns pooled sources
├── Events.cs                   PlayAudioClipSO event definition
├── MXManager.cs                Singleton music manager: intensity-layered songs with looping, song blending, and intensity fades
├── SongSO.cs                   ScriptableObject describing a song: intensity-layer clips, reverb tail, source config (with editor timeline preview)
├── PlaySong.cs                 Event: play a song by index with optional intensity, start time, and blend durations
├── StopSong.cs                 Event: fade out the current song
├── SetIntensity.cs             Event: blend the music intensity (0–1) over a duration
└── PriorityAudioListener.cs    Keeps exactly one AudioListener enabled — the highest-priority registered one
```

## SFX

Create `AudioClipSO` and `AudioSourceConfigSO` assets to define audio content and playback settings. Use extension methods for direct AudioSource control, or raise `PlayAudioClipSO` events for fire-and-forget pooled playback (handled by `AudioManager`):

```csharp
audioSource.Play(audioClipSO);
audioSource.CrossFade(targetSource, audioClipSO, 2f);

EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO
{
    AudioClipSO = clipSO,
    Position = transform.position
});
```

`AudioManager` throttles repeated playback of the same `AudioClipSO` using the clip's `MinimalInterval` (or the event's `IntervalOverload`).

## Music (MXManager)

Create `SongSO` assets, each holding one or more intensity-layer clips of equal length. Add an `MXManager` to the scene, assign its song list and an `AudioSourceWrapper` prefab, and make sure an object pool installer for `AudioSourceWrapper` is active. All layers of a song play in sync; the current intensity (0–1) crossfades between adjacent layers.

```csharp
EventBus<PlaySong>.Raise(PlaySong.Default(0));  // play song 0 with default blend/intensity
EventBus<SetIntensity>.Raise(new SetIntensity { Intensity = 0.8f, BlendDuration = 2f });
EventBus<StopSong>.Raise(new StopSong { FadeOutDuration = 1.5f });
```

Songs loop by default (`loopCurrentSong`): the next iteration starts just before the reverb tail of the current one so the loop overlaps seamlessly. Disabling the manager stops playback and releases all pooled sources.

## Priority AudioListener

Add `PriorityAudioListener` next to every `AudioListener` in the scene (player camera, free camera, kill cam, ...) and set a priority. Only the listener with the highest priority stays enabled; the rest are disabled automatically as listeners register and deregister.
