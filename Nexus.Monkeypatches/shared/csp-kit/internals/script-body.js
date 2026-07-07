export function createScriptMarker() {
    return `__w7_csp_${crypto.randomUUID()}`;
}

/**
 * @param {string} body
 * @param {{ nonce?: string }} [options]
 */
function appendInlineScriptSync(body, options = {}) {
    const script = document.createElement("script");

    if (options.nonce) {
        script.nonce = options.nonce;
    }

    script.textContent = body;
    document.head.appendChild(script);
    script.remove();
}

/**
 * @param {string} [bodyExtra]
 * @param {{ nonce?: string }} [options]
 * @returns {boolean}
 */
export function runInlineWithMarkerSync(bodyExtra = "", options = {}) {
    const marker = createScriptMarker();
    window[marker] = false;

    const body = `window[${JSON.stringify(marker)}]=true${bodyExtra ? `;${bodyExtra}` : ""}`;
    appendInlineScriptSync(body, options);

    const executed = window[marker] === true;
    delete window[marker];

    return executed;
}

/**
 * @returns {boolean}
 */
export function runEvalWithMarkerSync() {
    const marker = createScriptMarker();
    window[marker] = false;

    try {
        // eslint-disable-next-line no-eval
        (0, eval)(`window[${JSON.stringify(marker)}]=true`);
        return window[marker] === true;
    } catch {
        return false;
    } finally {
        delete window[marker];
    }
}

/**
 * @param {string} body
 * @returns {boolean}
 */
export function runEvalBodySync(body) {
    try {
        // eslint-disable-next-line no-eval
        (0, eval)(body);
        return true;
    } catch {
        return false;
    }
}

/**
 * @param {string} [bodyExtra]
 * @param {(body: string) => string} buildUrl
 * @returns {Promise<boolean>}
 */
export function runScriptBodyViaUrlAsync(bodyExtra, buildUrl) {
    const marker = createScriptMarker();
    window[marker] = false;

    const body = `window[${JSON.stringify(marker)}]=true${bodyExtra ? `;${bodyExtra}` : ""}`;
    const url = buildUrl(body);

    return new Promise((resolve) => {
        const script = document.createElement("script");
        script.src = url;

        const finish = (executed) => {
            if (url.startsWith("blob:")) {
                URL.revokeObjectURL(url);
            }

            delete window[marker];
            script.remove();
            resolve(executed);
        };

        script.onload = () => finish(window[marker] === true);
        script.onerror = () => finish(false);
        document.head.appendChild(script);
    });
}
