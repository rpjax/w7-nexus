import { SCRIPT_INJECTION_METHOD } from "../env.js";
import { getHijackableNonce } from "./nonce.js";
import { probeInlineScript } from "./probes.js";
import {
    isBlobScriptAllowedAsync,
    isDataScriptAllowedAsync,
    isUnsafeEvalEnabled,
    isUnsafeInlineEnabled,
} from "./policy.js";

/**
 * @param {import("../env.js").ScriptInjectionMethod} method
 * @returns {Promise<boolean>}
 */
async function isInjectionMethodAvailableAsync(method) {
    switch (method) {
        case SCRIPT_INJECTION_METHOD.NONCE: {
            const nonce = getHijackableNonce();
            return nonce != null && probeInlineScript({ nonce });
        }
        case SCRIPT_INJECTION_METHOD.EVAL:
            return isUnsafeEvalEnabled();
        case SCRIPT_INJECTION_METHOD.INLINE:
            return isUnsafeInlineEnabled();
        case SCRIPT_INJECTION_METHOD.BLOB:
            return isBlobScriptAllowedAsync();
        case SCRIPT_INJECTION_METHOD.DATA:
            return isDataScriptAllowedAsync();
        default:
            return false;
    }
}

/**
 * @returns {Promise<import("../env.js").ScriptInjectionMethod[]>}
 */
export async function listAvailableScriptInjectionMethodsAsync() {
    /** @type {import("../env.js").ScriptInjectionMethod[]} */
    const available = [];

    for (const method of Object.values(SCRIPT_INJECTION_METHOD)) {
        if (await isInjectionMethodAvailableAsync(method)) {
            available.push(method);
        }
    }

    return available;
}
