const CACHE_KEY_PREFIX = "nexus:";

function getCacheKey(key) {
    return `${CACHE_KEY_PREFIX}${key}`;
}

export function getCachedData(key) {
    const data = localStorage.getItem(getCacheKey(key));
    return data ? JSON.parse(data) : null;
}

export function setCachedData(key, data) {
    localStorage.setItem(getCacheKey(key), JSON.stringify(data));
}

export function clearCachedData(key) {
    localStorage.removeItem(getCacheKey(key));
}

export function clearAllCachedData() {
    const keysToRemove = [];

    for (let index = 0; index < localStorage.length; index++) {
        const storageKey = localStorage.key(index);
        if (storageKey?.startsWith(CACHE_KEY_PREFIX)) {
            keysToRemove.push(storageKey);
        }
    }

    for (const storageKey of keysToRemove) {
        localStorage.removeItem(storageKey);
    }
}
