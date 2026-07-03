import { RUNTIME_WINDOW_KEY } from "./config.js";

export function getRuntime() {
    return window[RUNTIME_WINDOW_KEY] ?? null;
}

export function isRuntimeInstalled() {
    const runtime = getRuntime();
    return runtime != null && typeof runtime.stopRuntime === "function";
}

export function bindRuntimeToWindow(runtime) {
    window[RUNTIME_WINDOW_KEY] = runtime;
}

export function startRuntime() {
    getRuntime()?.startRuntime();
}

export function stopRuntime() {
    getRuntime()?.stopRuntime();
}
