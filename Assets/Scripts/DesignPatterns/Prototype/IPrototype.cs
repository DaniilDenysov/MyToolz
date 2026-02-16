using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.DesignPatterns.Prototype
{
    public interface IPrototype<T>
    {
        public T Get();
    }
}
