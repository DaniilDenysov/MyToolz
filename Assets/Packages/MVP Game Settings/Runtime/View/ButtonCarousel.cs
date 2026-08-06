using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.GameSettings
{
    [Serializable]
    public class CarouselState
    {
        public Sprite sprite;
        public string label;
    }

    [AddComponentMenu("MyToolz/Settings/Button Carousel")]
    public class ButtonCarousel : AbstractButtonCarousel
    {
        [Header("States")]
        [Tooltip("One entry per selectable value; the count should match the setting's range.")]
        [SerializeField] private List<CarouselState> states = new List<CarouselState>();

        protected override int Count => states != null ? states.Count : 0;

        protected override Sprite GetSprite(int index) => states[index].sprite;

        protected override string GetLabel(int index) => states[index].label;
    }
}
