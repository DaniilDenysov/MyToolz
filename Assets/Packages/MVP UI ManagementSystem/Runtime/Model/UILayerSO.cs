using UnityEngine;

namespace MyToolz.UI.Management
{
    public enum ActivationMode
    {
        Override, //overrides stack completely
        Additive, //added on top, exits previous state
        Blend //added on top, doesn't exit previous state
    }

    [CreateAssetMenu(fileName = "UILayer", menuName = "MyToolz/UI/Layer")]
    public class UILayerSO : ScriptableObject
    {
        [SerializeField] private ActivationMode activationMode = ActivationMode.Override;
        public ActivationMode ActivationMode { get { return activationMode; } }
    }
}
