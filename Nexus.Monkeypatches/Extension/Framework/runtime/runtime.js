import "./config.js";
import { RUNTIME_VERSION } from "../env.js";
import { startMonkeyPatchManagerAsync, stopMonkeyPatchManager } from "./monkeypatch_manager.js";
import { getState } from "./state.js";
import { logLifecycle } from "../logger.js";
import { installMainWorldBridge } from "../bridge/main_world.js";
import { watchExtension, unwatchExtension } from "./extension_watcher.js";

const RUNTIME_GLOBAL_NAME = "w7runtime";

let running = false;

function buildRuntimeApi() {
    return {
        version: RUNTIME_VERSION,
        start: start,
        stop: stop,
        getState: getState,
        watchExtension: watchExtension,
        unwatchExtension: unwatchExtension,
    };
}

function installRuntimeApi() {
    window[RUNTIME_GLOBAL_NAME] = buildRuntimeApi();
}

function init() {
    installMainWorldBridge();
    installRuntimeApi();
    logLifecycle("init", {
        version: RUNTIME_VERSION,
        host: location.hostname,
        origin: location.origin,
        surface: `window.${RUNTIME_GLOBAL_NAME}`,
    });
}

async function start() {
    if (running) {
        return;
    }

    running = true;
    void startMonkeyPatchManagerAsync();
    logLifecycle("online", {
        host: location.hostname,
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
        host: location.hostname,
        origin: location.origin,
    });
}

init();
start();
