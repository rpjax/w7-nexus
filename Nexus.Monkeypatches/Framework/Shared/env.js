/* =============================================================================
 * METADATA
 *
 * Build-time values embedded in bundles and reported to Nexus (e.g. via WebSocket).
 * ============================================================================= */

export const RUNTIME_VERSION = "0.0.0";

export const API_BASE_URL = "https://websete.localhost:444";

export const RUNTIME_ENDPOINT = `${API_BASE_URL}/monkeypatches/framework/runtime.min.js`;

export const NEXUS_HUB_URL = `${API_BASE_URL}/hubs/extension`;
