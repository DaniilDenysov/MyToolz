using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.SceneManagement
{
    /// <summary>Raised after every scene of <see cref="Group"/> loaded and activated successfully.</summary>
    public struct SceneGroupLoaded : IEvent
    {
        public SceneGroupSO Group;
    }

    /// <summary>
    /// Raised instead of <see cref="SceneGroupLoaded"/> when a group load failed or was cancelled.
    /// Scenes that did load stay loaded; <see cref="Error"/> describes what went wrong.
    /// </summary>
    public struct SceneGroupLoadFailed : IEvent
    {
        public SceneGroupSO Group;
        public bool Cancelled;
        public string Error;
    }
}
