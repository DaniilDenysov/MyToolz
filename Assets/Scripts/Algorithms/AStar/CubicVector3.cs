using System;
using UnityEngine;

namespace MyToolz.Math
{
    [System.Serializable]
    public class CubicVector3
    {
        public float Q => q;
        public float R => r;
        public float S => s;

        [SerializeField] private float q;
        [SerializeField] private float r;
        [SerializeField] private float s;

        public CubicVector3()
        {

        }

        public CubicVector3(float q, float r, float s)
        {
            if (Mathf.RoundToInt(q + r + s) != 0)
                throw new ArgumentException("Invalid cube coordinates: q + r + s must equal 0.");

            this.q = q;
            this.r = r;
            this.s = s;
        }

        public static float Distance(CubicVector3 vec1, CubicVector3 vec2)
        {
            return (Mathf.Abs(vec1.q - vec2.q) + Mathf.Abs(vec1.r - vec2.r) + Mathf.Abs(vec1.s - vec2.s)) / 2f;
        }

        public static CubicVector3 operator +(CubicVector3 a, CubicVector3 b)
        {
            return new CubicVector3(a.q + b.q, a.r + b.r, a.s + b.s);
        }

        public static CubicVector3 operator -(CubicVector3 a, CubicVector3 b)
        {
            return new CubicVector3(a.q - b.q, a.r - b.r, a.s - b.s);
        }

        public static implicit operator Vector3(CubicVector3 c)
        {
            return new Vector3(c.q, c.r, c.s);
        }

        public static implicit operator Vector3Int(CubicVector3 c)
        {
            return new Vector3Int((int)c.q, (int)c.r, (int)c.s);
        }

        public static CubicVector3 FromAxial(float q, float r)
        {
            return new CubicVector3(q, r, -q - r);
        }


        public static explicit operator CubicVector3(Vector3 v)
        {
            return new CubicVector3(v.x, v.y, v.z);
        }

        public static explicit operator CubicVector3(Vector3Int v)
        {
            return new CubicVector3(v.x, v.y, v.z);
        }

        public override string ToString()
        {
            return $"({q}, {r}, {s})";
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as CubicVector3);
        }

        public bool Equals(CubicVector3 other)
        {
            if (other is null)
                return false;

            return Mathf.Approximately(q, other.q) &&
                   Mathf.Approximately(r, other.r) &&
                   Mathf.Approximately(s, other.s);
        }

        public override int GetHashCode()
        {
            int hq = Mathf.RoundToInt(q * 1000f);
            int hr = Mathf.RoundToInt(r * 1000f);
            int hs = Mathf.RoundToInt(s * 1000f);
            return hq ^ hr << 2 ^ hs >> 2;
        }

        public static CubicVector3 WorldToCube(Vector3 position, float radius)
        {
            float q = (Mathf.Sqrt(3f) / 3f * position.x - 1f / 3f * position.z) / radius;
            float r = (2f / 3f * position.z) / radius;
            float s = -q - r;
            return RoundCube(q, r, s);
        }

        private static CubicVector3 RoundCube(float q, float r, float s)
        {
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                rq = -rr - rs;
            }
            else if (rDiff > sDiff)
            {
                rr = -rq - rs;
            }
            else
            {
                rs = -rq - rr;
            }

            return new CubicVector3(rq, rr, rs);
        }


        public static bool operator ==(CubicVector3 left, CubicVector3 right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(CubicVector3 left, CubicVector3 right)
        {
            return !(left == right);
        }
    }
}
