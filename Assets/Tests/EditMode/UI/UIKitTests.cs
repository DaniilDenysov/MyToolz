using System;
using System.Reflection;
using MyToolz.UI.Kit;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode.UI
{
    public class CardCarouselTests
    {
        private sealed class TestCarousel : CardCarousel<CarouselCard>
        {
            public Predicate<int> Hidden = _ => false;
            public int Chosen = -1;

            protected override void Choose(int index) => Chosen = index;

            protected override bool IsShown(CarouselCard card) => !Hidden(Array.IndexOf(Cards, card));

            // Edit mode does not run Awake/OnEnable for components added from code.
            public void Boot()
            {
                Awake();
                OnEnable();
            }
        }

        private GameObject root;
        private TestCarousel carousel;
        private RectTransform[] slots;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Carousel", typeof(RectTransform));
            carousel = root.AddComponent<TestCarousel>();
            slots = new RectTransform[3];
            var cards = new CarouselCard[slots.Length];

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new GameObject("Slot " + i, typeof(RectTransform));
                slot.transform.SetParent(root.transform, false);
                slots[i] = (RectTransform)slot.transform;
                cards[i] = new CarouselCard { Slot = slots[i] };
            }

            SetField("modes", cards);
            SetField("snapDuration", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void HiddenCards_AreSwitchedOffAndSkipped()
        {
            carousel.Hidden = i => i == 1;
            carousel.Boot();

            Assert.That(carousel.ShownCount, Is.EqualTo(2));
            Assert.That(carousel.IsCardShown(1), Is.False);
            Assert.That(slots[1].gameObject.activeSelf, Is.False, "a hidden card's slot is switched off");
            Assert.That(slots[0].gameObject.activeSelf && slots[2].gameObject.activeSelf, Is.True);

            carousel.Select(1);
            Assert.That(carousel.Current, Is.Not.EqualTo(1), "a hidden card cannot be selected");

            carousel.SnapTo(1);
            Assert.That(carousel.Current, Is.Not.EqualTo(1), "snapping to a hidden card lands on the nearest shown one");
        }

        [Test]
        public void Refresh_ReadsVisibilityAgain_AndTheOnlyCardCanBeChosen()
        {
            carousel.Boot();
            Assert.That(carousel.ShownCount, Is.EqualTo(3));
            Assert.That(carousel.TryChooseOnlyCard(), Is.False, "three cards are shown");

            carousel.Hidden = i => i != 2;
            carousel.Refresh();

            Assert.That(carousel.ShownCount, Is.EqualTo(1));
            Assert.That(carousel.Current, Is.EqualTo(2), "the carousel moves to the only shown card");
            Assert.That(carousel.TryChooseOnlyCard(), Is.True);
            Assert.That(carousel.Chosen, Is.EqualTo(2));
        }

        private void SetField(string name, object value) =>
            typeof(CardCarousel<CarouselCard>).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(carousel, value);
    }

    public class ArcLayoutTests
    {
        [Test]
        public void Setters_ClampLikeTheInspector()
        {
            var go = new GameObject("Arc", typeof(RectTransform));
            try
            {
                var arc = go.AddComponent<ArcLayout>();

                arc.SpanDegrees = 250f;
                Assert.That(arc.SpanDegrees, Is.EqualTo(180f));
                arc.SpanDegrees = -5f;
                Assert.That(arc.SpanDegrees, Is.EqualTo(0f));

                arc.SideScale = 0f;
                Assert.That(arc.SideScale, Is.EqualTo(0.01f));
                arc.SideScale = 0.7f;
                Assert.That(arc.SideScale, Is.EqualTo(0.7f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
