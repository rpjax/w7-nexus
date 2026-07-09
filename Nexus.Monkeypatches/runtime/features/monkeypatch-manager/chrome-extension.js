import { SCRIPTS_ENDPOINT } from "../../env.js";
import { evalInMainWorldAsync, fetchAsync } from "../../hosts/chrome-extension/bridge/main-world.js";
import { logError, logLifecycle, logWarn } from "../../logger.js";

async function fetchScriptsForHostAsync() {
    const url = `${SCRIPTS_ENDPOINT}?host=${encodeURIComponent(location.hostname)}&channel=prod`;
    const response = await fetchAsync({ url });

    if (!response.ok) {
        if (response.status < 500) {
            logWarn("scripts not available", { url, status: response.status });
            return [];
        }

        throw new Error(`scripts fetch failed (${response.status})`);
    }

    const payload = JSON.parse(response.body);
    return payload.items ?? [];
}

export async function startChromeExtensionMonkeypatchAsync() {
    try {
        const scripts = await fetchScriptsForHostAsync();

        if (scripts.length === 0) {
            return;
        }

        for (const script of scripts) {
            await evalInMainWorldAsync({ source: script.sourceCode });
            logLifecycle("patch", {
                name: script.name,
                version: script.version,
                origin: location.origin,
                host: "CHROME_EXTENSION",
            });
        }
    } catch (error) {
        logError("monkeypatch load failed", { error, host: "CHROME_EXTENSION" });
    }
}
