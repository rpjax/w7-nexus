import { startNetworkObserveAsync, stopNetworkObserveAsync } from "../../hosts/chrome-extension/bridge/main-world.js";

/** @type {Set<string>} */
const activeWatches = new Set();

export async function watchExtension(extensionId) {
    if (typeof extensionId !== "string" || extensionId.length === 0) {
        throw new Error("watchExtension requires extensionId");
    }

    const result = await startNetworkObserveAsync({ extensionId });
    activeWatches.add(result.extensionId);
}

export async function unwatchExtension(extensionId) {
    if (typeof extensionId !== "string" || extensionId.length === 0) {
        throw new Error("unwatchExtension requires extensionId");
    }

    const normalizedExtensionId = extensionId.trim().toLowerCase();

    if (!activeWatches.has(normalizedExtensionId)) {
        return;
    }

    await stopNetworkObserveAsync({ extensionId: normalizedExtensionId });
    activeWatches.delete(normalizedExtensionId);
}

export async function unwatchAllExtensionsAsync() {
    const extensionIds = [...activeWatches];

    for (const extensionId of extensionIds) {
        await stopNetworkObserveAsync({ extensionId });
        activeWatches.delete(extensionId);
    }
}

/** @returns {string[]} */
export function getActiveWatches() {
    return [...activeWatches];
}
