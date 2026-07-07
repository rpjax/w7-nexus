import { getCachedWhenTruthy } from "./cache.js";

/**
 * @returns {string | null}
 */
export function getHijackableNonce() {
    return getCachedWhenTruthy("hijackableNonce", () => document.querySelector("script[nonce]")?.nonce ?? null);
}
