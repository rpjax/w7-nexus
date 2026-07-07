/* =============================================================================
 * REMOTE ENDPOINTS
 *
 * URLs served from wwwroot / Nexus API. Patches and other payloads are fetched
 * from MONKEYPATCH_ENDPOINT — not part of the framework install lifecycle.
 * ============================================================================= */

export const API_BASE_URL = "https://babette-xeric-zaida.ngrok-free.dev";

export const INSTALLER_ENDPOINT = `${API_BASE_URL}/monkeypatches/framework/installer.min.js`;

export const RUNTIME_ENDPOINT = `${API_BASE_URL}/monkeypatches/framework/runtime.min.js`;

export const SERVICE_WORKER_ENDPOINT = `${API_BASE_URL}/monkeypatches/framework/service-worker.min.js`;

export const MONKEYPATCH_ENDPOINT = `${API_BASE_URL}/monkeypatches`;


/* =============================================================================
 * SERVICE WORKER
 *
 * Registration scope, Cache API storage, same-origin virtual artifact paths,
 * and installer ↔ service worker messaging.
 * ============================================================================= */

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


/* =============================================================================
 * WINDOW KEYS
 *
 * Properties attached to `window` for cross-script discovery within a page.
 * ============================================================================= */

export const RUNTIME_WINDOW_KEY = "__w7_mp_runtime__";


/* =============================================================================
 * METADATA
 *
 * Build-time values embedded in bundles and reported to Nexus (e.g. via WebSocket).
 * ============================================================================= */

/** Reported by the runtime on connect; bump on each runtime release. */
export const RUNTIME_VERSION = "0.0.0";
