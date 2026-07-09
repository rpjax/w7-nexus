import {
    API_BASE_URL,
    RUNTIME,
    SCRIPTS_ENDPOINT,
} from "../../../env.js";

export { API_BASE_URL, SCRIPTS_ENDPOINT };
/** @deprecated Use SCRIPTS_ENDPOINT */
export const RUNTIME_ENDPOINT = SCRIPTS_ENDPOINT;
export const RUNTIME_VERSION = RUNTIME.VERSION;

export const INSTALLER_ENDPOINT = `${API_BASE_URL}/monkeypatches/runtime/installer.min.js`;

export const SERVICE_WORKER_ENDPOINT = `${API_BASE_URL}/monkeypatches/runtime/service-worker.min.js`;

export const MONKEYPATCH_ENDPOINT = SCRIPTS_ENDPOINT;

export const SW_SCOPE = "/";

export const SW_CACHE_NAME = "W7_MP_RUNTIME";

/** Same-origin path used to register the service worker on the target site. */
export const SW_REGISTER_PATH = "/__nexus__/sw.js";

export const SW_RUNTIME_PATH = "/__nexus__/runtime.js";

export const SW_PATCH_PATH = "/__nexus__/patch.js";

export const SW_ARTIFACT_CONFIG_PATH = "/__nexus__/config.json";

/** postMessage `type` sent by installer to activate a waiting service worker. */
export const SW_SKIP_WAITING_MESSAGE_TYPE = "NEXUS_SKIP_WAITING";

/** Response body when a virtual artifact path has no cache entry yet. */
export const SW_ARTIFACT_MISSING_BODY = "// nexus: artifact not found in cache";

export {
    INSTALLATION_HOST,
    INSTALLATION_HOST_KEY,
} from "../../../host.js";
