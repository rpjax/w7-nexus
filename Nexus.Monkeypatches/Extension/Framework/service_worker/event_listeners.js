import { RUNTIME_ENDPOINT } from "../env.js";
import {
    handleServiceWorkerMessage,
    injectRuntimeInMainWorldAsync,
    installIsolatedBridgeAsync,
} from "../bridge/service_worker/service_worker.js";
import { clearTabWatches } from "../bridge/service_worker/network_observer.js";

/** Per-tab generation counter — cancels stale bootstrap when navigation races. */
const tabBootstrapGeneration = new Map();

function isStaleBootstrap(tabId, generation) {
    return tabBootstrapGeneration.get(tabId) !== generation;
}

async function fetchRuntimeSourceAsync() {
    const response = await fetch(RUNTIME_ENDPOINT);

    if (!response.ok) {
        throw new Error(`runtime fetch failed (${response.status})`);
    }

    return await response.text();
}

async function installRuntimeAsync(tabId, generation) {
    // Relay must exist before runtime posts invocation requests.
    await installIsolatedBridgeAsync(tabId);

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    const runtimeSource = await fetchRuntimeSourceAsync();

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    await injectRuntimeInMainWorldAsync(tabId, runtimeSource);
}

export function setupEventListeners() {
    chrome.runtime.onMessage.addListener(handleServiceWorkerMessage);

    chrome.webNavigation.onCommitted.addListener((details) => {
        if (details.frameId !== 0) {
            return;
        }

        if (!details.url.startsWith("http://") && !details.url.startsWith("https://")) {
            return;
        }

        const generation = (tabBootstrapGeneration.get(details.tabId) ?? 0) + 1;
        tabBootstrapGeneration.set(details.tabId, generation);

        void (async () => {
            try {
                await installRuntimeAsync(details.tabId, generation);
            } catch (error) {
                console.error("[w7-bootstrap] runtime bootstrap failed:", error);
            }
        })();
    });

    chrome.tabs.onRemoved.addListener((tabId) => {
        tabBootstrapGeneration.delete(tabId);
        clearTabWatches(tabId);
    });
}
