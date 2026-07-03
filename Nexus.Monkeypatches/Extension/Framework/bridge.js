/** postMessage channel between MAIN and ISOLATED worlds. */
export const PAGE_BRIDGE_CHANNEL = "w7-page-bridge";

/** chrome.runtime.sendMessage type handled by the service worker. */
export const SERVICE_WORKER_REQUEST_TYPE = "w7-bridge-request";

/** Global on window — privileged API for the runtime (MAIN world). */
export const PAGE_BRIDGE_GLOBAL = "__w7_pageBridge";

/** Guard on window — isolated relay mounted once per frame. */
export const ISOLATED_RELAY_MOUNTED = "__w7_isolatedRelayMounted";

export const PrivilegedRequestKind = {
    FETCH: "fetch",
    INJECT_SCRIPT: "inject-script",
};

/**
 * @param {unknown} result
 * @returns {result is { error: string }}
 */
function isErrorResult(result) {
    return result != null && typeof result === "object" && "error" in result;
}

/**
 * ISOLATED world — forwards page bridge requests to the service worker.
 *
 * @param {string} pageChannel
 * @param {string} serviceWorkerRequestType
 * @param {string} mountedKey
 */
export function mountIsolatedWorldRelay(pageChannel, serviceWorkerRequestType, mountedKey) {
    if (window[mountedKey]) {
        return;
    }

    window[mountedKey] = true;

    window.addEventListener("message", (event) => {
        if (event.source !== window || event.data?.channel !== pageChannel) {
            return;
        }

        const { requestId, kind, url, scriptSource } = event.data;

        chrome.runtime.sendMessage(
            { type: serviceWorkerRequestType, kind, url, scriptSource },
            (result) => {
                if (chrome.runtime.lastError) {
                    window.postMessage(
                        { channel: pageChannel, requestId, error: chrome.runtime.lastError.message },
                        "*",
                    );
                    return;
                }

                window.postMessage({ channel: pageChannel, requestId, result }, "*");
            },
        );
    });
}

/**
 * MAIN world — exposes privileged fetch/inject to the runtime bundle.
 *
 * @param {string} pageChannel
 * @param {string} fetchKind
 * @param {string} injectKind
 * @param {string} globalName
 */
export function mountPageBridge(pageChannel, fetchKind, injectKind, globalName) {
    if (window[globalName]) {
        return;
    }

    let nextRequestId = 0;
    /** @type {Map<number, { resolve: Function, reject: Function }>} */
    const pendingRequests = new Map();

    window.addEventListener("message", (event) => {
        if (event.source !== window || event.data?.channel !== pageChannel) {
            return;
        }

        const pending = pendingRequests.get(event.data.requestId);
        if (!pending) {
            return;
        }

        pendingRequests.delete(event.data.requestId);

        if (event.data.error) {
            pending.reject(new Error(event.data.error));
            return;
        }

        if (isErrorResult(event.data.result)) {
            pending.reject(new Error(event.data.result.error));
            return;
        }

        pending.resolve(event.data.result);
    });

    function sendPrivilegedRequest(kind, payload) {
        return new Promise((resolve, reject) => {
            const requestId = ++nextRequestId;
            pendingRequests.set(requestId, { resolve, reject });
            window.postMessage({ channel: pageChannel, requestId, kind, ...payload }, "*");
        });
    }

    window[globalName] = {
        fetchRemote(url) {
            return sendPrivilegedRequest(fetchKind, { url }).then((result) => ({
                ok: result.ok,
                status: result.status,
                text: async () => result.body,
            }));
        },
        injectScript(scriptSource) {
            return sendPrivilegedRequest(injectKind, { scriptSource });
        },
    };
}

// --- Runtime (MAIN world) ---

function getPageBridge() {
    const bridge = window[PAGE_BRIDGE_GLOBAL];

    if (bridge == null) {
        throw new Error("page bridge is not mounted");
    }

    return bridge;
}

export function isPageBridgeMounted() {
    return window[PAGE_BRIDGE_GLOBAL] != null;
}

export function fetchRemote(url) {
    return getPageBridge().fetchRemote(url);
}

export function injectScript(scriptSource) {
    return getPageBridge().injectScript(scriptSource);
}

// --- Service worker ---

/**
 * @param {{ kind: string, url?: string, scriptSource?: string }} request
 * @param {number | undefined} tabId
 */
export async function dispatchPrivilegedRequest(request, tabId) {
    switch (request.kind) {
        case PrivilegedRequestKind.FETCH: {
            const response = await fetch(request.url);
            const body = await response.text();

            return {
                ok: response.ok,
                status: response.status,
                body,
            };
        }

        case PrivilegedRequestKind.INJECT_SCRIPT: {
            if (tabId == null) {
                throw new Error("privileged inject requires tab id");
            }

            await evalScriptInMainWorld(tabId, request.scriptSource);
            return { ok: true };
        }

        default:
            throw new Error(`unknown privileged request kind: ${request.kind}`);
    }
}

/**
 * @param {number} tabId
 * @param {string} scriptSource
 */
export async function evalScriptInMainWorld(tabId, scriptSource) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "MAIN",
        func: (code) => {
            (0, eval)(code);
        },
        args: [scriptSource],
    });
}
