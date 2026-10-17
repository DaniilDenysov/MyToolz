using System.Collections.Generic;
using DG.Tweening;
using MyToolz.Tweener;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode.Tweening
{
    /// <summary>
    /// Exercises the DOTween-free control logic on the shared <see cref="Tweener{T}"/> base that
    /// every concrete tweener (including the UI Tweener) inherits: the early-out guards in
    /// CreateSequence and the ignore-time-scale toggle. The tween-building paths themselves drive
    /// the DOTween timeline and belong in play-mode/manual verification, not unit tests.
    /// </summary>
    public class TweenerBaseTests
    {
        private sealed class NoopStrategy : AbstractTweenStrategy
        {
            public override Tween GetTween() => null;
        }

        private sealed class TestTweener : Tweener<NoopStrategy>
        {
            public bool IgnoreTimeScaleValue => ignoreTimeScale;
        }

        private GameObject _go;
        private TestTweener _tweener;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Tweener");
            _tweener = _go.AddComponent<TestTweener>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void CreateSequence_Null_ReturnsNull()
        {
            Assert.IsNull(_tweener.CreateSequence(null));
        }

        [Test]
        public void CreateSequence_EmptyList_ReturnsNull()
        {
            Assert.IsNull(_tweener.CreateSequence(new List<NoopStrategy>()));
        }

        [Test]
        public void IgnoreTimeScale_DefaultsToTrue()
        {
            Assert.IsTrue(_tweener.IgnoreTimeScaleValue, "tweeners ignore Time.timeScale by default");
        }

        [Test]
        public void SetIgnoreTimeScale_UpdatesTheFlag_AndDoesNotThrowWithNoRunningTweens()
        {
            Assert.DoesNotThrow(() => _tweener.SetIgnoreTimeScale(false));
            Assert.IsFalse(_tweener.IgnoreTimeScaleValue);

            Assert.DoesNotThrow(() => _tweener.SetIgnoreTimeScale(true));
            Assert.IsTrue(_tweener.IgnoreTimeScaleValue);
        }

        [Test]
        public void IsTweening_IsFalse_WhenNothingWasStarted()
        {
            Assert.IsFalse(_tweener.IsTweening);
        }
    }
}
