using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace MyToolz.SceneManagement
{
    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public SceneType SceneType;
        public uint Priority;
        public string Name => Reference.Name;
    }
}
