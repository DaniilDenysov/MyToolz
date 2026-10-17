using MyToolz.GameSettings.Data;
using MyToolz.IO;

namespace MyToolz.GameSettings
{
    public class SettingsSaveLoad : SaveLoadBase<SavableData>
    {
        protected override void OnEnable()
        {
        }

        protected override void OnDisable()
        {
        }

        // SettingsPresenter owns saving (debounced, dirty-tracked); the saver's own lifecycle
        // autosaves are disabled so they cannot race it or warn about a missing cache.
        protected override void OnApplicationPause(bool pause)
        {
        }

        protected override void OnApplicationFocus(bool hasFocus)
        {
        }

        protected override void OnDestroy()
        {
        }
    }
}
