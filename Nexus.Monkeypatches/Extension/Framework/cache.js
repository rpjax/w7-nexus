const CACHE_PREFIX = "W7_MP:";

function resolveKey(key) {
    return `${CACHE_PREFIX}${key}`;
}

export function getCache(key) {
    return localStorage.getItem(resolveKey(key));
}

export function setCache(key, value) {
    localStorage.setItem(resolveKey(key), value);
}

export function removeCache(key) {
    localStorage.removeItem(resolveKey(key));
}

export function getCacheJson(key) {
    const raw = getCache(key);
    if (raw == null) {
        return null;
    }

    try {
        return JSON.parse(raw);
    } catch {
        removeCache(key);
        return null;
    }
}

export function setCacheJson(key, value) {
    setCache(key, JSON.stringify(value));
}

export function onCacheChange(key, listener) {
    if (typeof window === "undefined") {
        return () => {};
    }

    const storageKey = resolveKey(key);
    const handler = (event) => {
        if (event.key === storageKey) {
            listener(event.newValue);
        }
    };

    window.addEventListener("storage", handler);
    return () => window.removeEventListener("storage", handler);
}
