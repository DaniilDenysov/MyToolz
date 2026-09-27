// Bridges MyToolz.IO.WebGlFileSync -> Emscripten FS.syncfs.
// callback signature: void callback(int statusCode)
//   1  = success
//  -1  = terminal failure after bounded retry
//  -2  = timeout; commit state is unknown
//
// Only errors that are plausibly transient are retried. Quota/security failures are
// reported immediately because retrying them cannot create storage capacity/access.
mergeInto(LibraryManager.library, {
  MyToolz_SyncFsToIndexedDb: function (callback) {
    var finished = false;
    var attempt = 0;
    var maxAttempts = 3;
    var timeoutMs = 15000;
    var mount = Module["__unityIdbfsMount"] && Module["__unityIdbfsMount"].mount;
    var autoPersistMount = Module["autoSyncPersistentDataPath"] && mount ? mount : null;
    var autoPersistWaitStartedAt = Date.now();

    var complete = function (statusCode) {
      if (finished) return;
      finished = true;
      {{{ makeDynCall('vi', 'callback') }}}(statusCode);
    };

    var errorName = function (err) {
      if (!err) return "";
      return err.name || (err.target && err.target.error && err.target.error.name) || "";
    };

    var isTransient = function (err) {
      var name = errorName(err);
      return name === "AbortError" ||
             name === "UnknownError" ||
             name === "InvalidStateError" ||
             name === "TransactionInactiveError" ||
             name === "NetworkError";
    };

    var retryDelayMs = function (attemptNumber) {
      return attemptNumber === 1 ? 100 : 300;
    };

    var prepareAutoPersist = function () {
      if (!autoPersistMount) return true;

      var state = autoPersistMount.idbPersistState;
      if (typeof state === "number" && state !== 0) {
        // Unity's autosync queued this same mutation for the next tick. Cancel that
        // timer so the awaited sync below is the only IndexedDB transaction.
        clearTimeout(state);
        autoPersistMount.idbPersistState = 0;
        return true;
      }

      return !state;
    };

    var releaseAutoPersist = function () {
      if (!autoPersistMount) return false;

      var needsAnotherPass = autoPersistMount.idbPersistState === "again";
      autoPersistMount.idbPersistState = 0;
      return needsAnotherPass;
    };

    var runWhenAutoPersistIdle = function () {
      if (finished) return;

      if (!prepareAutoPersist()) {
        if (Date.now() - autoPersistWaitStartedAt >= timeoutMs) {
          console.error("[MyToolz] Timed out waiting for Unity IDBFS autosync to become idle.");
          complete(-2);
          return;
        }

        setTimeout(runWhenAutoPersistIdle, 25);
        return;
      }

      runAttempt();
    };

    var runAttempt = function () {
      if (finished) return;
      attempt++;
      var settled = false;

      // Participate in Unity's autosync state machine. File changes made while this
      // awaited sync is in flight set the state to "again" and receive a second pass.
      if (autoPersistMount) autoPersistMount.idbPersistState = "idb";

      var timer = setTimeout(function () {
        if (settled || finished) return;
        settled = true;
        console.error("[MyToolz] IndexedDB sync timed out after " + timeoutMs + " ms (attempt " + attempt + ").");
        complete(-2);
      }, timeoutMs);

      try {
        FS.syncfs(false, function (err) {
          if (settled || finished) return;
          settled = true;
          clearTimeout(timer);
          var needsAnotherPass = releaseAutoPersist();

          if (!err) {
            if (needsAnotherPass) {
              attempt = 0;
              autoPersistWaitStartedAt = Date.now();
              runWhenAutoPersistIdle();
              return;
            }

            complete(1);
            return;
          }

          if (isTransient(err) && attempt < maxAttempts) {
            console.warn("[MyToolz] Transient IndexedDB sync failure; retrying (attempt " + attempt + "/" + maxAttempts + "):", err);
            setTimeout(runAttempt, retryDelayMs(attempt));
            return;
          }

          console.error("[MyToolz] IndexedDB sync failed:", err);
          complete(-1);
        });
      } catch (e) {
        if (settled || finished) return;
        settled = true;
        clearTimeout(timer);
        releaseAutoPersist();

        if (isTransient(e) && attempt < maxAttempts) {
          console.warn("[MyToolz] Transient IndexedDB sync exception; retrying (attempt " + attempt + "/" + maxAttempts + "):", e);
          setTimeout(runAttempt, retryDelayMs(attempt));
          return;
        }

        console.error("[MyToolz] IndexedDB sync threw:", e);
        complete(-1);
      }
    };

    runWhenAutoPersistIdle();
  }
});
