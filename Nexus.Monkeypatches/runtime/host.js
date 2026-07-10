export const INSTALLATION_HOST_KEY = "__runtime_installation_host";

export const INSTALLATION_HOST = {
    XSS: "XSS",
    CHROME_EXTENSION: "CHROME-EXTENSION",
    W7_ENGINE: "W7-ENGINE",
    W7_SPECULUM: "W7-SPECULUM",
    MITM_INJECTION: "MITM-INJECTION",
};

/**
 * @returns {string}
 */
export function getInstallationHost() {
    const host = window[INSTALLATION_HOST_KEY];

    if (typeof host !== "string" || host.length === 0) {
        throw new Error(`missing ${INSTALLATION_HOST_KEY} — delivery must set installation host before runtime init`);
    }

    return host;
}

/**
 * @param {string} host
 */
export function setInstallationHost(host) {
    window[INSTALLATION_HOST_KEY] = host;
}
