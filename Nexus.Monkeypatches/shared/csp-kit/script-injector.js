import { SCRIPT_INJECTION_METHOD } from "./env.js";
import { listAvailableScriptInjectionMethodsAsync } from "./internals/injection-methods.js";
import { getHijackableNonce } from "./internals/nonce.js";
import {
    runEvalBodySync,
    runInlineWithMarkerSync,
    runScriptBodyViaUrlAsync,
} from "./internals/script-body.js";

/**
 * @param {string} source
 * @param {import("./env.js").ScriptInjectionMethod} method
 * @returns {boolean | Promise<boolean>}
 */
function injectViaMethod(source, method) {
    switch (method) {
        case SCRIPT_INJECTION_METHOD.NONCE: {
            const nonce = getHijackableNonce();
            return nonce != null && runInlineWithMarkerSync(source, { nonce });
        }
        case SCRIPT_INJECTION_METHOD.EVAL:
            return runEvalBodySync(source);
        case SCRIPT_INJECTION_METHOD.INLINE:
            return runInlineWithMarkerSync(source);
        case SCRIPT_INJECTION_METHOD.BLOB:
            return runScriptBodyViaUrlAsync(source, (body) => URL.createObjectURL(
                new Blob([body], { type: "text/javascript" }),
            ));
        case SCRIPT_INJECTION_METHOD.DATA:
            return runScriptBodyViaUrlAsync(
                source,
                (body) => `data:text/javascript,${encodeURIComponent(body)}`,
            );
        default:
            return false;
    }
}

/**
 * @returns {Promise<import("./env.js").ScriptInjectionMethod[]>}
 */
export async function listScriptInjectionMethodsAsync() {
    return listAvailableScriptInjectionMethodsAsync();
}

/**
 * @returns {Promise<boolean>}
 */
export async function isScriptInjectionAvailableAsync() {
    return (await listScriptInjectionMethodsAsync()).length > 0;
}

/**
 * @param {{ source: string }} params
 * @returns {Promise<import("./env.js").ScriptInjectionMethod>}
 */
export async function injectScriptAsync({ source }) {
    const methods = await listScriptInjectionMethodsAsync();

    if (methods.length === 0) {
        throw new Error("no script injection methods available");
    }

    for (const method of methods) {
        const result = injectViaMethod(source, method);
        const executed = result instanceof Promise ? await result : result;

        if (executed) {
            return method;
        }
    }

    throw new Error("all available script injection methods failed");
}
