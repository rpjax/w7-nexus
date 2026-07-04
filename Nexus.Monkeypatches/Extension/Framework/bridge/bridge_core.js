/**
 * W7 bridge protocol — shared constants and message-shape examples.
 *
 * Every envelope carries `{ isW7BridgeMessage, source, target, type, ...payload }`.
 * Messages are treated as events; the isolated relay never awaits `sendMessage` return values.
 *
 * @see bridge/README.md
 */

/** Global on `window` where the MAIN-world API is exposed after `installMainWorldBridge()`. */
export const BRIDGE_GLOBAL_NAME = "w7framework_bridge";

/** Guard on `window` — ensures the isolated relay is mounted once per frame. */
export const ISOLATED_RELAY_MOUNTED = "__w7bridge_isolated_relay_mounted";

/** Logical endpoints in the bridge graph. */
export const TARGET_ID = {
    SERVICE_WORKER: "service_worker",
    ISOLATED_WORLD: "isolated_world",
    MAIN_WORLD: "main_world",
};

/** Event types on the bridge bus. */
export const MESSAGE_TYPE = {
    RELAY_ERROR: "w7bridge:relay_error",
    INVOCATION_REQUEST: "w7bridge:invocation:request",
    INVOCATION_RESPONSE: "w7bridge:invocation:response",
    NETWORK_EVENT: "w7bridge:network:event",
};

/** Privileged operations dispatched by the service worker. */
export const INVOCATION_METHOD = {
    EVAL_IN_MAIN_WORLD: "eval_in_main_world",
    EVAL_IN_ISOLATED_WORLD: "eval_in_isolated_world",
    FETCH: "fetch",
    SET_NETWORK_REDIRECT: "set_network_redirect",
    UNSET_NETWORK_REDIRECT: "unset_network_redirect",
};

// ── Live contract examples (keep in sync with handlers) ─────────────────────

/** @type {import("./types.js").InvocationRequestMessage} */
const INVOCATION_EXAMPLE = {
    isW7BridgeMessage: true,
    source: TARGET_ID.MAIN_WORLD,
    target: TARGET_ID.SERVICE_WORKER,
    type: MESSAGE_TYPE.INVOCATION_REQUEST,
    invocationId: 1,
    method: INVOCATION_METHOD.EVAL_IN_MAIN_WORLD,
    args: {
        source: "return args?.greeting ?? 'Hello';",
        args: { greeting: "Hello, world!" },
    },
};

/** @type {import("./types.js").InvocationResponseMessage} */
const INVOCATION_RESPONSE_EXAMPLE = {
    isW7BridgeMessage: true,
    source: TARGET_ID.SERVICE_WORKER,
    target: TARGET_ID.MAIN_WORLD,
    type: MESSAGE_TYPE.INVOCATION_RESPONSE,
    invocationId: 1,
    isSuccess: true,
    result: "Hello, world!",
    error: null,
};

/** @type {import("./types.js").NetworkEventMessage} */
const NETWORK_EVENT_EXAMPLE = {
    isW7BridgeMessage: true,
    source: TARGET_ID.SERVICE_WORKER,
    target: TARGET_ID.MAIN_WORLD,
    type: MESSAGE_TYPE.NETWORK_EVENT,
    eventId: 1,
    url: "https://example.com",
    method: "GET",
    status: 200,
};

/** @type {import("./types.js").RelayErrorMessage} */
const RELAY_ERROR_EXAMPLE = {
    isW7BridgeMessage: true,
    type: MESSAGE_TYPE.RELAY_ERROR,
    source: TARGET_ID.ISOLATED_WORLD,
    target: TARGET_ID.MAIN_WORLD,
    sourceMessage: INVOCATION_EXAMPLE,
    error: "Extension context invalidated",
};
