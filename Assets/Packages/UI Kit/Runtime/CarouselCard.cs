using System;
using MyToolz.UI.Layout;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.UI.Kit
{
    [Serializable]
    public class CarouselCard
    {
        [Tooltip("Wrapper the carousel moves, scales and fades. The card button sits inside it.")]
        public RectTransform Slot;

        [Tooltip("Card button. Tapping it chooses the card when centred, or brings it to the centre otherwise.")]
        public UIStrongButton Card;

        public Image Dot;

        [Tooltip("Colour of this card's page dot while it is centred.")]
        public Color Accent = Color.white;
    }
}
