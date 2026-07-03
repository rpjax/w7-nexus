import { API_BASE_URL } from "./env.js";
import { fetchRemote, injectScript, isPageBridgeMounted } from "./bridge.js";
import { logLifecycle } from "./logger.js";

const MONKEYPATCH_ENDPOINT = `${API_BASE_URL}/monkeypatches`;

let active = false;

function requirePageBridge() {
    if (!isPageBridgeMounted()) {
        throw new Error("page bridge is not mounted");
    }
}

async function fetchMonkeyPatchAsync() {
    requirePageBridge();

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

async function injectMonkeyPatch(source) {
    requirePageBridge();
    await injectScript(source);
}

export async function startMonkeyPatchManager() {
    if (active) {
        return;
    }

    active = true;

    try {
        const source = await fetchMonkeyPatchAsync();

        if (!active || source == null) {
            return;
        }

        await injectMonkeyPatch(source);
        logLifecycle("patch", {
            origin: location.origin,
            endpoint: MONKEYPATCH_ENDPOINT,
        });
    } catch (error) {
        active = false;
        console.error("[w7-runtime] monkeypatch load failed:", error);
    }
}

export function stopMonkeyPatchManager() {
    active = false;
}
