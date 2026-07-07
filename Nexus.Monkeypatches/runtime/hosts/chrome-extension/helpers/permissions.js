/**
 * Bridge invocation permission discovery (service worker context).
 *
 * Uses chrome.permissions.contains — does not request optional permissions.
 */

import { INVOCATION_METHOD } from "../bridge/bridge-core.js";

const ALL_URLS_PATTERN = "<all_urls>";

/** @typedef {{ permissions: string[], origins: string[] }} PermissionRequirements */

/** @type {Record<string, PermissionRequirements>} */
const STATIC_REQUIREMENTS = {
    [INVOCATION_METHOD.FETCH]: {
        permissions: [],
        origins: [],
    },
    [INVOCATION_METHOD.EVAL_IN_MAIN_WORLD]: {
        permissions: ["scripting"],
        origins: [],
    },
    [INVOCATION_METHOD.EVAL_IN_ISOLATED_WORLD]: {
        permissions: ["scripting"],
        origins: [],
    },
    [INVOCATION_METHOD.SET_NETWORK_REDIRECT]: {
        permissions: ["declarativeNetRequest"],
        origins: [],
    },
    [INVOCATION_METHOD.UNSET_NETWORK_REDIRECT]: {
        permissions: ["declarativeNetRequest"],
        origins: [],
    },
    [INVOCATION_METHOD.START_NETWORK_OBSERVE]: {
        permissions: ["webRequest", "extraHeaders"],
        origins: [ALL_URLS_PATTERN],
    },
    [INVOCATION_METHOD.STOP_NETWORK_OBSERVE]: {
        permissions: ["webRequest"],
        origins: [],
    },
};

/**
 * @param {string} url
 * @returns {string}
 */
export function originPatternFromUrl(url) {
    return `${new URL(url).origin}/*`;
}

/**
 * @param {number | undefined} tabId
 * @returns {Promise<string>}
 */
async function tabOriginPatternAsync(tabId) {
    if (tabId == null) {
        throw new Error("eval requires tab id");
    }

    const tab = await chrome.tabs.get(tabId);

    if (typeof tab.url !== "string" || !tab.url.startsWith("http")) {
        throw new Error("eval requires an http(s) tab url");
    }

    return originPatternFromUrl(tab.url);
}

/**
 * @param {string} method
 * @returns {PermissionRequirements | null}
 */
export function getInvocationRequirements(method) {
    const requirements = STATIC_REQUIREMENTS[method];

    if (requirements == null) {
        return null;
    }

    return {
        permissions: [...requirements.permissions],
        origins: [...requirements.origins],
    };
}

/**
 * @param {string} method
 * @param {unknown} args
 * @param {chrome.runtime.MessageSender} sender
 * @returns {Promise<PermissionRequirements | null>}
 */
export async function resolveInvocationRequirements(method, args, sender) {
    const base = getInvocationRequirements(method);

    if (base == null) {
        return null;
    }

    const requirements = {
        permissions: [...base.permissions],
        origins: [...base.origins],
    };

    switch (method) {
        case INVOCATION_METHOD.FETCH: {
            if (typeof args?.url !== "string") {
                throw new Error("fetch requires args.url");
            }

            requirements.origins.push(originPatternFromUrl(args.url));
            break;
        }

        case INVOCATION_METHOD.EVAL_IN_MAIN_WORLD:
        case INVOCATION_METHOD.EVAL_IN_ISOLATED_WORLD: {
            requirements.origins.push(await tabOriginPatternAsync(sender.tab?.id));
            break;
        }
    }

    return requirements;
}

/**
 * @param {PermissionRequirements} requirements
 * @returns {Promise<string[]>}
 */
async function getMissingPermissions(requirements) {
    /** @type {string[]} */
    const missing = [];

    for (const permission of requirements.permissions) {
        const granted = await chrome.permissions.contains({ permissions: [permission] });

        if (!granted) {
            missing.push(permission);
        }
    }

    for (const origin of requirements.origins) {
        const granted = await chrome.permissions.contains({ origins: [origin] });

        if (!granted) {
            missing.push(origin);
        }
    }

    return missing;
}

/**
 * @param {string} method
 * @param {unknown} args
 * @param {chrome.runtime.MessageSender} sender
 * @returns {Promise<{ granted: boolean, missing: string[] }>}
 */
export async function checkInvocationPermissions(method, args, sender) {
    const requirements = await resolveInvocationRequirements(method, args, sender);

    if (requirements == null) {
        return { granted: true, missing: [] };
    }

    const missing = await getMissingPermissions(requirements);

    return {
        granted: missing.length === 0,
        missing,
    };
}

/**
 * @param {string} method
 * @param {unknown} args
 * @param {chrome.runtime.MessageSender} sender
 */
export async function assertInvocationPermissions(method, args, sender) {
    const { granted, missing } = await checkInvocationPermissions(method, args, sender);

    if (!granted) {
        throw new Error(`missing permission: ${missing.join(", ")}`);
    }
}

/**
 * @param {unknown} [args]
 * @param {chrome.runtime.MessageSender} [sender]
 * @returns {Promise<Record<string, { granted: boolean, missing: string[] }>>}
 */
export async function getBridgeCapabilitiesAsync(args, sender) {
    /** @type {Record<string, { granted: boolean, missing: string[] }>} */
    const capabilities = {};

    for (const method of Object.values(INVOCATION_METHOD)) {
        try {
            capabilities[method] = await checkInvocationPermissions(method, args, sender ?? {});
        } catch {
            const staticRequirements = getInvocationRequirements(method);

            if (staticRequirements == null) {
                capabilities[method] = { granted: true, missing: [] };
                continue;
            }

            const missing = await getMissingPermissions(staticRequirements);
            capabilities[method] = {
                granted: missing.length === 0,
                missing,
            };
        }
    }

    return capabilities;
}
