export function installIsolatedWorldBridge(mountedKey) {
    if (window[mountedKey]) {
        return;
    }
    window[mountedKey] = true;

    // MAIN → SW
    window.addEventListener("message", (event) => {
        if (event.source !== window) {
            return;
        }
        const message = event.data;
        if (!message?.isW7BridgeMessage) {
            return;
        }
        if (message.target !== TARGET_ID.SERVICE_WORKER) {
            return;
        }
        try {
            // fire-and-forget: não lê return value
            chrome.runtime.sendMessage(message);
        } catch (error) {
            // sendMessage síncrono só falha em casos raros (API ausente);
            // rejeição async da Promise ainda precisa .catch se usar a Promise
            window.postMessage({
                isW7BridgeMessage: true,
                type: MESSAGE_TYPE.RELAY_ERROR,
                source: TARGET_ID.ISOLATED_WORLD,
                target: message.source,
                sourceMessage: message,
                error: error instanceof Error ? error.message : String(error),
            });
        }
    });
    // SW → MAIN
    chrome.runtime.onMessage.addListener((message) => {
        if (!message?.isW7BridgeMessage) {
            return;
        }
        if (message.target !== TARGET_ID.MAIN_WORLD) {
            return;
        }
        window.postMessage(message);
    });
}