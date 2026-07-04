import {
    ISOLATED_RELAY_MOUNTED,
    TARGET_ID,
    MESSAGE_TYPE,
    INVOCATION_METHOD,
} from "./bridge_core";
import { installIsolatedWorldBridge } from "./isolated_world";

/*
    Message Sending
*/

function sendMessageToIsolatedWorld(message) {
    message.isW7BridgeMessage = true;
    message.source = TARGET_ID.SERVICE_WORKER;
    message.target = TARGET_ID.ISOLATED_WORLD;
    chrome.runtime.sendMessage(message);
}

function sendInvocationResponse(
    invocationId,
    isSuccess,
    result,
    error) {
    sendMessageToIsolatedWorld({
        type: MESSAGE_TYPE.INVOCATION_RESPONSE,
        invocationId,
        isSuccess,
        result,
        error,
    });
}

function sendNetworkEvent() {
    //...send network event...
}

/*
    Message Handling
*/

async function handleEvalInMainWorldAsync({ source, args }) {
    //...handle eval in main world...
}

async function handleEvalInIsolatedWorldAsync({ source, args }) {
    //...handle eval in isolated world...
}

async function handleInvocationRequestAsync(message) {
    //...handle invocation request...
    const method = message.method;
    const args = message.args;
    const invocationId = message.invocationId;
    const result = null;

    try {
        switch (method) {
            case INVOCATION_METHOD.EVAL_IN_MAIN_WORLD: {
                result = await handleEvalInMainWorldAsync({ source, args });
                break;
            }
            case INVOCATION_METHOD.EVAL_IN_ISOLATED_WORLD: {
                result = await handleEvalInIsolatedWorldAsync({ source, args });
                break;
            }
            default: {
                return;
            }
        }
    } catch (error) {
        sendInvocationResponse(invocationId, false, null, error.message);
        return;
    }

    sendInvocationResponse(invocationId, true, result, null);
}

// listener for chrome.runtime.onMessage
export function handleServiceWorkerMessage(message) {
    if (!message.isW7BridgeMessage) {
        return;
    }
    if (message.target !== TARGET_ID.SERVICE_WORKER) {
        return;
    }

    switch (message.type) {
        case MESSAGE_TYPE.INVOCATION_REQUEST: {
            void (async () => {
                await handleInvocationRequestAsync(message);
            })();
            break;
        }
        default: {
            return;
        }
    }
}

/*
    Installation
*/

export async function installServiceWorkerBridgeAsync(tabId) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "ISOLATED",
        func: installIsolatedWorldBridge,
        args: [ISOLATED_RELAY_MOUNTED, MESSAGE_TYPE.INVOCATION_REQUEST, MESSAGE_TYPE.INVOCATION_RESPONSE],
    });
}