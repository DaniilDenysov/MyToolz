using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.SceneManagement
{
    /// <summary>
    /// Raised instead of <see cref="SceneGroupLoaded"/> when a group load failed or was cancelled.
    /// Scenes that did load stay loaded and the loading screen is still hidden; <see cref="Error"/>
    /// describes what went wrong. Listen for it to tell the player.
    /// </summary>
    public struct SceneGroupLoadFailed : IEvent
    {
        public SceneGroupSO Group;
        public bool Cancelled;
        public string Error;
    }
}
