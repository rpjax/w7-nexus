import { isFetchToUrlAllowedAsync } from "./internals/fetch-probes.js";

/**
 * @param {{ url?: string }} [params]
 * @returns {Promise<boolean>}
 */
export async function isFetchAvailableAsync({ url } = {}) {
    const probeUrl = url ?? `${location.origin}/`;
    return isFetchToUrlAllowedAsync(probeUrl);
}

/**
 * @param {{ url: string }} params
 * @returns {Promise<Response>}
 */
export async function fetchAsync({ url }) {
    if (!await isFetchToUrlAllowedAsync(url)) {
        throw new Error(`fetch not allowed by CSP: ${url}`);
    }

    return fetch(url);
}
