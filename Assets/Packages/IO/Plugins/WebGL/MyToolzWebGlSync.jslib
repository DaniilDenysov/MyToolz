// Bridges MyToolz.IO.WebGlFileSync → Emscripten's IndexedDB filesystem flush.
// Unity mounts Application.persistentDataPath as an IDBFS (IndexedDB) mount and
// only loads it into memory at startup; changes stay in memory until synced, so
// without this a WebGL file save is lost on refresh. Called after each save.
mergeInto(LibraryManager.library, {
  MyToolz_SyncFsToIndexedDb: function () {
    try {
      FS.syncfs(false, function (err) {
        if (err) {
          console.error("[MyToolz] IndexedDB sync failed:", err);
        }
      });
    } catch (e) {
      console.error("[MyToolz] IndexedDB sync threw:", e);
    }
  }
});
