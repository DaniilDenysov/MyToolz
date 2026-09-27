using MyToolz.DesignPatterns.EventBus;

namespace MyToolz.SceneManagement
{
    /// <summary>
    /// Raised instead of <see cref="SceneGroupLoaded"/> when one or more scenes of a group failed to
    /// load. The loading screen is still hidden; listen for this to tell the player.
    /// </summary>
    public struct SceneGroupLoadFailed : IEvent
    {
        public SceneGroupSO Group;
        public string Reason;
    }
}
