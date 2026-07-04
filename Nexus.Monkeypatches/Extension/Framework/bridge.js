// ─── Protocol ─────────────────────────────────────────────────────────────────

export const INVOCATION_REQUEST = "w7bridge:invocation:request";
export const INVOCATION_RESPONSE = "w7bridge:invocation:response";

export const FETCH_METHOD = "fetch";
export const EVAL_IN_MAIN_WORLD = "eval_in_main_world";
export const EVAL_IN_ISOLATED_WORLD = "eval_in_isolated_world";
export const SET_NETWORK_REDIRECT = "set_network_redirect";
export const UNSET_NETWORK_REDIRECT = "unset_network_redirect";

/** Global on window — message bus installed in the MAIN world. */
export const PAGE_BRIDGE_GLOBAL = "__w7bridge";

/** Guard on window — ensures the isolated relay is mounted once per frame. */
export const ISOLATED_RELAY_MOUNTED = "__w7bridge_isolated_relay_mounted";

// ─── Bootstrap & runtime install ───────────────────────────────────────────────
//
// Used by bootstrap.js to mount the bridge and inject the runtime into a tab.
// installIsolatedWorldBridge / installMainWorldBridge are self-contained
// (no module closures) — serialised and injected via chrome.scripting.executeScript.

/**
 * ISOLATED world — dumb relay: forwards postMessage requests to the SW and
 * posts responses back to MAIN. Always delivers an INVOCATION_RESPONSE so MAIN
 * never hangs on transport failure.
 *
 * @param {string} mountedKey
 * @param {string} invocationRequest
 * @param {string} invocationResponse
 */
function installIsolatedWorldBridge(mountedKey, invocationRequest, invocationResponse) {
    if (window[mountedKey]) {
        return;
    }

    window[mountedKey] = true;

    window.addEventListener("message", (event) => {
        if (event.source !== window) {
            return;
        }

        switch (event.data?.type) {
            case invocationRequest:
                void (async () => {
                    try {
                        const response = await chrome.runtime.sendMessage(event.data);

                        if (response?.type !== invocationResponse) {
                            window.postMessage({
                                type: invocationResponse,
                                id: event.data.id,
                                isSuccess: false,
                                result: null,
                                error: "invalid or missing service worker response",
                            });
                            return;
                        }

                        window.postMessage(response);
                    } catch (error) {
                        window.postMessage({
                            type: invocationResponse,
                            id: event.data.id,
                            isSuccess: false,
                            result: null,
                            error: error instanceof Error ? error.message : String(error),
                        });
                    }
                })();
                break;
        }
    });
}

/**
 * MAIN world — message bus: sends invocation requests to the SW and resolves
 * their responses by id.
 *
 * @param {string} globalName
 * @param {string} invocationRequest
 * @param {string} invocationResponse
 */
function installMainWorldBridge(globalName, invocationRequest, invocationResponse) {
    if (window[globalName]) {
        return;
    }

    let nextId = 0;
    /** @type {Map<number, { resolve: Function, reject: Function }>} */
    const pending = new Map();

    window.addEventListener("message", (event) => {
        if (event.source !== window) {
            return;
        }

        switch (event.data?.type) {
            case invocationResponse: {
                const d = event.data;
                const p = pending.get(d.id);

                if (!p) {
                    return;
                }

                pending.delete(d.id);
                d.isSuccess ? p.resolve(d.result) : p.reject(new Error(d.error ?? "request failed"));
                break;
            }
        }
    });

    /** @param {{ method: string, args: unknown }} message */
    async function sendMessageToServiceWorkerAsync({ method, args }) {
        return await new Promise((resolve, reject) => {
            const id = ++nextId;
            pending.set(id, { resolve, reject });
            window.postMessage({ type: invocationRequest, id, method, args });
        });
    }

    window[globalName] = { sendMessageToServiceWorkerAsync };
}

/**
 * Inject the relay (ISOLATED) and the message bus (MAIN) into a tab.
 *
 * @param {number} tabId
 */
export async function installBridgeAsync(tabId) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "ISOLATED",
        func: installIsolatedWorldBridge,
        args: [ISOLATED_RELAY_MOUNTED, INVOCATION_REQUEST, INVOCATION_RESPONSE],
    });

    await chrome.scripting.executeScript({
        target: { tabId },
        world: "MAIN",
        func: installMainWorldBridge,
        args: [PAGE_BRIDGE_GLOBAL, INVOCATION_REQUEST, INVOCATION_RESPONSE],
    });
}

/**
 * Inject the runtime bundle into MAIN for a tab.
 *
 * @param {number} tabId
 * @param {string} runtimeSource
 */
export async function injectRuntimeInMainWorldAsync(tabId, runtimeSource) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "MAIN",
        func: (code) => {
            (0, eval)(code);
        },
        args: [runtimeSource],
    });
}

// ─── Service worker: dispatch & invocation handlers ───────────────────────────
//
// Wire chrome.runtime.onMessage to handleServiceWorkerMessage in bootstrap.
// New message types / methods are a new case in the switches below.

/**
 * Entry point for chrome.runtime.onMessage — routes by message.type.
 *
 * @param {{ type: string, [key: string]: unknown }} message
 * @param {chrome.runtime.MessageSender} sender
 */
export function handleServiceWorkerMessage(message, sender) {
    switch (message.type) {
        case INVOCATION_REQUEST:
            return handleInvocationRequestAsync(message, sender);
    }
}

/**
 * @param {{ id: number, method: string, args: unknown }} request
 * @param {chrome.runtime.MessageSender} sender
 */
async function handleInvocationRequestAsync(request, sender) {
    try {
        switch (request.method) {
            case FETCH_METHOD:
                return await handleFetchAsync(request);

            case EVAL_IN_MAIN_WORLD:
                return await handleEvalAsync(request, sender, "MAIN");

            case EVAL_IN_ISOLATED_WORLD:
                return await handleEvalAsync(request, sender, "ISOLATED");

            case SET_NETWORK_REDIRECT:
                return await handleSetNetworkRedirectAsync(request);

            case UNSET_NETWORK_REDIRECT:
                return await handleUnsetNetworkRedirectAsync(request);

            default:
                return {
                    type: INVOCATION_RESPONSE,
                    id: request.id,
                    isSuccess: false,
                    result: null,
                    error: `unknown method: ${request.method ?? "(none)"}`,
                };
        }
    } catch (error) {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: error instanceof Error ? error.message : String(error),
        };
    }
}

/**
 * @param {{ id: number, args: { url: string } }} request
 */
async function handleFetchAsync(request) {
    if (typeof request.args?.url !== "string") {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "fetch requires args.url",
        };
    }

    const response = await fetch(request.args.url);
    const result = {
        ok: response.ok,
        status: response.status,
        statusText: response.statusText,
        url: response.url,
        redirected: response.redirected,
        type: response.type,
        headers: Object.fromEntries(response.headers.entries()),
        body: await response.text(),
    };

    return {
        type: INVOCATION_RESPONSE,
        id: request.id,
        isSuccess: true,
        result,
        error: null,
    };
}

/**
 * Run source as the body of async (args) => { ... } in the given world.
 *
 * @param {{ id: number, args: { source: string, args?: unknown } }} request
 * @param {chrome.runtime.MessageSender} sender
 * @param {"MAIN" | "ISOLATED"} world
 */
async function handleEvalAsync(request, sender, world) {
    if (typeof request.args?.source !== "string") {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "eval requires args.source",
        };
    }

    const tabId = sender.tab?.id;

    if (tabId == null) {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "eval requires tab id",
        };
    }

    const results = await chrome.scripting.executeScript({
        target: { tabId },
        world,
        func: (source, args) => (0, eval)(`(async (args) => { ${source} })`)(args),
        args: [request.args.source, request.args.args ?? null],
    });

    if (!results?.length) {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "no injection result",
        };
    }

    return {
        type: INVOCATION_RESPONSE,
        id: request.id,
        isSuccess: true,
        result: results[0].result,
        error: null,
    };
}

/**
 * @param {{ id: number, args: { rules: object[] } }} request
 */
async function handleSetNetworkRedirectAsync(request) {
    const rules = request.args?.rules;

    if (!Array.isArray(rules) || rules.length === 0) {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "set_network_redirect requires args.rules (non-empty array)",
        };
    }

    const existing = await chrome.declarativeNetRequest.getDynamicRules();
    const usedIds = new Set(existing.map((rule) => rule.id));
    let nextId = 1;
    const removeRuleIds = [];
    /** @type {chrome.declarativeNetRequest.Rule[]} */
    const addRules = [];

    for (const spec of rules) {
        const condition = spec?.condition;
        const redirect = spec?.redirect;

        if (condition == null || (typeof condition.urlFilter !== "string" && typeof condition.regexFilter !== "string")) {
            return {
                type: INVOCATION_RESPONSE,
                id: request.id,
                isSuccess: false,
                result: null,
                error: "each rule requires condition.urlFilter or condition.regexFilter",
            };
        }

        if (
            redirect == null ||
            (typeof redirect.url !== "string" &&
                typeof redirect.regexSubstitution !== "string" &&
                redirect.transform == null)
        ) {
            return {
                type: INVOCATION_RESPONSE,
                id: request.id,
                isSuccess: false,
                result: null,
                error: "each rule requires redirect.url, redirect.regexSubstitution, or redirect.transform",
            };
        }

        let ruleId;

        if (typeof spec.id === "number") {
            ruleId = spec.id;
        } else {
            while (usedIds.has(nextId)) {
                nextId += 1;
            }

            ruleId = nextId;
            nextId += 1;
        }

        if (usedIds.has(ruleId) && existing.some((rule) => rule.id === ruleId)) {
            removeRuleIds.push(ruleId);
        } else if (usedIds.has(ruleId)) {
            return {
                type: INVOCATION_RESPONSE,
                id: request.id,
                isSuccess: false,
                result: null,
                error: `duplicate rule id in batch: ${ruleId}`,
            };
        }

        usedIds.add(ruleId);

        addRules.push({
            id: ruleId,
            priority: typeof spec.priority === "number" ? spec.priority : 1,
            action: { type: "redirect", redirect },
            condition,
        });
    }

    await chrome.declarativeNetRequest.updateDynamicRules({
        removeRuleIds: [...new Set(removeRuleIds)],
        addRules,
    });

    return {
        type: INVOCATION_RESPONSE,
        id: request.id,
        isSuccess: true,
        result: { ruleIds: addRules.map((rule) => rule.id) },
        error: null,
    };
}

/**
 * @param {{ id: number, args: { ids?: number[], all?: boolean } }} request
 */
async function handleUnsetNetworkRedirectAsync(request) {
    const args = request.args ?? {};
    /** @type {number[]} */
    let removeRuleIds;

    if (args.all === true) {
        const existing = await chrome.declarativeNetRequest.getDynamicRules();
        removeRuleIds = existing.map((rule) => rule.id);
    } else if (Array.isArray(args.ids) && args.ids.length > 0 && args.ids.every((id) => typeof id === "number")) {
        removeRuleIds = args.ids;
    } else {
        return {
            type: INVOCATION_RESPONSE,
            id: request.id,
            isSuccess: false,
            result: null,
            error: "unset_network_redirect requires args.all === true or args.ids (non-empty number array)",
        };
    }

    if (removeRuleIds.length > 0) {
        await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds });
    }

    return {
        type: INVOCATION_RESPONSE,
        id: request.id,
        isSuccess: true,
        result: { removedIds: removeRuleIds },
        error: null,
    };
}

// ─── Public API ───────────────────────────────────────────────────────────────
//
// Imported by runtime modules. Requires the bridge to be mounted (installBridgeAsync).

function getPageBridge() {
    const bridge = window[PAGE_BRIDGE_GLOBAL];

    if (bridge == null) {
        throw new Error("page bridge is not mounted");
    }

    return bridge;
}

/** @param {{ method: string, args: unknown }} message */
export async function invokeAsync({ method, args }) {
    return await getPageBridge().sendMessageToServiceWorkerAsync({ method, args });
}

/** @param {{ url: string }} args */
export async function fetchAsync(args) {
    return await invokeAsync({ method: FETCH_METHOD, args });
}

/** @param {{ source: string, args?: unknown }} request */
export async function evalInMainWorldAsync({ source, args = null }) {
    return await invokeAsync({ method: EVAL_IN_MAIN_WORLD, args: { source, args } });
}

/** @param {{ source: string, args?: unknown }} request */
export async function evalInIsolatedWorldAsync({ source, args = null }) {
    return await invokeAsync({ method: EVAL_IN_ISOLATED_WORLD, args: { source, args } });
}

/** @param {{ rules: object[] }} args */
export async function setNetworkRedirectAsync(args) {
    return await invokeAsync({ method: SET_NETWORK_REDIRECT, args });
}

/** @param {{ ids?: number[], all?: boolean }} args */
export async function unsetNetworkRedirectAsync(args) {
    return await invokeAsync({ method: UNSET_NETWORK_REDIRECT, args });
}
