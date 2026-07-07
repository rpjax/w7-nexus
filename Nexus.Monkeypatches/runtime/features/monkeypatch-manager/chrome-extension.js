import { API_BASE_URL } from "../../env.js";
import { evalInMainWorldAsync, fetchAsync } from "../../hosts/chrome-extension/bridge/main-world.js";
import { logError, logLifecycle, logWarn } from "../../logger.js";

const MONKEYPATCH_ENDPOINT = `${API_BASE_URL}/monkeypatches`;

async function fetchMonkeyPatchAsync() {
    const url = `${MONKEYPATCH_ENDPOINT}?origin=${encodeURIComponent(location.origin)}`;
    const response = await fetchAsync({ url });

    if (!response.ok) {
        if (response.status < 500) {
            logWarn("monkeypatch not available", { url, status: response.status });
            return null;
        }

        throw new Error(`monkeypatch fetch failed (${response.status})`);
    }

    return response.body;
}

export async function startChromeExtensionMonkeypatchAsync() {
    try {
        const source = await fetchMonkeyPatchAsync();

        if (source == null) {
            return;
        }

        await evalInMainWorldAsync({ source });
        logLifecycle("patch", {
            origin: location.origin,
            endpoint: MONKEYPATCH_ENDPOINT,
            host: "CHROME_EXTENSION",
        });
    } catch (error) {
        logError("monkeypatch load failed", { error, host: "CHROME_EXTENSION" });
    }
}
