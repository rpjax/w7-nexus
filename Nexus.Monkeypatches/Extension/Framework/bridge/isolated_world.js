/**
 * ISOLATED world — stateless event relay between MAIN and the service worker.
 *
 * Injected via `chrome.scripting.executeScript` (bootstrap). Must stay self-contained:
 * no module imports, no closures over SW constants — protocol values are passed as args.
 *
 * ```
 * MAIN ──postMessage(target: SW)──► ISOLATED ──runtime.sendMessage──► SW
 * SW   ──tabs.sendMessage(target: MAIN)──► ISOLATED ──postMessage──► MAIN
 * ```
 *
 * The relay never reads the return value of `sendMessage`. Failures surface as
 * `RELAY_ERROR` events with the original `sourceMessage` attached.
 *
 * @param {string} mountedKey - `window` guard key (`ISOLATED_RELAY_MOUNTED`).
 * @param {string} targetServiceWorker - `TARGET_ID.SERVICE_WORKER`.
 * @param {string} targetMainWorld - `TARGET_ID.MAIN_WORLD`.
 * @param {string} targetIsolatedWorld - `TARGET_ID.ISOLATED_WORLD`.
 * @param {string} relayErrorType - `MESSAGE_TYPE.RELAY_ERROR`.
 */
export function installIsolatedWorldRelay(
    mountedKey,
    targetServiceWorker,
    targetMainWorld,
    targetIsolatedWorld,
    relayErrorType,
) {
    if (window[mountedKey]) {
        return;
    }

    window[mountedKey] = true;

    // MAIN → SW (fire-and-forget)
    window.addEventListener("message", (event) => {
        if (event.source !== window) {
            return;
        }

        const message = event.data;

        if (!message?.isW7BridgeMessage || message.target !== targetServiceWorker) {
            return;
        }

        void chrome.runtime.sendMessage(message)
            .catch((error) => {
                window.postMessage({
                    isW7BridgeMessage: true,
                    type: relayErrorType,
                    source: targetIsolatedWorld,
                    target: message.source ?? targetMainWorld,
                    sourceMessage: message,
                    error: error instanceof Error ? error.message : String(error),
                });
            });
    });

    // SW → MAIN
    chrome.runtime.onMessage.addListener((message) => {
        if (!message?.isW7BridgeMessage || message.target !== targetMainWorld) {
            return;
        }

        window.postMessage(message);
    });
}
