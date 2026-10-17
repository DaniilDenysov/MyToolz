using MyToolz.DesignPatterns.ObjectPool;

namespace MyToolz.Audio
{
#if MYTOOLZ_ADDRESSABLES
    // Pools Addressable and directly referenced prefabs alike.
    public class AudioSourceObjectPool : AddressableObjectPoolInstaller<AudioSourceWrapper>
    {
    }
#else
    // Without com.unity.addressables only directly referenced prefabs can be pooled.
    public class AudioSourceObjectPool : DefaultObjectPoolInstaller<AudioSourceWrapper>
    {
    }
#endif
}
