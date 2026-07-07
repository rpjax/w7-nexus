import { getCachedAsync } from "./cache.js";

const FETCH_PROBE_TIMEOUT_MS = 250;

/**
 * @param {string} url
 * @returns {Promise<boolean>}
 */
export function isFetchToUrlAllowedAsync(url) {
    return getCachedAsync(`fetch:${url}`, () => new Promise((resolve) => {
        let settled = false;

        /**
         * @param {boolean} allowed
         */
        const settle = (allowed) => {
            if (settled) {
                return;
            }

            settled = true;
            document.removeEventListener("securitypolicyviolation", onViolation);
            clearTimeout(timeoutId);
            resolve(allowed);
        };

        /** @param {SecurityPolicyViolationEvent} event */
        const onViolation = (event) => {
            if (event.effectiveDirective === "connect-src") {
                settle(false);
            }
        };

        document.addEventListener("securitypolicyviolation", onViolation);

        const timeoutId = setTimeout(() => settle(true), FETCH_PROBE_TIMEOUT_MS);

        void fetch(url, { method: "HEAD", cache: "no-store" })
            .then(() => settle(true))
            .catch(() => settle(false));
    }));
}
