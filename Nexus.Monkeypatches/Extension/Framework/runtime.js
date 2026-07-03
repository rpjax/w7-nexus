import "./config.js";
import { startMonkeyPatchManager, stopMonkeyPatchManager } from "./monkeypatch_manager.js";
import { getState } from "./state.js";
import { logLifecycle } from "./logger.js";

export function alert(message) {
    window.alert(message);
}

let running = false;

function bindRuntimeApi() {
    window.w7runtime = {
        start,
        stop,
        getState: (key) => getState(key),
    };
}

function init() {
    bindRuntimeApi();
    logLifecycle("init", {
        host: location.hostname,
        origin: location.origin,
        surface: "window.w7runtime",
    });
}

export function start() {
    if (running) {
        return;
    }

    running = true;
    void startMonkeyPatchManager();
    logLifecycle("online", {
        host: location.hostname,
        origin: location.origin,
    });
}

export function stop() {
    if (!running) {
        return;
    }

    stopMonkeyPatchManager();
    running = false;
    logLifecycle("offline", {
        host: location.hostname,
        origin: location.origin,
    });
}

init();
start();
