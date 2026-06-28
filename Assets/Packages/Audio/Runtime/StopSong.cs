using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.Audio.Events
{
    public struct StopSong : IEvent
    {
        public float FadeOutDuration;
    }
}
