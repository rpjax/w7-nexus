// Bundle entry for csp-kit.min.js — installs window.cspKit when injected via chrome.scripting files.
// Monorepo consumers import ./fetcher.js and ./script-injector.js directly.

import {
    CSP_KIT,
    CSP_KIT_API,
    FETCHER_API,
    SCRIPT_INJECTOR_API,
} from "./env.js";
import { fetchAsync, isFetchAvailableAsync } from "./fetcher.js";
import {
    injectScriptAsync,
    isScriptInjectionAvailableAsync,
    listScriptInjectionMethodsAsync,
} from "./script-injector.js";

function buildFetcherApi() {
    return {
        [FETCHER_API.IS_FETCH_AVAILABLE_ASYNC]: isFetchAvailableAsync,
        [FETCHER_API.FETCH_ASYNC]: fetchAsync,
    };
}

function buildScriptInjectorApi() {
    return {
        [SCRIPT_INJECTOR_API.IS_SCRIPT_INJECTION_AVAILABLE_ASYNC]: isScriptInjectionAvailableAsync,
        [SCRIPT_INJECTOR_API.INJECT_SCRIPT_ASYNC]: injectScriptAsync,
        [SCRIPT_INJECTOR_API.LIST_SCRIPT_INJECTION_METHODS_ASYNC]: listScriptInjectionMethodsAsync,
    };
}

export function buildCspKitApi() {
    return {
        [CSP_KIT_API.VERSION]: CSP_KIT.VERSION,
        [CSP_KIT_API.FETCHER]: buildFetcherApi(),
        [CSP_KIT_API.SCRIPT_INJECTOR]: buildScriptInjectorApi(),
    };
}

try {
    window[CSP_KIT.WINDOW_NAME] = buildCspKitApi();
} catch (error) {
    console.error("Failed to inject CSP kit into window", error);
}
