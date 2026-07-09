import { API_BASE_URL } from "../../env.js";

export const SCRIPTS_ENDPOINT = `${API_BASE_URL}/scripts`;

export async function fetchScriptsAsync({ name, host, channel = "prod", fetchImpl = fetch } = {}) {
    const params = new URLSearchParams();

    if (name) {
        params.set("name", name);
    }

    if (host) {
        params.set("host", host);
    }

    if (channel) {
        params.set("channel", channel);
    }

    const response = await fetchImpl(`${SCRIPTS_ENDPOINT}?${params.toString()}`);

    if (!response.ok) {
        if (response.status < 500) {
            return [];
        }

        throw new Error(`scripts fetch failed (${response.status})`);
    }

    const payload = await response.json();
    return payload.items ?? [];
}

export async function fetchScriptSourceAsync(options) {
    const items = await fetchScriptsAsync(options);
    return items[0]?.sourceCode ?? null;
}
