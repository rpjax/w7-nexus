import {
    TARGET_ID,
    MESSAGE_TYPE,
    EVAL_IN_MAIN_WORLD_METHOD
} from "./bridge_core";

import { CompletionSource } from "../helpers/completion_source";
import { logError } from "../logger";

const pendingInvocations = new Map();
const nextInvocationId = 0;
const networkEventListeners = [];

/*
    Message Sending
*/

function sendMessageToServiceWorker(message) {
    message.isW7BridgeMessage = true;
    message.source = TARGET_ID.MAIN_WORLD;
    message.target = TARGET_ID.SERVICE_WORKER;
    window.postMessage(message);
}

function sendInvocationRequest(method, args) {
    const invocationId = ++nextInvocationId;
    const completionSource = new CompletionSource();
    pendingInvocations.set(invocationId, completionSource);
    sendMessageToServiceWorker({
        type: MESSAGE_TYPE.INVOCATION_REQUEST,
        invocationId,
        method,
        args
    });
    return invocationId;
}

/*
    Message Handling
*/

function handleRelayError(message) {
    //...handle relay error...
    // if pending invocation, reject with the error
    // else, log the error to let the caller know that the message was not sent
    const d = message;
    const p = pendingInvocations.get(d.invocationId);
    if (p) {
        p.reject(new Error(d.error ?? "request failed"));
    } else {
        logError("Relay error: message not sent", { message });
    }
}

function handleInvocationResponse(message) {
    const d = message;
    const p = pendingInvocations.get(d.invocationId);
    if (!p) {
        return;
    }
    pendingInvocations.delete(d.invocationId);
    d.isSuccess ? p.resolve(d.result) : p.reject(new Error(d.error ?? "request failed"));
}

function handleNetworkEvent(message) {
    //...handle network event...
    networkEventListeners.forEach(listener => listener(message));
}

function handleMainWorldMessage(message) {
    switch (message.type) {
        case MESSAGE_TYPE.RELAY_ERROR: {
            handleRelayError(message);
            break;
        }
        case MESSAGE_TYPE.INVOCATION_RESPONSE: {
            handleInvocationResponse(message);
            break;
        }
        case MESSAGE_TYPE.NETWORK_EVENT: {
            handleNetworkEvent(message);
            break;
        }
        default: {
            return;
        }
    }
}

function handleMessageEvent(event) {
    if (event.source !== window) {
        return;
    }
    if (!event.data?.isW7BridgeMessage) {
        return;
    }
    if (event.data.target !== TARGET_ID.MAIN_WORLD) {
        return;
    }

    handleMainWorldMessage(event.data);
}

/*
    Public API
*/

/*
    Invocation
*/

export function invokeAsync(method, args) {
    const invocationId = sendInvocationRequest(method, args);
    const completionSource = pendingInvocations.get(invocationId);
    if (!completionSource) {
        throw new Error(`Invocation request failed: ${invocationId}`);
    }
    return completionSource.promise;
}

export function evalInMainWorldAsync({ source, args = null }) {
    return invokeAsync(EVAL_IN_MAIN_WORLD_METHOD, { source, args });
}

export function evalInIsolatedWorldAsync({ source, args = null }) {
    return invokeAsync(EVAL_IN_ISOLATED_WORLD_METHOD, { source, args });
}

export function fetchAsync(args) {
    return invokeAsync(FETCH_METHOD, args);
}

// etc... other invocation methods...

export function addNetworkEventListener(listener) {
    networkEventListeners.push(listener);
}

export function removeNetworkEventListener(listener) {
    networkEventListeners.splice(networkEventListeners.indexOf(listener), 1);
}

function buildBridgeApi() {
    return {
        invokeAsync,
        evalInMainWorldAsync,
        evalInIsolatedWorldAsync,
        fetchAsync,
    }
}

/*
    Installation
*/

export function installMainWorldBridge() {
    if (window[BRIDGE_GLOBAL_NAME]) {
        return;
    }

    window.addEventListener("message", handleMessageEvent);
    window[BRIDGE_GLOBAL_NAME] = buildBridgeApi();
}


