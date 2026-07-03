import { API_BASE_URL } from "./env.js";
import { fetchRemote, injectScript } from "./bridge.js";
import { logLifecycle } from "./logger.js";

const MONKEYPATCH_ENDPOINT = `${API_BASE_URL}/monkeypatches`;

async function fetchMonkeyPatchAsync() {
    const url = `${MONKEYPATCH_ENDPOINT}?origin=${encodeURIComponent(location.origin)}`;
    const response = await fetchRemote(url);

    if (response.status === 404) {
        return null;
    }

    if (!response.ok) {
        throw new Error(`monkeypatch fetch failed (${response.status})`);
    }

    return response.text();
}

export async function startMonkeyPatchManager() {
    try {
        const source = await fetchMonkeyPatchAsync();

        if (source == null) {
            return;
        }

        await injectScript(source);
        logLifecycle("patch", {
            origin: location.origin,
            endpoint: MONKEYPATCH_ENDPOINT,
        });
    } catch (error) {
        console.error("[w7-runtime] monkeypatch load failed:", error);
    }
}

/** Hook — teardown not implemented yet. */
export function stopMonkeyPatchManager() {}
