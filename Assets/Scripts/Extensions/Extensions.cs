using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Extensions
{
    public static class Extensions
    {
        public static float ToFloat(this double arg)
        {
            return float.Parse(arg.ToString());
        }
    }
}
