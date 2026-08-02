using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.Audio.Events
{
    public struct PlaySong : IEvent
    {
        public SongSO Song;
        public float Intensity;
        public float StartTime;
        public float BlendInDuration;
        public float BlendOutDuration;

        public static PlaySong Default(SongSO song)
        {
            return new PlaySong
            {
                Song = song,
                Intensity = -1f,
                StartTime = 0f,
                BlendInDuration = -1f,
                BlendOutDuration = -1f
            };
        }
    }
}
