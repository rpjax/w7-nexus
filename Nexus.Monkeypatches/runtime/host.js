export const INSTALLATION_HOST_KEY = "__w7_installation_host";

export const INSTALLATION_HOST = {
    CHROME_EXTENSION: "CHROME_EXTENSION",
    XSS: "XSS",
    MITM_INJECTION: "MITM_INJECTION",
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
