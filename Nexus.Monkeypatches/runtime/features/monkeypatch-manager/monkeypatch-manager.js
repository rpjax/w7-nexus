import { getInstallationHost, INSTALLATION_HOST } from "../../host.js";
import { startChromeExtensionMonkeypatchAsync } from "./chrome-extension.js";
import { startDefaultMonkeypatchAsync } from "./default.js";

export async function startMonkeyPatchManagerAsync() {
    if (getInstallationHost() === INSTALLATION_HOST.CHROME_EXTENSION) {
        return startChromeExtensionMonkeypatchAsync();
    }

    return startDefaultMonkeypatchAsync();
}

/** Hook — teardown not implemented yet. */
export function stopMonkeyPatchManager() {}
