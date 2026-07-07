/** Headers required for cross-origin fetch (ngrok free returns HTML without these). */
export const REMOTE_FETCH_HEADERS = {
    "ngrok-skip-browser-warning": "1",
};

export async function fetchText(url) {
    const response = await fetch(url, { headers: REMOTE_FETCH_HEADERS });

    if (!response.ok) {
        throw new Error(`Fetch failed (${response.status}): ${url}`);
    }

    return response.text();
}

export async function importModuleFromSource(source) {
    const blobUrl = URL.createObjectURL(new Blob([source], { type: "text/javascript" }));

    try {
        return await import(blobUrl);
    } finally {
        URL.revokeObjectURL(blobUrl);
    }
}

export async function importModule(url) {
    return importModuleFromSource(await fetchText(url));
}
