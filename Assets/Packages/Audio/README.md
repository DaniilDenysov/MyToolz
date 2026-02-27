# AudioSource Extensions

Extension methods for Unity's `AudioSource` that integrate with the `AudioClipSO` and `AudioSourceConfigSO` ScriptableObject-based audio system. Fade operations use DOTween.

## Dependencies

- [DOTween](http://dotween.demigiant.com/)
- `MyToolz.Audio` (AudioClipSO, AudioSourceConfigSO)

## API Reference

### Configure

Applies all settings from an `AudioSourceConfigSO` to an `AudioSource`, including mixer group, volume, pitch, spatial blend, loop, and bypass settings. Volume and pitch are randomized automatically when enabled in the config.

```csharp
audioSource.Configure(configSO);
```

### Play

Retrieves a clip and config from an `AudioClipSO`, configures the source, and plays immediately or after a delay.

```csharp
audioSource.Play(audioClipSO);
audioSource.Play(audioClipSO, delay: 0.5f);
```

### PlayWithCooldown

Same as `Play` but respects the `MinimalInterval` cooldown defined on the `AudioClipSO`. Returns `true` if playback started, `false` if the clip is still on cooldown or null.

```csharp
bool played = audioSource.PlayWithCooldown(audioClipSO);
bool played = audioSource.PlayWithCooldown(audioClipSO, delay: 0.2f);
```

### PlayLoop

Configures and starts looping playback of a clip.

```csharp
audioSource.PlayLoop(audioClipSO);
```

### StopLoop

Disables looping and stops the source.

```csharp
audioSource.StopLoop();
```

### FadeOut

Fades volume to zero over `duration` seconds, then stops the source and restores its original volume. Returns a `Tween` that can be chained, awaited, or killed.

```csharp
audioSource.FadeOut(1f);
audioSource.FadeOut(2f, Ease.Linear);
```

### FadeIn

Configures the source from an `AudioClipSO`, sets volume to zero, starts playback, and fades to the target volume. Returns a `Tween`.

```csharp
audioSource.FadeIn(audioClipSO, 1f);
audioSource.FadeIn(audioClipSO, 2f, Ease.OutCubic);
```

### CrossFade

Fades out the current source while simultaneously fading in a target source with a new clip. Both fades run in parallel via a DOTween `Sequence`. The outgoing source is stopped and its volume restored on completion.

```csharp
audioSource.CrossFade(targetSource, audioClipSO, 1.5f);
audioSource.CrossFade(targetSource, audioClipSO, 2f, Ease.InOutSine);
```

Requires two `AudioSource` components — one for the outgoing track and one for the incoming track.

## Usage Example

```csharp
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource sourceA;
    [SerializeField] private AudioSource sourceB;
    [SerializeField] private AudioClipSO menuMusic;
    [SerializeField] private AudioClipSO gameMusic;

    private AudioSource current;

    private void Start()
    {
        current = sourceA;
        current.PlayLoop(menuMusic);
    }

    public void TransitionToGameMusic()
    {
        AudioSource next = current == sourceA ? sourceB : sourceA;
        current.CrossFade(next, gameMusic, 2f);
        current = next;
    }
}
```
