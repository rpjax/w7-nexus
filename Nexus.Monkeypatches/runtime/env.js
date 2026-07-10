/* =============================================================================
 * METADATA
 *
 * Build-time values embedded in bundles and reported to Nexus (e.g. via WebSocket).
 * ============================================================================= */

export const RUNTIME = {
    VERSION: "0.0.0",
    KEY: "w7runtime",
};

export const CHROME_EXTENSION = {
    KEY: "chromeExtension",
    BRIDGE_KEY: "bridge",
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

export const API_ENDPOINT = {
    SCRIPTS: `${API_BASE_URL}/scripts`,
    NEXUS_HUB: `${API_BASE_URL}/hubs/extension`,
};
