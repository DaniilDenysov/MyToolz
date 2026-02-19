using UnityEngine;
using MyToolz.ScriptableObjects.GameSettings;
using MyToolz.EditorToolz;

namespace MyToolz.ScriptableObjects.GameSettings
{
    [CreateAssetMenu(fileName = "FullscreenSettingSO", menuName = "MyToolz/GameSettings/FullscreenSettingSO")]
    public class FullscreenSettingSO : BoolSettingSO
    {
        [Header("Fullscreen mapping")]
        [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.FullScreenWindow;
        [SerializeField] private FullScreenMode windowedMode = FullScreenMode.Windowed;

        [Button("Refresh Fullscreen From System")]
        public void RefreshFromSystem()
        {
            currentValue = Screen.fullScreen;
            NotifyValueUpdated();
        }

        public void SetValue(bool value, bool applyImmediately = true)
        {
            currentValue = value;
            NotifyValueUpdated();

            if (applyImmediately)
                ApplyCurrent();
        }

        public void ApplyCurrent()
        {
            var mode = currentValue ? fullscreenMode : windowedMode;
            Screen.fullScreenMode = mode;
        }

        protected override bool IsCurrentValueValid() => true;
    }
}
