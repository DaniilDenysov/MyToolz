using System.Collections.Generic;
using UnityEngine;
using MyToolz.EditorToolz;
using ModestTree;
using MyToolz.Utilities.Debug;

namespace MyToolz.ScriptableObjects.GameSettings
{

    [CreateAssetMenu(fileName = "QualitySettingSO", menuName = "MyToolz/GameSettings/QualitySettingSO")]
    public class QualitySettingSO : MultipleOptionSettingSO<string, List<string>>
    {
        public IReadOnlyList<string> OptionNames => QualitySettings.names;

        public int CurrentIndex => QualitySettings.GetQualityLevel();

#if UNITY_EDITOR
        [Button("Refresh Quality Levels")]
        public void RefreshQualityLevels()
        {
            if (options == null)
                options = new List<string>();

            options.Clear();

            for (int i = 0; i < QualitySettings.names.Length; i++)
                options.Add(QualitySettings.names[i]);

            defaultValue = QualitySettings.names[QualitySettings.GetQualityLevel()];

           
        }
#endif
        public override void SetCurrentValue(string newValue)
        {
            if (!IsValueValid(newValue))
            {
                DebugUtility.LogError(this, $"Invalid value {newValue} on {settingName}, it will not be accepted!");
                return;
            }
            base.SetCurrentValue(newValue);
            QualitySettings.SetQualityLevel(QualitySettings.names.IndexOf(newValue));
        }
    }
}