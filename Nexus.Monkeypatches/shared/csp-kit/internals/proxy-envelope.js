export const PROXY_ENVELOPE_ID = "csp-kit-proxy-envelope";
export const PROXY_ENVELOPE_VERSION = 1;

/**
 * @typedef {Object} ProxyEnvelope
 * @property {number} v
 * @property {number} status
 * @property {string} contentType
 * @property {string} body
 * @property {string} [error]
 */

/**
 * @typedef {Object} DecodedProxyEnvelope
 * @property {number} status
 * @property {string} contentType
 * @property {string} body
 * @property {string|null} error
 */

/**
 * @param {{ status: number, contentType: string, body: string, error?: string|null }} params
 * @returns {string}
 */
export function buildProxyEnvelopeHtml({ status, contentType, body, error = null }) {
    /** @type {ProxyEnvelope} */
    const envelope = {
        v: PROXY_ENVELOPE_VERSION,
        status,
        contentType,
        body: encodeBase64Utf8(body),
    };

    if (error) {
        envelope.error = error;
    }

    return `<div id="${PROXY_ENVELOPE_ID}">${JSON.stringify(envelope)}</div>`;
}

/**
 * @param {string} html
 * @returns {DecodedProxyEnvelope}
 */
export function decodeProxyEnvelope(html) {
    const doc = new DOMParser().parseFromString(html, "text/html");
    const node = doc.getElementById(PROXY_ENVELOPE_ID);

    if (!node) {
        throw new Error(`Missing #${PROXY_ENVELOPE_ID}`);
    }

    /** @type {ProxyEnvelope} */
    const envelope = JSON.parse(node.textContent.trim());

    if (envelope.v !== PROXY_ENVELOPE_VERSION) {
        throw new Error(`Unsupported proxy envelope version: ${envelope.v}`);
    }

    return {
        status: envelope.status,
        contentType: envelope.contentType,
        body: decodeBase64Utf8(envelope.body),
        error: envelope.error ?? null,
    };
}

/**
 * @param {string} value
 * @returns {string}
 */
function encodeBase64Utf8(value) {
    const bytes = new TextEncoder().encode(value);
    let binary = "";

    for (const byte of bytes) {
        binary += String.fromCharCode(byte);
    }

    return btoa(binary);
}

/**
 * @param {string} value
 * @returns {string}
 */
function decodeBase64Utf8(value) {
    const bytes = Uint8Array.from(atob(value), (char) => char.charCodeAt(0));
    return new TextDecoder().decode(bytes);
}
