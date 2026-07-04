// blueprints
// isW7BridgeMessage: a flag to indicate that the message is a W7 bridge message
// type: the type of the message. This is used to determine the 'event' type. Messages should be trated like events.

export const BRIDGE_GLOBAL_NAME = "w7framework_bridge";

export const TARGET_ID = {
    SERVICE_WORKER: "service_worker",
    ISOLATED_WORLD: "isolated_world",
    MAIN_WORLD: "main_world",
};

export const MESSAGE_TYPE = {
    RELAY_ERROR: "w7bridge:relay_error",
    INVOCATION_REQUEST: "w7bridge:invocation:request",
    INVOCATION_RESPONSE: "w7bridge:invocation:response",
    NETWORK_EVENT: "w7bridge:network:event",
};

export const INVOCATION_METHOD = {
    EVAL_IN_MAIN_WORLD: "eval_in_main_world",
    EVAL_IN_ISOLATED_WORLD: "eval_in_isolated_world",
    FETCH: "fetch",
    SET_NETWORK_REDIRECT: "set_network_redirect",
    UNSET_NETWORK_REDIRECT: "unset_network_redirect",
};

const MESSAGE_BASE = {
    isW7BridgeMessage: true,
    source: "",
    target: "",
    type: "",
};

const INVOCATION_EXAMPLE = {
    // all messages have
    isW7BridgeMessage: true,
    source: TARGET_ID.MAIN_WORLD,
    target: TARGET_ID.SERVICE_WORKER,
    type: MESSAGE_TYPE.INVOCATION_REQUEST,
    // invocation request specific
    invocationId: 1, // the id of the invocation request
    method: "eval_in_main_world",
    args: {
        source: "alert('Hello, world!');",
    },
};

const INVOCATION_RESPONSE_EXAMPLE = {
    // all messages have
    isW7BridgeMessage: true,
    source: TARGET_ID.SERVICE_WORKER,
    target: TARGET_ID.MAIN_WORLD,
    type: MESSAGE_TYPE.INVOCATION_RESPONSE,
    // invocation response specific
    invocationId: 1, // the id of the invocation request
    isSuccess: true,
    result: "Hello, world!",
    error: null,
};

const NETWORK_EVENT_EXAMPLE = {
    // all messages have
    isW7BridgeMessage: true,
    source: TARGET_ID.SERVICE_WORKER,
    target: TARGET_ID.MAIN_WORLD,
    type: MESSAGE_TYPE.NETWORK_EVENT,
    // network event specific
    eventId: 1,
    url: "https://example.com",
    method: "GET",
    status: 200,
};

const ERROR_MESSAGE_EXAMPLE = {
    // all messages have
    isW7BridgeMessage: true,
    type: MESSAGE_TYPE.ERROR,
    // error specific
    sourceMessage: INVOCATION_EXAMPLE, // the message that caused the error
    error: "An error occurred",
};
