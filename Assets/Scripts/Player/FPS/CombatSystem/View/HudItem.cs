using MyToolz.Tweener.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Player.FPS.CombatSystem.View
{
    public class HudItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private TMP_Text binding;
        [SerializeField] private UITweener tweener;
        public TMP_Text Text
        {
            get => text;
        }

        public TMP_Text Binding
        {
            get => binding;
        }

        [SerializeField] private Image itemIcon;
        public Image ItemIcon
        {
            get => itemIcon;
        }

        public void Select()
        {
            tweener.OnPointerEnter(null);
        }

        public void Deselect()
        {
            tweener.OnPointerExit(null);
        }
    }
}
