using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MyToolz.UI.Management;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MyToolz.Tests.PlayMode
{
    /// <summary>
    /// A Cover screen (loading screen) takes the focus off the screens it is drawn over, but keeps a
    /// selection it made itself. Needs play mode: EventSystem.current is only set once an EventSystem is enabled.
    /// </summary>
    public class UIScreenCoverFocusTests
    {
        private readonly List<Object> created = new List<Object>();
        private EventSystem eventSystem;
        private GameObject coveredButton;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            eventSystem = Track(new GameObject("EventSystem")).AddComponent<EventSystem>();
            coveredButton = Track(new GameObject("Covered button"));
            yield return null;

            Assert.AreSame(eventSystem, EventSystem.current);
            eventSystem.SetSelectedGameObject(coveredButton);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }
            created.Clear();
        }

        [Test]
        public void Cover_KeepsTheFocusOnItsFirstSelected()
        {
            UIScreen cover = Screen(ActivationMode.Cover, out GameObject button, withFirstSelected: true);

            cover.OnEnter();

            Assert.AreSame(button, eventSystem.currentSelectedGameObject, "a loading screen with a button keeps that button focused");
        }

        [Test]
        public void Cover_WithoutFirstSelected_TakesTheFocusOffTheCoveredScreen()
        {
            UIScreen cover = Screen(ActivationMode.Cover, out _, withFirstSelected: false);

            cover.OnEnter();

            Assert.IsNull(eventSystem.currentSelectedGameObject, "a key press must not click a screen behind the cover");
        }

        [Test]
        public void OverrideScreen_WithoutFirstSelected_LeavesTheFocusAlone()
        {
            UIScreen screen = Screen(ActivationMode.Override, out _, withFirstSelected: false);

            screen.OnEnter();

            Assert.AreSame(coveredButton, eventSystem.currentSelectedGameObject, "only a cover clears foreign focus");
        }

        private UIScreen Screen(ActivationMode mode, out GameObject button, bool withFirstSelected)
        {
            UILayerSO layer = Track(ScriptableObject.CreateInstance<UILayerSO>());
            typeof(UILayerSO).GetField("activationMode", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(layer, mode);

            UIScreen screen = Track(new GameObject($"{mode} screen")).AddComponent<UIScreen>();
            typeof(UIScreen).GetField("layer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(screen, layer);

            button = new GameObject("Screen button");
            button.transform.SetParent(screen.transform);
            if (withFirstSelected)
            {
                typeof(UIScreenBase).GetField("firstSelected", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(screen, button);
            }

            return screen;
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
