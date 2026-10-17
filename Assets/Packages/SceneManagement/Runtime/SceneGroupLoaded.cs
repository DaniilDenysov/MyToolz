using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.SceneManagement
{
    /// <summary>Raised after every scene of <see cref="Group"/> loaded and activated successfully.</summary>
    public struct SceneGroupLoaded : IEvent
    {
        public SceneGroupSO Group;
    }
}
