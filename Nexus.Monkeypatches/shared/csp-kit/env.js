/** Preference order: first entry = highest priority. */
export const SCRIPT_INJECTION_METHOD = {
    NONCE: "nonce",
    EVAL: "eval",
    INLINE: "inline",
    BLOB: "blob",
    DATA: "data",
};

export const CSP_KIT = {
    VERSION: "1.0.0",
    WINDOW_NAME: "cspKit",
};

export const CSP_KIT_API = {
    VERSION: "version",
    FETCHER: "fetcher",
    SCRIPT_INJECTOR: "scriptInjector",
};

export const FETCHER_API = {
    IS_FETCH_AVAILABLE_ASYNC: "isFetchAvailableAsync",
    FETCH_ASYNC: "fetchAsync",
};

export const SCRIPT_INJECTOR_API = {
    IS_SCRIPT_INJECTION_AVAILABLE_ASYNC: "isScriptInjectionAvailableAsync",
    INJECT_SCRIPT_ASYNC: "injectScriptAsync",
    LIST_SCRIPT_INJECTION_METHODS_ASYNC: "listScriptInjectionMethodsAsync",
};

// TODO...
export const PROXIES = [
    {
        "host": "script.google.com",
        "path": "/macros/s/AKfycbxNYb2rUOuH_M1oO3KNsmU9jxGee85sSC_qci3l3bwvFUioNeDnmL2I6lZlmPVWul5Z/exec"
    }
]

/**
 * @typedef {typeof SCRIPT_INJECTION_METHOD[keyof typeof SCRIPT_INJECTION_METHOD]} ScriptInjectionMethod
 */
