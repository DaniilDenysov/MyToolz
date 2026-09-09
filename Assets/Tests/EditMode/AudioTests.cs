using System.Collections.Generic;
using System.Reflection;
using MyToolz.Audio;
using MyToolz.Extensions;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    public class AudioTests : SilentLogTest
    {
        private readonly List<Object> _toDestroy = new();

        [TearDown]
        public void CleanupObjects()
        {
            foreach (var obj in _toDestroy)
                if (obj != null) Object.DestroyImmediate(obj);
            _toDestroy.Clear();
        }

        private static void SetField(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private AudioSourceConfigSO Config()
        {
            var config = ScriptableObject.CreateInstance<AudioSourceConfigSO>();
            _toDestroy.Add(config);
            return config;
        }

        // ----- AudioSourceConfigSO -----

        [Test]
        public void SpatialBlend_IsForcedToZero_WhenGlobal()
        {
            var config = Config();
            SetField(config, "isGlobal", true);
            SetField(config, "spatialBlend", 0.9f);

            Assert.AreEqual(0f, config.SpatialBlend, "a global source is always 2D regardless of the serialized blend");
        }

        [Test]
        public void SpatialBlend_UsesSerializedValue_WhenNotGlobal()
        {
            var config = Config();
            SetField(config, "isGlobal", false);
            SetField(config, "spatialBlend", 0.7f);

            Assert.AreEqual(0.7f, config.SpatialBlend, 1e-4);
        }

        [Test]
        public void GetRandomVolume_StaysWithinRange()
        {
            var config = Config();
            SetField(config, "volumeRange", new Vector2(0.2f, 0.4f));

            for (int i = 0; i < 50; i++)
            {
                float v = config.GetRandomVolume();
                Assert.GreaterOrEqual(v, 0.2f);
                Assert.LessOrEqual(v, 0.4f);
            }
        }

        [Test]
        public void GetRandomPitch_StaysWithinRange_AndMinMaxMatch()
        {
            var config = Config();
            SetField(config, "pitchRange", new Vector2(0.8f, 1.2f));

            Assert.AreEqual(0.8f, config.MinPitch, 1e-4);
            Assert.AreEqual(1.2f, config.MaxPitch, 1e-4);

            for (int i = 0; i < 50; i++)
            {
                float p = config.GetRandomPitch();
                Assert.GreaterOrEqual(p, 0.8f);
                Assert.LessOrEqual(p, 1.2f);
            }
        }

        // ----- AudioSourceExtensions.Configure -----

        [Test]
        public void Configure_AppliesConfigValuesToAudioSource()
        {
            var config = Config();
            SetField(config, "isGlobal", false);
            SetField(config, "spatialBlend", 0.7f);
            SetField(config, "volume", 0.5f);
            SetField(config, "pitch", 1.5f);
            SetField(config, "loop", true);
            SetField(config, "playOnAwake", true);
            SetField(config, "bypassEffects", true);

            var go = new GameObject("Speaker");
            _toDestroy.Add(go);
            var source = go.AddComponent<AudioSource>();

            source.Configure(config);

            Assert.AreEqual(0.5f, source.volume, 1e-4);
            Assert.AreEqual(1.5f, source.pitch, 1e-4);
            Assert.AreEqual(0.7f, source.spatialBlend, 1e-4);
            Assert.IsTrue(source.loop);
            Assert.IsTrue(source.playOnAwake);
            Assert.IsTrue(source.bypassEffects);
            Assert.IsTrue(source.bypassReverbZones, "bypassReverbZones mirrors BypassEffects");
        }

        [Test]
        public void Configure_WithNullArguments_DoesNotThrow()
        {
            var go = new GameObject("Speaker");
            _toDestroy.Add(go);
            var source = go.AddComponent<AudioSource>();

            Assert.DoesNotThrow(() => source.Configure(null));
            Assert.DoesNotThrow(() => AudioSourceExtensions.Configure(null, Config()));
        }

        // ----- AudioClipSO cooldown -----

        private AudioClipSO Clip()
        {
            var clip = ScriptableObject.CreateInstance<AudioClipSO>();
            _toDestroy.Add(clip);
            return clip;
        }

        [Test]
        public void IsOnCooldown_IsTrueAfterMarkPlayed_AndFalseAfterReset()
        {
            var clipSo = Clip();
            SetField(clipSo, "minimalInterval", 100f); // large window so the frame time never matters

            Assert.IsFalse(clipSo.IsOnCooldown, "a fresh clip starts off cooldown");

            clipSo.MarkPlayed();
            Assert.IsTrue(clipSo.IsOnCooldown);

            clipSo.ResetCooldown();
            Assert.IsFalse(clipSo.IsOnCooldown);
        }

        [Test]
        public void GetClipAndConfig_ReturnsClipAndGlobalConfig_WhenNotPerClip()
        {
            var clipSo = Clip();
            var config = Config();
            var rawClip = AudioClip.Create("test", 1, 1, 44100, false);
            _toDestroy.Add(rawClip);

            SetField(clipSo, "useAudioConfigPerClip", false);
            SetField(clipSo, "randomize", false);
            SetField(clipSo, "clip", rawClip);
            SetField(clipSo, "globalSoundConfig", config);

            var (clip, resolvedConfig) = clipSo.GetClipAndConfig();

            Assert.AreSame(rawClip, clip);
            Assert.AreSame(config, resolvedConfig);
        }
    }
}
