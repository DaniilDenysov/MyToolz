using MyToolz.Core;
using UnityEngine;
using Zenject;


namespace MyToolz.MiniMap
{
    public class MinimapWorldObject : MonoBehaviourPlus, IMinimapObject
    {
        [SerializeField] private bool followObject = false;
        [SerializeField] private Sprite minimapIcon;
        public Sprite MinimapIcon => minimapIcon;

        public bool IsHidden => hideObject;
        private bool hideObject = true;
        private IMinimap minimap;

        [Inject]
        private void Construct(IMinimap minimap)
        {
           this.minimap = minimap;
        }

        private void Start()
        {
            minimap?.Register(this, followObject);
        }

        private void OnDestroy()
        {
            minimap?.Deregister(this);
        }

        public void Show()
        {
            hideObject = false;
        }

        public void Hide()
        {
            hideObject = true;
        }
    }
}
