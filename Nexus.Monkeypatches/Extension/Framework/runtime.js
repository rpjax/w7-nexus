import "./config.js";
import { RUNTIME_VERSION } from "./env.js";
import { startMonkeyPatchManagerAsync, stopMonkeyPatchManager } from "./monkeypatch_manager.js";
import { getState } from "./state.js";
import { logLifecycle } from "./logger.js";
import { evalInMainWorldAsync } from "./bridge.js";

export function alert(message) {
    window.alert(message);
}

let running = false;

function installRuntimeApi() {
    window.w7runtime = {
        version: RUNTIME_VERSION,
        start,
        stop,
        getState: (key) => getState(key),
    };
}

function init() {
    installRuntimeApi();
    logLifecycle("init", {
        version: RUNTIME_VERSION,
        host: location.hostname,
        origin: location.origin,
        surface: "window.w7runtime",
    });
}

export async function start() {
    if (running) {
        return;
    }

    running = true;
    void startMonkeyPatchManagerAsync();
    logLifecycle("online", {
        host: location.hostname,
        origin: location.origin,
    });
    // alert "Hello, world!"
    await evalInMainWorldAsync({ source: "alert('Hello, world!');" });
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
