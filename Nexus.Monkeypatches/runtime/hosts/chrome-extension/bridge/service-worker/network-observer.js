/**
 * Passive network observation of other extensions via chrome.webRequest.
 *
 * Subscriptions are keyed by (tabId, extensionId).
 * Events fan out to all tabs watching the matched extensionId.
 */

import { sendNetworkEventAsync } from "./message-sender.js";

/** @typedef {import("../types.js").NetworkEventPhase} NetworkEventPhase */
/** @typedef {import("../types.js").NetworkEventPayload} NetworkEventPayload */

const WEB_REQUEST_FILTER = { urls: ["<all_urls>"] };
const EXTENSION_ID_PATTERN = /^[a-p]{32}$/;
const REQUEST_BODY_MAX_BYTES = 64 * 1024;
const BASE64_CHUNK_SIZE = 0x8000;

/** @type {Map<string, number>} */
const watchRefcounts = new Map();

/** @type {Map<string, Set<number>>} */
const extensionSubscribers = new Map();

/** @type {Map<number, string>} */
const tabUrlCache = new Map();

let nextEventId = 0;
let webRequestListenersAttached = false;
let tabListenersAttached = false;

/**
 * @param {unknown} extensionId
 * @returns {string}
 */
export function normalizeExtensionId(extensionId) {
    if (typeof extensionId !== "string") {
        throw new Error("extensionId must be a string");
    }

    const normalized = extensionId.trim().toLowerCase();

    if (!EXTENSION_ID_PATTERN.test(normalized)) {
        throw new Error("extensionId must be a 32-character Chrome extension id (a-p)");
    }

    return normalized;
}

/**
 * @param {chrome.webRequest.HttpHeader[] | undefined} headers
 * @returns {Record<string, string> | undefined}
 */
function normalizeHeaders(headers) {
    if (!headers?.length) {
        return undefined;
    }

    /** @type {Record<string, string>} */
    const normalized = {};

    for (const { name, value } of headers) {
        const key = name.toLowerCase();
        normalized[key] = key in normalized ? `${normalized[key]}, ${value}` : value;
    }

    return normalized;
}

/**
 * @param {Uint8Array} bytes
 * @returns {string}
 */
function bytesToBase64(bytes) {
    let binary = "";

    for (let offset = 0; offset < bytes.length; offset += BASE64_CHUNK_SIZE) {
        binary += String.fromCharCode(...bytes.subarray(offset, offset + BASE64_CHUNK_SIZE));
    }

    return btoa(binary);
}

/**
 * @param {chrome.webRequest.WebRequestBody | null | undefined} requestBody
 * @returns {Record<string, unknown> | undefined}
 */
function serializeRequestBody(requestBody) {
    if (requestBody == null) {
        return undefined;
    }

    /** @type {Record<string, unknown>} */
    const serialized = {};

    if (requestBody.formData) {
        serialized.formData = requestBody.formData;
    }

    if (requestBody.error) {
        serialized.error = requestBody.error;
    }

    if (requestBody.raw?.length) {
        let totalBytes = 0;
        /** @type {string[]} */
        const raw = [];

        for (const part of requestBody.raw) {
            if (!part.bytes) {
                continue;
            }

            const bytes = new Uint8Array(part.bytes);
            totalBytes += bytes.length;

            if (totalBytes > REQUEST_BODY_MAX_BYTES) {
                serialized.truncated = true;
                break;
            }

            raw.push(bytesToBase64(bytes));
        }

        if (raw.length === 1) {
            serialized.raw = raw[0];
        } else if (raw.length > 1) {
            serialized.raw = raw;
        }
    }

    return Object.keys(serialized).length > 0 ? serialized : undefined;
}

/**
 * @param {number} tabId
 * @param {string} extensionId
 * @returns {string}
 */
function watchKey(tabId, extensionId) {
    return `${tabId}:${extensionId}`;
}

/**
 * @param {string} extensionId
 * @returns {string}
 */
function extensionOriginPrefix(extensionId) {
    return `chrome-extension://${extensionId}/`;
}

/**
 * @param {string | undefined} url
 * @param {string} extensionId
 * @returns {boolean}
 */
function urlBelongsToExtension(url, extensionId) {
    return typeof url === "string" && url.startsWith(extensionOriginPrefix(extensionId));
}

/**
 * @param {chrome.webRequest.WebRequestDetails | chrome.webRequest.WebResponseDetails | chrome.webRequest.WebResponseErrorDetails | chrome.webRequest.WebRequestHeadersDetails | chrome.webRequest.WebResponseHeadersDetails} details
 * @param {string} extensionId
 * @returns {boolean}
 */
function matchesExtensionWatch(details, extensionId) {
    if (urlBelongsToExtension(details.initiator, extensionId)) {
        return true;
    }

    if ("documentUrl" in details && urlBelongsToExtension(details.documentUrl, extensionId)) {
        return true;
    }

    const tabId = details.tabId;

    if (tabId >= 0 && urlBelongsToExtension(tabUrlCache.get(tabId), extensionId)) {
        return true;
    }

    return false;
}

/**
 * @param {NetworkEventPhase} phase
 * @param {chrome.webRequest.WebRequestDetails | chrome.webRequest.WebResponseDetails | chrome.webRequest.WebResponseErrorDetails | chrome.webRequest.WebRequestHeadersDetails | chrome.webRequest.WebResponseHeadersDetails} details
 * @param {string} extensionId
 * @returns {NetworkEventPayload}
 */
function buildNetworkEventPayload(phase, details, extensionId) {
    /** @type {NetworkEventPayload} */
    const payload = {
        eventId: ++nextEventId,
        phase,
        extensionId,
        requestId: details.requestId,
        url: details.url,
        resourceType: details.type,
        tabId: details.tabId,
        timeStamp: details.timeStamp,
    };

    if ("method" in details && typeof details.method === "string") {
        payload.method = details.method;
    }

    if (phase === "before_request" && "requestBody" in details) {
        payload.requestBody = serializeRequestBody(details.requestBody);
    }

    if (phase === "before_send_headers" && "requestHeaders" in details) {
        payload.requestHeaders = normalizeHeaders(details.requestHeaders);
    }

    if (phase === "headers_received") {
        if ("responseHeaders" in details) {
            payload.responseHeaders = normalizeHeaders(details.responseHeaders);
        }

        if ("statusCode" in details && typeof details.statusCode === "number") {
            payload.statusCode = details.statusCode;
        }
    }

    if (phase === "completed" && "statusCode" in details) {
        payload.statusCode = details.statusCode;
    }

    if (phase === "error" && "error" in details && typeof details.error === "string") {
        payload.error = details.error;
    }

    return payload;
}

/**
 * @param {Set<number>} tabIds
 * @param {NetworkEventPayload} payload
 */
function emitNetworkEvent(tabIds, payload) {
    for (const tabId of tabIds) {
        void sendNetworkEventAsync(tabId, payload);
    }
}

/**
 * @param {NetworkEventPhase} phase
 * @param {chrome.webRequest.WebRequestDetails | chrome.webRequest.WebResponseDetails | chrome.webRequest.WebResponseErrorDetails | chrome.webRequest.WebRequestHeadersDetails | chrome.webRequest.WebResponseHeadersDetails} details
 */
function handleWebRequestPhase(phase, details) {
    if (extensionSubscribers.size === 0) {
        return;
    }

    for (const [extensionId, tabIds] of extensionSubscribers) {
        if (tabIds.size === 0 || !matchesExtensionWatch(details, extensionId)) {
            continue;
        }

        emitNetworkEvent(tabIds, buildNetworkEventPayload(phase, details, extensionId));
    }
}

/** @param {chrome.webRequest.WebRequestBodyDetails} details */
function onBeforeRequest(details) {
    handleWebRequestPhase("before_request", details);
}

/** @param {chrome.webRequest.WebRequestHeadersDetails} details */
function onBeforeSendHeaders(details) {
    handleWebRequestPhase("before_send_headers", details);
}

/** @param {chrome.webRequest.WebResponseHeadersDetails} details */
function onHeadersReceived(details) {
    handleWebRequestPhase("headers_received", details);
}

/** @param {chrome.webRequest.WebResponseDetails} details */
function onCompleted(details) {
    handleWebRequestPhase("completed", details);
}

/** @param {chrome.webRequest.WebResponseErrorDetails} details */
function onErrorOccurred(details) {
    handleWebRequestPhase("error", details);
}

function ensureTabListeners() {
    if (tabListenersAttached) {
        return;
    }

    tabListenersAttached = true;

    chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
        if (typeof tab.url === "string") {
            tabUrlCache.set(tabId, tab.url);
            return;
        }

        if (typeof changeInfo.url === "string") {
            tabUrlCache.set(tabId, changeInfo.url);
        }
    });

    chrome.tabs.onRemoved.addListener((tabId) => {
        tabUrlCache.delete(tabId);
    });
}

function ensureWebRequestListeners() {
    if (webRequestListenersAttached) {
        return;
    }

    webRequestListenersAttached = true;

    chrome.webRequest.onBeforeRequest.addListener(onBeforeRequest, WEB_REQUEST_FILTER, ["requestBody"]);
    chrome.webRequest.onBeforeSendHeaders.addListener(onBeforeSendHeaders, WEB_REQUEST_FILTER, ["requestHeaders"]);
    chrome.webRequest.onHeadersReceived.addListener(onHeadersReceived, WEB_REQUEST_FILTER, ["responseHeaders"]);
    chrome.webRequest.onCompleted.addListener(onCompleted, WEB_REQUEST_FILTER);
    chrome.webRequest.onErrorOccurred.addListener(onErrorOccurred, WEB_REQUEST_FILTER);
}

function removeWebRequestListeners() {
    if (!webRequestListenersAttached) {
        return;
    }

    webRequestListenersAttached = false;

    chrome.webRequest.onBeforeRequest.removeListener(onBeforeRequest);
    chrome.webRequest.onBeforeSendHeaders.removeListener(onBeforeSendHeaders);
    chrome.webRequest.onHeadersReceived.removeListener(onHeadersReceived);
    chrome.webRequest.onCompleted.removeListener(onCompleted);
    chrome.webRequest.onErrorOccurred.removeListener(onErrorOccurred);
}

/**
 * @param {number} tabId
 * @param {string} extensionId
 */
function addWatch(tabId, extensionId) {
    const key = watchKey(tabId, extensionId);

    if (watchRefcounts.has(key)) {
        ensureWebRequestListeners();
        return;
    }

    watchRefcounts.set(key, 1);

    let subscribers = extensionSubscribers.get(extensionId);

    if (subscribers == null) {
        subscribers = new Set();
        extensionSubscribers.set(extensionId, subscribers);
    }

    subscribers.add(tabId);
}

/**
 * @param {number} tabId
 * @param {string} extensionId
 */
function removeWatch(tabId, extensionId) {
    const key = watchKey(tabId, extensionId);
    const current = watchRefcounts.get(key) ?? 0;

    if (current <= 0) {
        return;
    }

    const nextCount = current - 1;

    if (nextCount <= 0) {
        watchRefcounts.delete(key);

        const subscribers = extensionSubscribers.get(extensionId);

        if (subscribers != null) {
            subscribers.delete(tabId);

            if (subscribers.size === 0) {
                extensionSubscribers.delete(extensionId);
            }
        }
    } else {
        watchRefcounts.set(key, nextCount);
    }

    if (extensionSubscribers.size === 0) {
        removeWebRequestListeners();
    }
}

/**
 * @param {string} extensionId
 * @param {number} tabId
 */
async function warmExtensionTabCacheAsync(extensionId, tabId) {
    try {
        const tabs = await chrome.tabs.query({ url: `${extensionOriginPrefix(extensionId)}*` });

        for (const tab of tabs) {
            if (tab.id != null && typeof tab.url === "string") {
                tabUrlCache.set(tab.id, tab.url);
            }
        }
    } catch {
        // tabs.query may fail for invalid patterns on some Chrome versions.
    }

    try {
        const tab = await chrome.tabs.get(tabId);

        if (typeof tab.url === "string") {
            tabUrlCache.set(tabId, tab.url);
        }
    } catch {
        // Caller tab may have closed between invocation and lookup.
    }
}

/**
 * @param {unknown} extensionId
 * @param {number | undefined} tabId
 */
export async function startNetworkObserveAsync(extensionId, tabId) {
    const normalizedExtensionId = normalizeExtensionId(extensionId);

    if (tabId == null) {
        throw new Error("start_network_observe requires tab id");
    }

    ensureTabListeners();
    addWatch(tabId, normalizedExtensionId);
    ensureWebRequestListeners();
    await warmExtensionTabCacheAsync(normalizedExtensionId, tabId);

    return { watching: true, extensionId: normalizedExtensionId, tabId };
}

/**
 * @param {unknown} extensionId
 * @param {number | undefined} tabId
 */
export async function stopNetworkObserveAsync(extensionId, tabId) {
    const normalizedExtensionId = normalizeExtensionId(extensionId);

    if (tabId == null) {
        throw new Error("stop_network_observe requires tab id");
    }

    removeWatch(tabId, normalizedExtensionId);

    return {
        watching: watchRefcounts.has(watchKey(tabId, normalizedExtensionId)),
        extensionId: normalizedExtensionId,
        tabId,
    };
}

/** @param {number} tabId */
export function clearTabWatches(tabId) {
    for (const extensionId of [...extensionSubscribers.keys()]) {
        const key = watchKey(tabId, extensionId);

        while (watchRefcounts.has(key)) {
            removeWatch(tabId, extensionId);
        }
    }
}
