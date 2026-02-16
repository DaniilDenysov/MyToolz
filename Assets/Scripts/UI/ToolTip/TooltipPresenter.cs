using MyToolz.DesignPatterns.EventBus;
using MyToolz.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;


namespace MyToolz.UI.ToolTip
{
    public interface ITooltipPresenter
    {
        public void Show(string description);
        public void Hide();
    }

    public class TooltipPresenter : MonoInstaller, ITooltipPresenter
    {
        [SerializeField, Required] private Transform root;
        [SerializeField, Required] private Tooltip tooltip;
        private Tooltip cachedTooltip;
        private string description;

        public override void InstallBindings()
        {
            Container.Bind<ITooltipPresenter>().FromInstance(this).AsSingle();
        }

        public void Show(string description)
        {
            if (string.IsNullOrEmpty(description) || string.IsNullOrWhiteSpace(description)) return;
            if (description.Equals(this.description)) return;

            EventBus<PoolRequest<Tooltip>>.Raise(new PoolRequest<Tooltip>()
            {
                Prefab = tooltip,
                Rotation = Quaternion.identity,
                Parent = root,
                Callback = (t) =>
                {
                    cachedTooltip = t;
                    cachedTooltip.Initialize(description);
                    this.description = description;
                }
            });
        }

        public void Hide()
        {
            if (cachedTooltip != null)
            {
                EventBus<ReleaseRequest<Tooltip>>.Raise(new ReleaseRequest<Tooltip>()
                {
                    PoolObject = cachedTooltip,
                    Callback = (t) =>
                    {
                        cachedTooltip = null;
                        description = null;
                    }
                });
            }
        }
    }
}
