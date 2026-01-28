using UnityEngine;

namespace MyToolz.ScriptableObjects.Utilites.Debug
{
    [CreateAssetMenu(fileName = "DebugUtilityPreferences", menuName = "MyToolz/Debug/DebugUtilityPreferences")]
    public class DebugUtilityPreferencesSO : ScriptableObject
    {
        [SerializeField] private DebugUtilityMessageSO errorMessage;
        [SerializeField] private DebugUtilityMessageSO warningMessage;
        [SerializeField] private DebugUtilityMessageSO logMessage;

        public DebugUtilityMessageSO ErrorMessage => errorMessage;
        public DebugUtilityMessageSO WarningMessage => warningMessage;
        public DebugUtilityMessageSO LogMessage => logMessage;

        //[Button]
        //public void TestEror()
        //{
        //    DebugUtility.LogError(this, "Dummy");
        //}

        //[Button]
        //public void TestWarning()
        //{
        //    DebugUtility.LogWarning(this, "Dummy");
        //}

        //[Button]
        //public void TestLog()
        //{
        //    DebugUtility.Log(this, "Dummy");
        //}
    }
}
