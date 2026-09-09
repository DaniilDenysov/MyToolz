using System.Collections.Generic;
using System.Reflection;
using MyToolz.Events;
using MyToolz.SceneManagement;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    internal static class SceneTestFactory
    {
        public static SceneReference Reference(string path, string name)
        {
            var reference = new SceneReference();
            Set(reference, "scenePath", path);
            Set(reference, "sceneName", name);
            return reference;
        }

        public static SceneData Data(SceneType type, uint priority, string name) => new SceneData
        {
            Reference = Reference($"Assets/Scenes/{name}.unity", name),
            SceneType = type,
            Priority = priority
        };

        public static SceneGroupSO Group(params SceneData[] scenes)
        {
            var group = ScriptableObject.CreateInstance<SceneGroupSO>();
            typeof(SceneGroupSO)
                .GetField("scenes", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(group, scenes);
            return group;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    public class SceneReferenceTests
    {
        [Test]
        public void AssignedReference_ExposesPathAndName()
        {
            var reference = SceneTestFactory.Reference("Assets/Scenes/Main.unity", "Main");

            Assert.AreEqual("Assets/Scenes/Main.unity", reference.Path);
            Assert.AreEqual("Main", reference.Name);
            Assert.IsTrue(reference.IsAssigned);
        }

        [Test]
        public void EmptyReference_IsNotAssigned()
        {
            Assert.IsFalse(new SceneReference().IsAssigned);
            Assert.IsFalse(SceneTestFactory.Reference("", "").IsAssigned);
        }

        [Test]
        public void SceneData_Name_DelegatesToReference()
        {
            var data = SceneTestFactory.Data(SceneType.Gameplay, 0, "Arena");
            Assert.AreEqual("Arena", data.Name);
        }
    }

    public class SceneGroupSOTests
    {
        private readonly List<SceneGroupSO> _groups = new();

        private SceneGroupSO Group(params SceneData[] scenes)
        {
            var group = SceneTestFactory.Group(scenes);
            _groups.Add(group);
            return group;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var group in _groups)
                if (group != null) Object.DestroyImmediate(group);
            _groups.Clear();
        }

        [Test]
        public void FindSceneByType_ReturnsMatchingSceneName()
        {
            var group = Group(
                SceneTestFactory.Data(SceneType.MainMenu, 0, "Menu"),
                SceneTestFactory.Data(SceneType.Gameplay, 1, "Level1"));

            Assert.AreEqual("Level1", group.FindSceneByType(SceneType.Gameplay));
            Assert.AreEqual("Menu", group.FindSceneByType(SceneType.MainMenu));
        }

        [Test]
        public void FindSceneByType_ReturnsEmpty_WhenNoMatch()
        {
            var group = Group(SceneTestFactory.Data(SceneType.MainMenu, 0, "Menu"));

            Assert.AreEqual(string.Empty, group.FindSceneByType(SceneType.HUD));
        }

        [Test]
        public void GetBatchedByPriority_GroupsAndOrdersByPriority()
        {
            var group = Group(
                SceneTestFactory.Data(SceneType.Gameplay, 2, "B"),
                SceneTestFactory.Data(SceneType.Environment, 1, "A1"),
                SceneTestFactory.Data(SceneType.HUD, 1, "A2"),
                SceneTestFactory.Data(SceneType.UserInterface, 3, "C"));

            var batches = group.GetBatchedByPriority();

            Assert.AreEqual(3, batches.Count, "one batch per distinct priority");
            Assert.AreEqual(2, batches[0].Length, "the two priority-1 scenes share the first batch");
            Assert.AreEqual(1u, batches[0][0].Priority, "batches are ordered ascending by priority");
            Assert.AreEqual(2u, batches[1][0].Priority);
            Assert.AreEqual(3u, batches[2][0].Priority);
        }
    }

    public class MultiLoadingProgressTests
    {
        [Test]
        public void Progress_IsTheAverageOfChildProgress()
        {
            var children = new List<LoadingProgress> { new(), new() };
            var multi = new MultiLoadingProgress(children);

            float reported = -1f;
            multi.Progressed += v => reported = v;

            children[0].Report(0.5f);
            Assert.AreEqual(0.25f, reported, 1e-4, "(0.5 + 0) / 2");

            children[1].Report(1f);
            Assert.AreEqual(0.75f, reported, 1e-4, "(0.5 + 1) / 2");
        }

        [Test]
        public void ChildProgress_IsClampedTo01()
        {
            var children = new List<LoadingProgress> { new() };
            var multi = new MultiLoadingProgress(children);

            float reported = -1f;
            multi.Progressed += v => reported = v;

            children[0].Report(5f); // over-reported progress is clamped, not averaged as 5
            Assert.AreEqual(1f, reported, 1e-4);
        }

        [Test]
        public void Dispose_StopsForwardingChildProgress()
        {
            var children = new List<LoadingProgress> { new(), new() };
            var multi = new MultiLoadingProgress(children);

            float reported = -1f;
            multi.Progressed += v => reported = v;

            children[0].Report(0.5f);
            Assert.AreEqual(0.25f, reported, 1e-4);

            multi.Dispose();
            children[1].Report(1f); // after dispose the child event is unsubscribed

            Assert.AreEqual(0.25f, reported, 1e-4, "no further updates should be forwarded after Dispose");
        }
    }
}
