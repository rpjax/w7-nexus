/**
 * Service worker — bridge dispatcher and bootstrap installers.
 *
 * Receives `INVOCATION_REQUEST` events from the isolated relay, runs privileged
 * handlers, and pushes `INVOCATION_RESPONSE` back to MAIN via `tabs.sendMessage`.
 */

import {
    ISOLATED_RELAY_MOUNTED,
    TARGET_ID,
    MESSAGE_TYPE,
    INVOCATION_METHOD,
} from "./bridge_core.js";
import { installIsolatedWorldRelay } from "./isolated_world.js";

// ── DNR serial queue ────────────────────────────────────────────────────────
// Prevents read-modify-write races when concurrent set/unset calls share getDynamicRules().

/** @type {Promise<void>} */
let dnrQueue = Promise.resolve();

/**
 * Runs a declarativeNetRequest task after prior DNR tasks finish.
 *
 * @template T
 * @param {() => Promise<T>} task
 * @returns {Promise<T>}
 */
function runDnrSerialAsync(task) {
    const result = dnrQueue.then(task);
    dnrQueue = result.then(
        () => {},
        () => {},
    );
    return result;
}

// ── Outbound to MAIN (via isolated relay) ───────────────────────────────────

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
 * @param {number} invocationId
 * @param {boolean} isSuccess
 * @param {unknown} result
 * @param {string | null} error
 */
async function sendInvocationResponse(tabId, invocationId, isSuccess, result, error) {
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

/**
 * Push a network observation event to MAIN (handler wiring is future work).
 *
 * @param {number | undefined} tabId
 * @param {Record<string, unknown>} payload
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

// ── Invocation handlers ─────────────────────────────────────────────────────

/** @param {{ url?: string }} args */
async function handleFetchAsync(args) {
    if (typeof args?.url !== "string") {
        throw new Error("fetch requires args.url");
    }

    const response = await fetch(args.url);

    return {
        ok: response.ok,
        status: response.status,
        statusText: response.statusText,
        url: response.url,
        redirected: response.redirected,
        type: response.type,
        headers: Object.fromEntries(response.headers.entries()),
        body: await response.text(),
    };
}

/**
 * @param {{ source?: string, args?: unknown }} args
 * @param {number | undefined} tabId
 * @param {"MAIN" | "ISOLATED"} world
 */
async function handleEvalAsync(args, tabId, world) {
    if (typeof args?.source !== "string") {
        throw new Error("eval requires args.source");
    }

    if (tabId == null) {
        throw new Error("eval requires tab id");
    }

    const results = await chrome.scripting.executeScript({
        target: { tabId },
        world,
        func: (source, evalArgs) => (0, eval)(`(async (args) => { ${source} })`)(evalArgs),
        args: [args.source, args.args ?? null],
    });

    if (!results?.length) {
        throw new Error("no injection result");
    }

    return results[0].result;
}

/** @param {{ rules?: unknown[] }} args */
async function handleSetNetworkRedirectAsync(args) {
    const rules = args?.rules;

    if (!Array.isArray(rules) || rules.length === 0) {
        throw new Error("set_network_redirect requires args.rules (non-empty array)");
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
            throw new Error("each rule requires condition.urlFilter or condition.regexFilter");
        }

        if (
            redirect == null ||
            (typeof redirect.url !== "string" &&
                typeof redirect.regexSubstitution !== "string" &&
                redirect.transform == null)
        ) {
            throw new Error("each rule requires redirect.url, redirect.regexSubstitution, or redirect.transform");
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
            throw new Error(`duplicate rule id in batch: ${ruleId}`);
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

    return { ruleIds: addRules.map((rule) => rule.id) };
}

/** @param {{ all?: boolean, ids?: number[] }} [args] */
async function handleUnsetNetworkRedirectAsync(args) {
    const payload = args ?? {};
    /** @type {number[]} */
    let removeRuleIds;

    if (payload.all === true) {
        const existing = await chrome.declarativeNetRequest.getDynamicRules();
        removeRuleIds = existing.map((rule) => rule.id);
    } else if (
        Array.isArray(payload.ids) &&
        payload.ids.length > 0 &&
        payload.ids.every((id) => typeof id === "number")
    ) {
        removeRuleIds = payload.ids;
    } else {
        throw new Error("unset_network_redirect requires args.all === true or args.ids (non-empty number array)");
    }

    if (removeRuleIds.length > 0) {
        await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds });
    }

    return { removedIds: removeRuleIds };
}

// ── Inbound dispatch ────────────────────────────────────────────────────────

/**
 * @param {import("./types.js").InvocationRequestMessage} message
 * @param {chrome.runtime.MessageSender} sender
 */
async function handleInvocationRequestAsync(message, sender) {
    const { method, args, invocationId } = message;
    const tabId = sender.tab?.id;

    try {
        let result;

        switch (method) {
            case INVOCATION_METHOD.FETCH:
                result = await handleFetchAsync(args);
                break;

            case INVOCATION_METHOD.EVAL_IN_MAIN_WORLD:
                result = await handleEvalAsync(args, tabId, "MAIN");
                break;

            case INVOCATION_METHOD.EVAL_IN_ISOLATED_WORLD:
                result = await handleEvalAsync(args, tabId, "ISOLATED");
                break;

            case INVOCATION_METHOD.SET_NETWORK_REDIRECT:
                result = await runDnrSerialAsync(() => handleSetNetworkRedirectAsync(args));
                break;

            case INVOCATION_METHOD.UNSET_NETWORK_REDIRECT:
                result = await runDnrSerialAsync(() => handleUnsetNetworkRedirectAsync(args));
                break;

            default:
                await sendInvocationResponse(
                    tabId,
                    invocationId,
                    false,
                    null,
                    `unknown method: ${method ?? "(none)"}`,
                );
                return;
        }

        await sendInvocationResponse(tabId, invocationId, true, result, null);
    } catch (error) {
        await sendInvocationResponse(
            tabId,
            invocationId,
            false,
            null,
            error instanceof Error ? error.message : String(error),
        );
    }
}

/**
 * Entry point for `chrome.runtime.onMessage` in bootstrap.
 * Fire-and-forget — never returns a value to the caller.
 *
 * @param {import("./types.js").W7BridgeEnvelope & Record<string, unknown>} message
 * @param {chrome.runtime.MessageSender} sender
 */
export function handleServiceWorkerMessage(message, sender) {
    if (!message?.isW7BridgeMessage || message.target !== TARGET_ID.SERVICE_WORKER) {
        return;
    }

    switch (message.type) {
        case MESSAGE_TYPE.INVOCATION_REQUEST:
            void handleInvocationRequestAsync(
                /** @type {import("./types.js").InvocationRequestMessage} */ (message),
                sender,
            );
            break;
    }
}

// ── Bootstrap installers ──────────────────────────────────────────────────────

/** Mount the isolated relay before runtime injection. */
export async function installIsolatedBridgeAsync(tabId) {
    await chrome.scripting.executeScript({
        target: { tabId },
        world: "ISOLATED",
        func: installIsolatedWorldRelay,
        args: [
            ISOLATED_RELAY_MOUNTED,
            TARGET_ID.SERVICE_WORKER,
            TARGET_ID.MAIN_WORLD,
            TARGET_ID.ISOLATED_WORLD,
            MESSAGE_TYPE.RELAY_ERROR,
        ],
    });
}

/**
 * Inject the runtime bundle into MAIN (bootstrap only — not the invocation API).
 *
 * @param {number} tabId
 * @param {string} runtimeSource - Full IIFE bundle text from Nexus.
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
