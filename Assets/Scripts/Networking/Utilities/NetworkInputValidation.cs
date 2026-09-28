using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

namespace MyToolz.Networking.Utilities
{
    /// <summary>
    /// Server-side checks for values that arrive in client commands. A command is a request, not a
    /// fact: every value a client sends is sanitized or re-derived on the server before it changes
    /// synchronized state.
    /// </summary>
    public static class NetworkInputValidation
    {
        public const int MaxNicknameLength = 24;
        public const int MaxChatMessageLength = 256;

        /// <summary>
        /// Trims, strips control characters and TextMeshPro rich-text tags, and caps the length.
        /// Returns null when nothing printable remains.
        /// </summary>
        public static string SanitizeText(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var builder = new StringBuilder(Mathf.Min(value.Length, maxLength));
            foreach (char c in value)
            {
                if (char.IsControl(c))
                {
                    continue;
                }

                // Neutralize rich-text markup (<size=500>, <color>, <link>, …) instead of rendering it.
                builder.Append(c == '<' ? '‹' : c == '>' ? '›' : c);
                if (builder.Length >= maxLength)
                {
                    break;
                }
            }

            string sanitized = builder.ToString().Trim();
            return sanitized.Length == 0 ? null : sanitized;
        }

        public static string SanitizeNickname(string value) => SanitizeText(value, MaxNicknameLength);

        public static string SanitizeChatMessage(string value) => SanitizeText(value, MaxChatMessageLength);

        /// <summary>True when a finite position is within <paramref name="maxDistance"/> of <paramref name="origin"/>.</summary>
        public static bool IsNear(Vector3 position, Vector3 origin, float maxDistance)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
            {
                return false;
            }

            return (position - origin).sqrMagnitude <= maxDistance * maxDistance;
        }

        /// <summary>The sender's player object, or null when the connection has none.</summary>
        public static Core.NetworkPlayer GetPlayer(NetworkConnectionToClient conn) =>
            conn != null && conn.identity != null && conn.identity.TryGetComponent(out Core.NetworkPlayer player) ? player : null;
    }

    /// <summary>Per-connection minimum interval between accepted requests of one kind.</summary>
    public sealed class ConnectionRateLimiter
    {
        private readonly float minInterval;
        private readonly Dictionary<int, double> lastAccepted = new Dictionary<int, double>();

        public ConnectionRateLimiter(float minIntervalSeconds)
        {
            minInterval = Mathf.Max(0f, minIntervalSeconds);
        }

        /// <summary>Records and allows the request when the connection's last one is old enough.</summary>
        public bool TryAccept(NetworkConnectionToClient conn)
        {
            if (conn == null) return false;

            double now = NetworkTime.localTime;
            if (lastAccepted.TryGetValue(conn.connectionId, out double last) && now - last < minInterval)
            {
                return false;
            }

            lastAccepted[conn.connectionId] = now;
            return true;
        }

        public void Forget(NetworkConnectionToClient conn)
        {
            if (conn != null) lastAccepted.Remove(conn.connectionId);
        }

        public void Clear() => lastAccepted.Clear();
    }
}
