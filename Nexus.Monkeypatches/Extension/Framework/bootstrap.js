import { RUNTIME_ENDPOINT } from "./env.js";
import {
    ISOLATED_RELAY_MOUNTED,
    PAGE_BRIDGE_CHANNEL,
    PAGE_BRIDGE_GLOBAL,
    PrivilegedRequestKind,
    SERVICE_WORKER_REQUEST_TYPE,
    dispatchPrivilegedRequest,
    evalScriptInMainWorld,
    mountIsolatedWorldRelay,
    mountPageBridge,
} from "./bridge.js";

const tabBootstrapGeneration = new Map();

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    if (request.type !== SERVICE_WORKER_REQUEST_TYPE) {
        return;
    }

    void dispatchPrivilegedRequest(request, sender.tab?.id)
        .then((result) => sendResponse(result))
        .catch((error) => sendResponse({ error: error.message }));

    return true;
});

chrome.webNavigation.onCommitted.addListener((details) => {
    if (details.frameId !== 0) {
        return;
    }

    if (!details.url.startsWith("http://") && !details.url.startsWith("https://")) {
        return;
    }

    const generation = (tabBootstrapGeneration.get(details.tabId) ?? 0) + 1;
    tabBootstrapGeneration.set(details.tabId, generation);

    void bootstrapRuntimeForTab(details.tabId, generation).catch((error) => {
        console.error("[w7-bootstrap] runtime bootstrap failed:", error);
    });
});

chrome.tabs.onRemoved.addListener((tabId) => {
    tabBootstrapGeneration.delete(tabId);
});

function isStaleBootstrap(tabId, generation) {
    return tabBootstrapGeneration.get(tabId) !== generation;
}

async function mountPageBridgeForTab(tabId) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "ISOLATED",
        func: mountIsolatedWorldRelay,
        args: [PAGE_BRIDGE_CHANNEL, SERVICE_WORKER_REQUEST_TYPE, ISOLATED_RELAY_MOUNTED],
    });

    await chrome.scripting.executeScript({
        target: { tabId },
        world: "MAIN",
        func: mountPageBridge,
        args: [
            PAGE_BRIDGE_CHANNEL,
            PrivilegedRequestKind.FETCH,
            PrivilegedRequestKind.INJECT_SCRIPT,
            PAGE_BRIDGE_GLOBAL,
        ],
    });
}

async function fetchRuntimeSource() {
    const response = await fetch(RUNTIME_ENDPOINT);

    if (!response.ok) {
        throw new Error(`runtime fetch failed (${response.status})`);
    }

    return response.text();
}

async function bootstrapRuntimeForTab(tabId, generation) {
    await mountPageBridgeForTab(tabId);

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    const runtimeSource = await fetchRuntimeSource();

    if (isStaleBootstrap(tabId, generation)) {
        return;
    }

    await evalScriptInMainWorld(tabId, runtimeSource);
}
