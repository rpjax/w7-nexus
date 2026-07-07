/* =============================================================================
 * METADATA
 *
 * Build-time values embedded in bundles and reported to Nexus (e.g. via WebSocket).
 * ============================================================================= */

export const RUNTIME = {
    VERSION: "0.0.0",
    WINDOW_NAME: "w7runtime",
};

export const CHROME_EXTENSION = {
    WINDOW_NAME: "chromeExtension",
    BRIDGE_WINDOW_NAME: "bridge",
};

export const RUNTIME_API = {
    VERSION: "version",
    HOST: "host",
    START: "start",
    STOP: "stop",
    LOGGER: "logger",
    STATE_MANAGER: "stateManager",
    EXTENSION_WATCHER: "extensionWatcher",
};

export const STATE_MANAGER_API = {
    GET_STATE: "getState",
    CREATE_STATE: "createState",
    DELETE_STATE: "deleteState",
};

export const EXTENSION_WATCHER_API = {
    WATCH_EXTENSION: "watchExtension",
    UNWATCH_EXTENSION: "unwatchExtension",
};

export const API_BASE_URL = "https://websete.localhost:444";

export const RUNTIME_ENDPOINT = `${API_BASE_URL}/monkeypatches/runtime/runtime.min.js`;

export const NEXUS_HUB_URL = `${API_BASE_URL}/hubs/extension`;
