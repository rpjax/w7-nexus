/**
 * Outbound bridge channel: service worker → MAIN world via the isolated relay.
 *
 * Uses `chrome.tabs.sendMessage` → ISOLATED `onMessage` → `postMessage` → MAIN.
 */

import { TARGET_ID, MESSAGE_TYPE } from "../bridge-core.js";

/**
 * @param {number} tabId
 * @param {Record<string, unknown>} message
 */
async function sendMessageToMainWorld(tabId, message) {
    await chrome.tabs.sendMessage(tabId, {
        ...message,
        isW7BridgeMessage: true,
        source: TARGET_ID.SERVICE_WORKER,
        target: TARGET_ID.MAIN_WORLD,
    });
}

/**
 * @param {number | undefined} tabId
 * @param {import("../types.js").NetworkEventPayload} payload
 */
export async function sendNetworkEventAsync(tabId, payload) {
    if (tabId == null) {
        return;
    }

    try {
        await sendMessageToMainWorld(tabId, {
            type: MESSAGE_TYPE.NETWORK_EVENT,
            ...payload,
        });
    } catch {
        // Tab closed or relay not mounted.
    }
}

/**
 * @param {number | undefined} tabId
 * @param {number} invocationId
 * @param {boolean} isSuccess
 * @param {unknown} result
 * @param {string | null} error
 */
export async function sendInvocationResponseAsync(tabId, invocationId, isSuccess, result, error) {
    if (tabId == null) {
        return;
    }

    try {
        await sendMessageToMainWorld(tabId, {
            type: MESSAGE_TYPE.INVOCATION_RESPONSE,
            invocationId,
            isSuccess,
            result,
            error,
        });
    } catch {
        // Tab closed or relay not mounted — response cannot be delivered.
    }
}
