/**
 * CSP-KIT Proxy — Cloudflare Worker
 *
 * Spec: proxy/ENVELOPE.md
 * Resposta: <div id="csp-kit-proxy-envelope">{json}</div>
 */

const ENVELOPE_ID = "csp-kit-proxy-envelope";
const ENVELOPE_VERSION = 1;
const ALLOWED_METHODS = new Set(["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"]);

export default {
    /**
     * @param {Request} request
     * @param {Env} env
     * @returns {Promise<Response>}
     */
    async fetch(request, env) {
        if (request.method === "OPTIONS") {
            return envelopeResponse(new Response(null, { status: 204 }));
        }

        const url = new URL(request.url);

        if (url.searchParams.get("path") === "/___internal__/ping") {
            return envelopeResponse(writeEnvelope(200, "text/plain", "PONG!"));
        }

        const targetUrl = url.searchParams.get("url");
        if (!targetUrl) {
            return envelopeResponse(writeError(400, "Missing url parameter"));
        }

        if (!isAllowedTarget(targetUrl, env.ALLOWED_HOSTS)) {
            return envelopeResponse(writeError(403, `Host not allowed: ${new URL(targetUrl).hostname}`));
        }

        const method = (url.searchParams.get("method") ?? request.method).toUpperCase();
        if (!ALLOWED_METHODS.has(method)) {
            return envelopeResponse(writeError(405, `Method not allowed: ${method}`));
        }

        const upstreamRequest = buildUpstreamRequest(request, targetUrl, method);
        const upstreamResponse = await fetch(upstreamRequest);
        const body = await upstreamResponse.text();
        const contentType = upstreamResponse.headers.get("Content-Type") ?? "text/plain";
        const error = upstreamResponse.status >= 400 ? "Upstream request failed" : null;

        return envelopeResponse(writeEnvelope(upstreamResponse.status, contentType, body, error));
    },
};

/**
 * @param {number} status
 * @param {string} contentType
 * @param {string} body
 * @param {string|null} [error]
 * @returns {Response}
 */
function writeEnvelope(status, contentType, body, error = null) {
    const envelope = {
        v: ENVELOPE_VERSION,
        status,
        contentType,
        body: encodeBase64Utf8(body),
    };

    if (error) {
        envelope.error = error;
    }

    const html = `<div id="${ENVELOPE_ID}">${JSON.stringify(envelope)}</div>`;

    return new Response(html, {
        status: 200,
        headers: { "Content-Type": "text/html; charset=utf-8" },
    });
}

/**
 * @param {number} status
 * @param {string} message
 * @returns {Response}
 */
function writeError(status, message) {
    return writeEnvelope(
        status,
        "application/json",
        JSON.stringify({ error: message, status }),
        message,
    );
}

/**
 * @param {Response} response
 * @returns {Response}
 */
function envelopeResponse(response) {
    const headers = new Headers(response.headers);
    headers.set("Access-Control-Allow-Origin", "*");
    headers.set("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS");
    headers.set("Access-Control-Allow-Headers", "Content-Type, Authorization, Accept");

    if (response.status !== 204) {
        headers.set("Content-Type", "text/html; charset=utf-8");
    }

    return new Response(response.body, {
        status: response.status,
        statusText: response.statusText,
        headers,
    });
}

/**
 * @param {Request} request
 * @param {string} targetUrl
 * @param {string} method
 * @returns {Request}
 */
function buildUpstreamRequest(request, targetUrl, method) {
    const headers = new Headers(request.headers);
    headers.delete("Host");
    headers.delete("Origin");
    headers.delete("Referer");

    const init = {
        method,
        headers,
        redirect: "follow",
    };

    if (method !== "GET" && method !== "HEAD") {
        init.body = request.body;
    }

    return new Request(targetUrl, init);
}

/**
 * @param {string} targetUrl
 * @param {string|undefined} allowlistRaw
 * @returns {boolean}
 */
function isAllowedTarget(targetUrl, allowlistRaw) {
    let parsed;

    try {
        parsed = new URL(targetUrl);
    } catch {
        return false;
    }

    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
        return false;
    }

    if (!allowlistRaw) {
        return true;
    }

    const host = parsed.hostname.toLowerCase();
    const allowlist = allowlistRaw.split(",").map((entry) => entry.trim().toLowerCase()).filter(Boolean);

    return allowlist.some((entry) => host === entry || host.endsWith(`.${entry}`));
}

/**
 * @param {string} value
 * @returns {string}
 */
function encodeBase64Utf8(value) {
    return btoa(String.fromCharCode(...new TextEncoder().encode(value)));
}

/** @typedef {{ ALLOWED_HOSTS?: string }} Env */
