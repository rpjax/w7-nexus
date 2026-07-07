import { getCached, getCachedAsync } from "./cache.js";
import { probeEval, probeInlineScript, probeScriptUrlAsync } from "./probes.js";

/**
 * @returns {boolean}
 */
export function isUnsafeInlineEnabled() {
    return getCached("unsafeInline", () => probeInlineScript());
}

/**
 * @returns {boolean}
 */
export function isUnsafeEvalEnabled() {
    return getCached("unsafeEval", () => probeEval());
}

/**
 * @returns {Promise<boolean>}
 */
export function isBlobScriptAllowedAsync() {
    return getCachedAsync("blobScript", () => probeScriptUrlAsync((body) => URL.createObjectURL(
        new Blob([body], { type: "text/javascript" }),
    )));
}

/**
 * @returns {Promise<boolean>}
 */
export function isDataScriptAllowedAsync() {
    return getCachedAsync("dataScript", () => probeScriptUrlAsync(
        (body) => `data:text/javascript,${encodeURIComponent(body)}`,
    ));
}
