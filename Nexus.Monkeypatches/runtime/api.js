/**
 * Patch-facing consumption API for the runtime installed on `window` by `initializer.js`.
 *
 * Safe to bundle into site patches — does not import or embed the runtime itself.
 */

import {
    RUNTIME,
    RUNTIME_API,
    STATE_MANAGER_API,
    EXTENSION_WATCHER_API,
} from "./env.js";

// ── Internal helpers ────────────────────────────────────────────────────────

function requireRuntime() {
    const runtime = window[RUNTIME.WINDOW_NAME];

    if (runtime == null) {
        throw new Error(`Runtime is not installed (window.${RUNTIME.WINDOW_NAME})`);
    }

    return runtime;
}

/**
 * @param {string} key
 */
function requireRuntimeProperty(key) {
    const value = requireRuntime()[key];

    if (value == null) {
        throw new Error(`Runtime property "${key}" is not available`);
    }

    return value;
}

/**
 * @param {string} key
 */
function requireRuntimeMethod(key) {
    const method = requireRuntime()[key];

    if (typeof method !== "function") {
        throw new Error(`Runtime method "${key}" is not available`);
    }

    return method;
}

function requireStateManager() {
    return requireRuntimeProperty(RUNTIME_API.STATE_MANAGER);
}

/**
 * @param {string} methodName
 */
function requireStateManagerMethod(methodName) {
    const method = requireStateManager()[methodName];

    if (typeof method !== "function") {
        throw new Error(`State manager method "${methodName}" is not available`);
    }

    return method;
}

function requireExtensionWatcher() {
    return requireRuntimeProperty(RUNTIME_API.EXTENSION_WATCHER);
}

/**
 * @param {string} methodName
 */
function requireExtensionWatcherMethod(methodName) {
    const method = requireExtensionWatcher()[methodName];

    if (typeof method !== "function") {
        throw new Error(`Extension watcher method "${methodName}" is not available`);
    }

    return method;
}

// ── Runtime ─────────────────────────────────────────────────────────────────

/** @returns {boolean} */
export function isRuntimeInstalled() {
    return window[RUNTIME.WINDOW_NAME] != null;
}

export function getRuntime() {
    return requireRuntime();
}

export function getVersion() {
    return requireRuntime()[RUNTIME_API.VERSION];
}

export function getHost() {
    return requireRuntime()[RUNTIME_API.HOST];
}

export function start() {
    return requireRuntimeMethod(RUNTIME_API.START)();
}

export function stop() {
    return requireRuntimeMethod(RUNTIME_API.STOP)();
}

export function getLogger() {
    return requireRuntimeProperty(RUNTIME_API.LOGGER);
}

// ── Features ────────────────────────────────────────────────────────────────

export function getStateManager() {
    return requireStateManager();
}

export function getState(key) {
    return requireStateManagerMethod(STATE_MANAGER_API.GET_STATE)(key);
}

export function createState(key, factory) {
    return requireStateManagerMethod(STATE_MANAGER_API.CREATE_STATE)(key, factory);
}

export function deleteState(key) {
    return requireStateManagerMethod(STATE_MANAGER_API.DELETE_STATE)(key);
}

export function getExtensionWatcher() {
    return requireExtensionWatcher();
}

export function watchExtension(extensionId) {
    return requireExtensionWatcherMethod(EXTENSION_WATCHER_API.WATCH_EXTENSION)(extensionId);
}

export function unwatchExtension(extensionId) {
    return requireExtensionWatcherMethod(EXTENSION_WATCHER_API.UNWATCH_EXTENSION)(extensionId);
}
