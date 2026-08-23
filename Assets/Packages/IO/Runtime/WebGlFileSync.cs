using System.Runtime.InteropServices;

namespace MyToolz.IO
{
    /// <summary>
    /// Flushes Unity's IndexedDB-backed WebGL filesystem so that file writes to
    /// persistentDataPath actually survive a page refresh. It's a no-op on every
    /// platform except a real WebGL player (files persist on their own there).
    /// </summary>
    internal static class WebGlFileSync
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void MyToolz_SyncFsToIndexedDb();

        public static void Flush() => MyToolz_SyncFsToIndexedDb();
#else
        public static void Flush() { }
#endif
    }
}
