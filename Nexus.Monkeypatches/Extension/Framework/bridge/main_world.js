/**
 * MAIN world — bridge client installed by the runtime bundle.
 *
 * Posts invocation requests to the isolated relay, correlates responses by
 * `invocationId`, and exposes a promise-based API on `window[BRIDGE_GLOBAL_NAME]`.
 */

import {
    BRIDGE_GLOBAL_NAME,
    TARGET_ID,
    MESSAGE_TYPE,
    INVOCATION_METHOD,
} from "./bridge_core.js";
import { CompletionSource } from "../helpers/completion_source.js";
import { logError } from "../logger.js";

/** @type {Map<number, CompletionSource>} */
const pendingInvocations = new Map();

/** @type {Array<(message: import("./types.js").NetworkEventMessage) => void>} */
const networkEventListeners = [];

let nextInvocationId = 0;

// ── Outbound ────────────────────────────────────────────────────────────────

/** @param {Record<string, unknown>} message */
function sendMessageToServiceWorker(message) {
    window.postMessage({
        ...message,
        isW7BridgeMessage: true,
        source: TARGET_ID.MAIN_WORLD,
        target: TARGET_ID.SERVICE_WORKER,
    });
}

/**
 * @param {string} method
 * @param {unknown} [args]
 * @returns {CompletionSource}
 */
function sendInvocationRequest(method, args) {
    const invocationId = ++nextInvocationId;
    const completionSource = new CompletionSource();

    pendingInvocations.set(invocationId, completionSource);
    sendMessageToServiceWorker({
        type: MESSAGE_TYPE.INVOCATION_REQUEST,
        invocationId,
        method,
        args,
    });

    return completionSource;
}

// ── Inbound ─────────────────────────────────────────────────────────────────

/** @param {import("./types.js").RelayErrorMessage} message */
function handleRelayError(message) {
    const invocationId = message.sourceMessage?.invocationId;
    const pending = invocationId != null ? pendingInvocations.get(invocationId) : null;

    if (pending) {
        pendingInvocations.delete(invocationId);
        pending.reject(new Error(message.error ?? "relay error"));
        return;
    }

    logError("relay error", { message });
}

/** @param {import("./types.js").InvocationResponseMessage} message */
function handleInvocationResponse(message) {
    const pending = pendingInvocations.get(message.invocationId);

    if (!pending) {
        return;
    }

    pendingInvocations.delete(message.invocationId);

    if (message.isSuccess) {
        pending.resolve(message.result);
    } else {
        pending.reject(new Error(message.error ?? "request failed"));
    }
}

/** @param {import("./types.js").NetworkEventMessage} message */
function handleNetworkEvent(message) {
    for (const listener of networkEventListeners) {
        listener(message);
    }
}

/** @param {import("./types.js").W7BridgeEnvelope & Record<string, unknown>} message */
function handleMainWorldMessage(message) {
    switch (message.type) {
        case MESSAGE_TYPE.RELAY_ERROR:
            handleRelayError(/** @type {import("./types.js").RelayErrorMessage} */ (message));
            break;

        case MESSAGE_TYPE.INVOCATION_RESPONSE:
            handleInvocationResponse(/** @type {import("./types.js").InvocationResponseMessage} */ (message));
            break;

        case MESSAGE_TYPE.NETWORK_EVENT:
            handleNetworkEvent(/** @type {import("./types.js").NetworkEventMessage} */ (message));
            break;
    }
}

/** @param {MessageEvent} event */
function handleMessageEvent(event) {
    if (event.source !== window) {
        return;
    }

    const message = event.data;

    if (!message?.isW7BridgeMessage || message.target !== TARGET_ID.MAIN_WORLD) {
        return;
    }

    handleMainWorldMessage(message);
}

// ── Public API — invocation ─────────────────────────────────────────────────

/**
 * Invoke a privileged method in the service worker.
 *
 * @param {string} method - `INVOCATION_METHOD` value.
 * @param {unknown} [args]
 * @returns {Promise<unknown>}
 */
export function invokeAsync(method, args) {
    return sendInvocationRequest(method, args).promise;
}

/** @param {{ source: string, args?: unknown }} params */
export function evalInMainWorldAsync({ source, args = null }) {
    return invokeAsync(INVOCATION_METHOD.EVAL_IN_MAIN_WORLD, { source, args });
}

/** @param {{ source: string, args?: unknown }} params */
export function evalInIsolatedWorldAsync({ source, args = null }) {
    return invokeAsync(INVOCATION_METHOD.EVAL_IN_ISOLATED_WORLD, { source, args });
}

/** @param {{ url: string }} args */
export function fetchAsync(args) {
    return invokeAsync(INVOCATION_METHOD.FETCH, args);
}

/** @param {object} args - DNR rule specs (`rules` array). */
export function setNetworkRedirectAsync(args) {
    return invokeAsync(INVOCATION_METHOD.SET_NETWORK_REDIRECT, args);
}

/** @param {{ all?: boolean, ids?: number[] }} [args] */
export function unsetNetworkRedirectAsync(args) {
    return invokeAsync(INVOCATION_METHOD.UNSET_NETWORK_REDIRECT, args);
}

// ── Public API — network events (push from SW; observe not wired yet) ───────

/** @param {(message: import("./types.js").NetworkEventMessage) => void} listener */
export function addNetworkEventListener(listener) {
    networkEventListeners.push(listener);
}

/** @param {(message: import("./types.js").NetworkEventMessage) => void} listener */
export function removeNetworkEventListener(listener) {
    const index = networkEventListeners.indexOf(listener);

    if (index !== -1) {
        networkEventListeners.splice(index, 1);
    }
}

function buildBridgeApi() {
    return {
        invokeAsync,
        evalInMainWorldAsync,
        evalInIsolatedWorldAsync,
        fetchAsync,
        setNetworkRedirectAsync,
        unsetNetworkRedirectAsync,
        addNetworkEventListener,
        removeNetworkEventListener,
    };
}

// ── Installation ────────────────────────────────────────────────────────────

/** Idempotent — safe to call on every runtime init. */
export function installMainWorldBridge() {
    if (window[BRIDGE_GLOBAL_NAME]) {
        return;
    }

    window.addEventListener("message", handleMessageEvent);
    window[BRIDGE_GLOBAL_NAME] = buildBridgeApi();
}
