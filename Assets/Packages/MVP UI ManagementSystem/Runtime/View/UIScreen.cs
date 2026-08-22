using MyToolz.EditorToolz;
using MyToolz.InputManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace MyToolz.UI.Management
{
    public class UIScreen : UIScreenBase, IUILayer, ISelfValidator
    {
        [Tooltip("Open this screen automatically on Start. A child is opened by its parent, so this is " +
                 "hidden on children (shown only if a stale value is set, so it can be cleared).")]
        [SerializeField, HideIf("@parent != null && !enterOnStart")] private bool enterOnStart;
        [Header("Config")]
        [SerializeField] private UIScreenBase defaultScreen;

        [Tooltip("The layer this screen occupies. Only the root of a hierarchy (no parent) defines it; " +
                 "a child inherits its root's layer, so this field is hidden on children.")]
        [SerializeField, HideIf("@parent != null")] private UILayerSO layer;

        private bool isRoot => parent == null;

        protected UIStateManager localUIStateManager = new UIStateManager();

        protected UILayerStateManager layerStateManager;

        public UILayerSO Layer
        {
            get
            {
                UIScreen root = this;
                int guard = 0;
                while (root.parent != null && guard++ < 64)
                    root = root.parent;
                return root.layer;
            }
        }
        [Header("InputManagement Config")]
        [SerializeField] private InputModeSO input;
        protected InputStateManager inputStateManager;

        private void Start()
        {
            if (enterOnStart) Open();
            if (defaultScreen != null) defaultScreen.Open();
        }

        [Inject]
        private void Construct(
            UILayerStateManager layerStateManager,
            InputStateManager inputStateManager)
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
            if (input != null)
            {
                inputStateManager.ChangeState(input);
            }

            if (defaultScreen != null && !defaultScreen.IsActive)
            {
                defaultScreen.Open();
            }
        }

        public override void Close()
        {
            localUIStateManager.ClearStack();
            if (isRoot && layerStateManager != null)
            {
                layerStateManager.ExitState();
            }
            else
            {
                parent.ExitState(this);
            }
        }

        public void ChangeState(UIScreenBase screen)
        {
            localUIStateManager.ChangeState(screen);
            if (input != null)
            {
                inputStateManager.ChangeState(input);
            }
        }

        public void ExitState(UIScreenBase screen)
        {
            localUIStateManager.ExitState();
        }

        public void Validate(SelfValidationResult result)
        {
            if (parent == null && layer == null)
            {
                result.AddError("Root screen (no Parent) must have a Layer assigned, otherwise it cannot register with the layer system.");
            }

            if (parent == this)
            {
                result.AddError("Parent references this screen (self-reference).");
            }

            if (defaultScreen == this)
            {
                result.AddError("Default Screen references this screen - opening it would loop forever.");
            }

            if (TryFindParentCycle(out string parentCycle))
            {
                result.AddError($"Parent chain forms a cycle: {parentCycle}");
            }

            if (TryFindDefaultScreenCycle(out string defaultCycle))
            {
                result.AddError($"Default Screen chain forms a cycle (opening this screen would recurse forever): {defaultCycle}");
            }
        }

        private bool TryFindParentCycle(out string path)
        {
            path = null;
            HashSet<UIScreen> visited = new HashSet<UIScreen>();
            List<string> chain = new List<string>();

            UIScreen current = this;
            while (current != null)
            {
                if (!visited.Add(current))
                {
                    chain.Add(current.name);
                    path = string.Join(" -> ", chain);
                    return true;
                }

                chain.Add(current.name);
                current = current.parent;
            }

            return false;
        }

        private bool TryFindDefaultScreenCycle(out string path)
        {
            path = null;
            HashSet<UIScreenBase> visited = new HashSet<UIScreenBase>();
            List<string> chain = new List<string>();

            UIScreenBase current = this;
            while (current != null)
            {
                if (!visited.Add(current))
                {
                    chain.Add(current.name);
                    path = string.Join(" -> ", chain);
                    return true;
                }

                chain.Add(current.name);
                current = current is UIScreen screen ? screen.defaultScreen : null;
            }

            return false;
        }

        private void OnDestroy()
        {
            if (isRoot && layerStateManager != null)
                layerStateManager.RemoveLayer(this);
        }
    }
}
