import "./settings.js";
import { initializeRuntime } from "./initializer.js";
import {
    startMonkeyPatchManagerAsync,
    stopMonkeyPatchManager,
} from "./features/monkeypatch-manager/monkeypatch-manager.js";
import { getInstallationHost } from "./host.js";
import { logLifecycle } from "./logger.js";

let running = false;

async function start() {
    if (running) {
        return;
    }

    running = true;
    void startMonkeyPatchManagerAsync();
    logLifecycle("online", {
        installationHost: getInstallationHost(),
        hostname: location.hostname,
        origin: location.origin,
    });
}

function stop() {
    if (!running) {
        return;
    }

    stopMonkeyPatchManager();
    running = false;
    logLifecycle("offline", {
        installationHost: getInstallationHost(),
        hostname: location.hostname,
        origin: location.origin,
    });
}

initializeRuntime(start, stop);
start();
