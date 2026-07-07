import {
    RUNTIME,
    RUNTIME_API,
    CHROME_EXTENSION,
    STATE_MANAGER_API,
    EXTENSION_WATCHER_API,
} from "./env.js";
import { unwatchAllExtensionsAsync, unwatchExtension, watchExtension } from "./features/extension_watcher/extension-watcher.js";
import { getState, createState, deleteState } from "./features/state-manager/state-manager.js";
import { getInstallationHost, INSTALLATION_HOST } from "./host.js";
import {
    buildChromeExtensionBridgeApi,
    installChromeExtensionBridge,
} from "./hosts/chrome-extension/bridge/main-world.js";
import * as logger from "./logger.js";

function initializeChromeExtension() {
    installChromeExtensionBridge();
}

function initializeHost() {
    switch (getInstallationHost()) {
        case INSTALLATION_HOST.CHROME_EXTENSION:
            initializeChromeExtension();
            return;
        default:
            return;
    }
}

function buildBaseApi(start, stop) {
    return {
        [RUNTIME_API.VERSION]: RUNTIME.VERSION,
        [RUNTIME_API.HOST]: getInstallationHost(),
        [RUNTIME_API.START]: start,
        [RUNTIME_API.STOP]: stop,
        [RUNTIME_API.STATE_MANAGER]: {
            [STATE_MANAGER_API.GET_STATE]: getState,
            [STATE_MANAGER_API.CREATE_STATE]: createState,
            [STATE_MANAGER_API.DELETE_STATE]: deleteState,
        },
        [RUNTIME_API.LOGGER]: logger,
        // features
        [RUNTIME_API.EXTENSION_WATCHER]: {
            [EXTENSION_WATCHER_API.WATCH_EXTENSION]: watchExtension,
            [EXTENSION_WATCHER_API.UNWATCH_EXTENSION]: unwatchExtension,
        },
    };
}

function buildBrowserExtensionApi(start, stop) {
    const stopWithCleanup = () => {
        stop();
        void unwatchAllExtensionsAsync();
    };

    return {
        ...buildBaseApi(start, stopWithCleanup),
        [CHROME_EXTENSION.WINDOW_NAME]: {
            [CHROME_EXTENSION.BRIDGE_WINDOW_NAME]: buildChromeExtensionBridgeApi(),
        },
    };
}

function buildRuntimeApi(start, stop) {
    const host = getInstallationHost();

    switch (host) {
        case INSTALLATION_HOST.CHROME_EXTENSION:
            return buildBrowserExtensionApi(start, stop);
        default:
            return buildBaseApi(start, stop);
    }
}

function installRuntimeApi(start, stop) {
    window[RUNTIME.WINDOW_NAME] = buildRuntimeApi(start, stop);
}

export function initializeRuntime(start, stop) {
    const host = getInstallationHost();

    initializeHost();
    installRuntimeApi(start, stop);

    logger.logLifecycle("init", {
        version: RUNTIME.VERSION,
        installationHost: host,
        hostname: location.hostname,
        origin: location.origin,
        surface: `window.${RUNTIME.WINDOW_NAME}`,
    });
}
