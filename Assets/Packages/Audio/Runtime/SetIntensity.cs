using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.Audio.Events
{
    public struct SetIntensity : IEvent
    {
        public float Intensity;
        public float BlendDuration;

        public static SetIntensity Immediate(float intensity)
        {
            return new SetIntensity
            {
                Intensity = intensity,
                BlendDuration = 0f
            };
        }
    }
}
