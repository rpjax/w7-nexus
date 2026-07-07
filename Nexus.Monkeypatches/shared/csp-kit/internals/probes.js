import {
    runEvalWithMarkerSync,
    runInlineWithMarkerSync,
    runScriptBodyViaUrlAsync,
} from "./script-body.js";

/**
 * @param {{ nonce?: string }} [options]
 * @returns {boolean}
 */
export function probeInlineScript(options = {}) {
    return runInlineWithMarkerSync("", options);
}

/**
 * @returns {boolean}
 */
export function probeEval() {
    return runEvalWithMarkerSync();
}

/**
 * @param {(body: string) => string} buildUrl
 * @returns {Promise<boolean>}
 */
export function probeScriptUrlAsync(buildUrl) {
    return runScriptBodyViaUrlAsync("", buildUrl);
}
