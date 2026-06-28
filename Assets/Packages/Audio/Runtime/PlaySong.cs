using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.Audio.Events
{
    public struct PlaySong : IEvent
    {
        public int SongIndex;
        public float Intensity;
        public float StartTime;
        public float BlendInDuration;
        public float BlendOutDuration;

        public static PlaySong Default(int songIndex)
        {
            return new PlaySong
            {
                SongIndex = songIndex,
                Intensity = -1f,
                StartTime = 0f,
                BlendInDuration = -1f,
                BlendOutDuration = -1f
            };
        }
    }
}
