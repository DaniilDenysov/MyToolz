using System.Collections.Generic;
using MyToolz.UI.Management;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class UILayerStateManagerTests : SilentLogTest
    {
        private readonly List<UILayerSO> _created = new();
        private UILayerStateManager _manager;

        [SetUp]
        public void SetUp() => _manager = new UILayerStateManager();

        [TearDown]
        public void CleanupSOs()
        {
            foreach (var so in _created)
            {
                if (so != null)
                {
                    Object.DestroyImmediate(so);
                }
            }
            _created.Clear();
        }

        private UILayerSO Layer(ActivationMode mode, string name)
        {
            var so = UILayerFactory.Create(mode, name);
            _created.Add(so);
            return so;
        }

        [Test]
        public void CurrentLayer_IsNull_Initially()
        {
            Assert.IsNull(_manager.CurrentLayer);
        }

        [Test]
        public void ChangeState_Override_EntersLayerAndBecomesCurrent()
        {
            var so = Layer(ActivationMode.Override, "A");
            var screen = new FakeUILayer(so);

            _manager.ChangeState(screen);

            Assert.AreSame(so, _manager.CurrentLayer);
            Assert.IsTrue(screen.IsActive);
            Assert.AreEqual(1, screen.EnterCount);
        }

        [Test]
        public void ChangeState_Override_ExitsPreviousLayers()
        {
            var soA = Layer(ActivationMode.Override, "A");
            var soB = Layer(ActivationMode.Override, "B");
            var a = new FakeUILayer(soA);
            var b = new FakeUILayer(soB);

            _manager.ChangeState(a);
            _manager.ChangeState(b);

            Assert.AreSame(soB, _manager.CurrentLayer);
            Assert.IsFalse(a.IsActive, "override clears the previous layer");
            Assert.AreEqual(1, a.ExitCount);
            Assert.IsTrue(b.IsActive);
        }

        [Test]
        public void ChangeState_Additive_ExitsPreviousButKeepsItOnStack()
        {
            var soA = Layer(ActivationMode.Override, "A");
            var soB = Layer(ActivationMode.Additive, "B");
            var a = new FakeUILayer(soA);
            var b = new FakeUILayer(soB);

            _manager.ChangeState(a);
            _manager.ChangeState(b);

            Assert.AreSame(soB, _manager.CurrentLayer);
            Assert.IsFalse(a.IsActive, "additive exits the layer beneath it");
            Assert.IsTrue(b.IsActive);
        }

        [Test]
        public void ExitState_Additive_RestoresPreviousLayer()
        {
            var soA = Layer(ActivationMode.Override, "A");
            var soB = Layer(ActivationMode.Additive, "B");
            var a = new FakeUILayer(soA);
            var b = new FakeUILayer(soB);

            _manager.ChangeState(a);
            _manager.ChangeState(b);
            _manager.ExitState();

            Assert.AreSame(soA, _manager.CurrentLayer);
            Assert.IsFalse(b.IsActive, "exited layer is gone");
            Assert.IsTrue(a.IsActive, "layer beneath is re-entered");
            Assert.AreEqual(2, a.EnterCount);
        }

        [Test]
        public void ChangeState_Blend_KeepsPreviousLayerActive()
        {
            var soA = Layer(ActivationMode.Override, "A");
            var soC = Layer(ActivationMode.Blend, "C");
            var a = new FakeUILayer(soA);
            var c = new FakeUILayer(soC);

            _manager.ChangeState(a);
            _manager.ChangeState(c);

            Assert.AreSame(soC, _manager.CurrentLayer);
            Assert.IsTrue(a.IsActive, "blend shows alongside the previous layer");
            Assert.IsTrue(c.IsActive);
        }

        [Test]
        public void ChangeState_ReEnteringCurrentLayer_DoesNotEnterTwice()
        {
            var so = Layer(ActivationMode.Override, "A");
            var screen = new FakeUILayer(so);

            _manager.ChangeState(screen);
            _manager.ChangeState(screen);

            Assert.AreEqual(1, screen.EnterCount, "an already-active screen must not be re-entered");
        }

        [Test]
        public void MultipleScreens_ShareOneLayer()
        {
            var so = Layer(ActivationMode.Override, "A");
            var first = new FakeUILayer(so);
            var second = new FakeUILayer(so);

            _manager.ChangeState(first);
            _manager.ChangeState(second); // same layer SO -> both screens active

            Assert.IsTrue(first.IsActive);
            Assert.IsTrue(second.IsActive);
            Assert.AreSame(so, _manager.CurrentLayer);
        }

        [Test]
        public void RemoveLayer_ExcludesScreenFromSubsequentExit()
        {
            var soA = Layer(ActivationMode.Override, "A");
            var soB = Layer(ActivationMode.Override, "B");
            var a1 = new FakeUILayer(soA);
            var a2 = new FakeUILayer(soA);

            _manager.ChangeState(a1);
            _manager.ChangeState(a2);
            _manager.RemoveLayer(a2);

            _manager.ChangeState(new FakeUILayer(soB)); // override exits layer A's remaining screens

            Assert.AreEqual(1, a1.ExitCount);
            Assert.AreEqual(0, a2.ExitCount, "a removed screen is no longer exited by the manager");
        }

        [Test]
        public void ChangeState_WithNullLayerSO_IsIgnored()
        {
            var screen = new FakeUILayer(null);

            Assert.DoesNotThrow(() => _manager.ChangeState(screen));
            Assert.IsNull(_manager.CurrentLayer);
        }

        [Test]
        public void ExitState_OnEmptyStack_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.ExitState());
            Assert.IsNull(_manager.CurrentLayer);
        }
    }
}
