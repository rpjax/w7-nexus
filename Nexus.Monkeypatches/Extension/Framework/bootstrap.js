import { RUNTIME_ENDPOINT } from "./env.js";
import {
    handleServiceWorkerMessage,
    injectRuntimeInMainWorldAsync,
    installBridgeAsync,
} from "./bridge.js";

const tabBootstrapGeneration = new Map();

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
});

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
    await installBridgeAsync(tabId);

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    const runtimeSource = await fetchRuntimeSourceAsync();

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    await injectRuntimeInMainWorldAsync(tabId, runtimeSource);
}
