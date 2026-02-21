using MyToolz.EditorToolz;
using MyToolz.Input;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace MyToolz.UI.Management
{
    public class UIScreen : UIScreenBase, IUILayer
    {
        [SerializeField] private bool enterOnStart;
        [Header("Config")]
        [SerializeField] private UIScreenBase defaultScreen;
        [SerializeField] private UILayerSO layer;
        private bool isRoot => parent == null && layer != null;

        protected UIStateManager localUIStateManager = new UIStateManager();

        protected UILayerStateManager layerStateManager;


        public UILayerSO Layer => layer;
        [Header("Input Config")]
        [SerializeReference, SubclassSelector] private InputMode input;
        protected InputStateManager inputStateManager;


        private void Start()
        {
            if (enterOnStart) Open();
            if (defaultScreen != null) defaultScreen.Open();
        }

        [Inject]
        private void Construct(UILayerStateManager layerStateManager, InputStateManager inputStateManager)
        {
            this.inputStateManager = inputStateManager;
            this.layerStateManager = layerStateManager;

            if (isRoot && layerStateManager != null)
            {
                layerStateManager.AddLayer(this);
            }
        }


        public override void Open()
        {
            if (isRoot && layerStateManager != null)
            {
                layerStateManager.ChangeState(this);
            }
            else
            {
                parent.ChangeState(this);
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (isRoot || input != null)
            {
                inputStateManager.ChangeState(input);
            }
            if (defaultScreen != null)
            {
                defaultScreen.Open();
            }
        }

        public override void Close()
        {
            localUIStateManager.ClearStack();
            if (isRoot && layerStateManager != null)
                layerStateManager.ExitState();
            else
            {
                parent.ExitState(this);
            }
        }

        public void ChangeState(UIScreenBase screen)
        {
            localUIStateManager.ChangeState(screen);
            if (isRoot || input != null)
            {
                inputStateManager.ChangeState(input);
            }
        }

        public void ExitState(UIScreenBase screen)
        {
            localUIStateManager.ExitState();
        }

        private void OnDestroy()
        {
            layerStateManager.RemoveLayer(this);
        }
    }
}
