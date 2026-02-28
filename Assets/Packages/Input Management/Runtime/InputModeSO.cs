using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyToolz.InputManagement
{
    [CreateAssetMenu(fileName = "InputModeSO", menuName = "MyToolz/InputManagement/InputModeSO")]
    public class InputModeSO : ScriptableObject, IPlayerInputState
    {
        [SerializeField] private bool cursorVisible = true;
        [SerializeField] private CursorLockMode cursorLockMode = CursorLockMode.None;
        [SerializeField] private List<InputActionReference> enabledActions = new();

        private InputAction[] resolvedActions;

        public IReadOnlyList<InputActionReference> EnabledActions => enabledActions;

        public void Initialize(InputActionAsset asset)
        {
            if (asset == null) return;

            var resolved = new List<InputAction>(enabledActions.Count);
            foreach (var actionRef in enabledActions)
            {
                if (actionRef == null || actionRef.action == null) continue;
                var action = asset.FindAction(actionRef.action.id);
                if (action != null)
                    resolved.Add(action);
            }
            resolvedActions = resolved.ToArray();
        }

        public void OnEnter()
        {
            Cursor.lockState = cursorLockMode;
            Cursor.visible = cursorVisible;

            if (resolvedActions == null) return;

            var actions = resolvedActions;
            for (int i = 0; i < actions.Length; i++)
                actions[i].Enable();
        }

        public void OnExit()
        {
            if (resolvedActions == null) return;

            var actions = resolvedActions;
            for (int i = 0; i < actions.Length; i++)
                actions[i].Disable();
        }
    }
}
