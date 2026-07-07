import { API_BASE_URL } from "../../env.js";
import { getInstallationHost } from "../../host.js";
import { REMOTE_FETCH_HEADERS } from "../../hosts/xss/delivery/remote.js";
import { logError, logLifecycle, logWarn } from "../../logger.js";
import { injectScriptAsync } from "../../../shared/csp-kit/script-injector.js";

const MONKEYPATCH_ENDPOINT = `${API_BASE_URL}/monkeypatches`;

async function fetchMonkeyPatchAsync() {
    const url = `${MONKEYPATCH_ENDPOINT}?origin=${encodeURIComponent(location.origin)}`;
    const response = await fetch(url, { headers: REMOTE_FETCH_HEADERS });

    if (!response.ok) {
        if (response.status < 500) {
            logWarn("monkeypatch not available", { url, status: response.status });
            return null;
        }

        throw new Error(`monkeypatch fetch failed (${response.status})`);
    }

    return await response.text();
}

export async function startDefaultMonkeypatchAsync() {
    const host = getInstallationHost();

    try {
        const source = await fetchMonkeyPatchAsync();

        if (source == null) {
            return;
        }

        const execPath = await injectScriptAsync({ source });
        logLifecycle("patch", {
            origin: location.origin,
            endpoint: MONKEYPATCH_ENDPOINT,
            host,
            execPath,
        });
    } catch (error) {
        logError("monkeypatch load failed", { error, host });
    }
}
