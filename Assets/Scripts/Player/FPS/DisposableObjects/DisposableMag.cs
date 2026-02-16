using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;

namespace MyToolz.Player.FPS.DisposableObjects
{
    public class DisposableMag : DisposableObject
    {
        private void OnEnable()
        {
            ResetObject();
            base.Start();
        }

        public override void OnObjectDispose()
        {
            EventBus<ReleaseRequest<DisposableMag>>.Raise(new ReleaseRequest<DisposableMag>()
            {
                PoolObject = this
            });
        }
    }
}