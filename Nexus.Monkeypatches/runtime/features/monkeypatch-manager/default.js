import { getInstallationHost } from "../../host.js";
import { REMOTE_FETCH_HEADERS } from "../../hosts/xss/delivery/remote.js";
import { logError, logLifecycle, logWarn } from "../../logger.js";
import { injectScriptAsync } from "../../../shared/csp-kit/script-injector.js";
import { fetchScriptsAsync } from "../../api/scripts-client.js";

export async function startDefaultMonkeypatchAsync() {
    const host = getInstallationHost();

    try {
        const scripts = await fetchScriptsAsync({
            host: location.hostname,
            fetchImpl: (url) => fetch(url, { headers: REMOTE_FETCH_HEADERS }),
        });

        if (scripts.length === 0) {
            logWarn("no scripts available for host", { host: location.hostname });
            return;
        }

        for (const script of scripts) {
            const execPath = await injectScriptAsync({ source: script.sourceCode });
            logLifecycle("patch", {
                name: script.name,
                version: script.version,
                origin: location.origin,
                host,
                execPath,
            });
        }
    } catch (error) {
        logError("monkeypatch load failed", { error, host });
    }
}
