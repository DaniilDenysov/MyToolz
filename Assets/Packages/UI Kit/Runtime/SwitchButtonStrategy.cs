using System;
using UnityEngine;

namespace MyToolz.UI.Kit
{
    [Serializable]
    public abstract class SwitchButtonStrategy
    {
        public abstract Sprite[] Icons { get; }

        public abstract int CurrentIndex { get; }

        public abstract void Select(int index);

        public abstract void Register(Action changed);

        public abstract void Deregister(Action changed);
    }
}
