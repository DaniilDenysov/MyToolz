# Audio

ScriptableObject-based audio system with configurable AudioSource settings, randomized clip selection, cooldown-based playback, object-pooled one-shot sources, and DOTween-powered fade/crossfade extension methods.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |
| Editor Toolz | `com.mytoolz.editortoolz` |

External: DOTween (`DG.Tweening`), UniTask (`Cysharp.Threading.Tasks`).

Runtime integration with Event Bus and Object Pool for pooled one-shot playback.

## Structure

```
Runtime/
├── AudioClipSO.cs              ScriptableObject defining clips with randomization, per-clip configs, and cooldown
├── AudioSourceConfigSO.cs      ScriptableObject for AudioSource settings (mixer, volume, pitch, spatial blend, etc.)
├── AudioSourceExtensions.cs    Extension methods: Configure, Play, PlayWithCooldown, PlayLoop, StopLoop, FadeOut, FadeIn, CrossFade
├── AudioSourceWrapper.cs       Poolable MonoBehaviour wrapping AudioSource with auto-release on clip completion
├── AudioSourceObjectPool.cs    Object pool installer listening for PlayAudioClipSO events via EventBus
└── Events.cs                   PlayAudioClipSO event definition
```

## Usage

Create `AudioClipSO` and `AudioSourceConfigSO` assets to define audio content and playback settings. Use extension methods for direct AudioSource control, or raise `PlayAudioClipSO` events for fire-and-forget pooled playback:

```csharp
audioSource.Play(audioClipSO);
audioSource.CrossFade(targetSource, audioClipSO, 2f);

EventBus<PlayAudioClipSO>.Raise(new PlayAudioClipSO
{
    AudioClipSO = clipSO,
    Position = transform.position
});
```
