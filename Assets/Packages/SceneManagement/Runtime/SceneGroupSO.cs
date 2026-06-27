using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyToolz.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneGroupSO", menuName = "MyToolz/BootStrapper/SceneGroupSO")]
    public class SceneGroupSO : ScriptableObject
    {
        [SerializeField] private string groupName = "GroupName";
        [SerializeField] private SceneData[] scenes;
        public string GroupName => groupName;
        public SceneData[] Scenes => scenes;

        public string FindSceneByType(SceneType SceneType) =>
            scenes.FirstOrDefault(s => s.SceneType == SceneType)?.Reference.Name ?? "";

        public List<SceneData[]> GetBatchedByPriority()
        {
            return scenes
                .OrderBy(s => s.Priority)
                .GroupBy(s => s.Priority)
                .Select(g => g.ToArray())
                .ToList();
        }
    }
}
