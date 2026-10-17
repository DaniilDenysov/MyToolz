using UnityEngine;

namespace MyToolz.UI.Management
{
    public enum ActivationMode
    {
        Override, //overrides stack completely
        Additive, //added on top, exits previous state
        Blend, //added on top, doesn't exit previous state
        Cover //clears the stack like Override but stays outside it, so layers opened under it never exit it; only its own Close does. The screens it opens over stay drawn behind it until their scene unloads (loading screens)
    }

    [CreateAssetMenu(fileName = "UILayer", menuName = "MyToolz/UI/Layer")]
    public class UILayerSO : ScriptableObject
    {
        [SerializeField] private ActivationMode activationMode = ActivationMode.Override;
        public ActivationMode ActivationMode { get { return activationMode; } }
    }
}
