import { getCacheJson, onCacheChange, removeCache, setCacheJson } from "./cache.js";

const STATE_STORAGE_PREFIX = "w7runtime:state:";

const W7_TRACKED = Symbol("w7_tracked");
const states = new Map();

/** @param {string} key Caller key, e.g. `"my-data"`. */
function toStorageKey(key) {
    return `${STATE_STORAGE_PREFIX}${key}`;
}

function isTrackable(value) {
    return value !== null && typeof value === "object";
}

function createStateTracker(target, persist) {
    if (!isTrackable(target) || target[W7_TRACKED]) {
        return target;
    }

    for (const key of Reflect.ownKeys(target)) {
        const value = target[key];
        if (isTrackable(value) && !value[W7_TRACKED]) {
            target[key] = createStateTracker(value, persist);
        }
    }

    const tracked = new Proxy(target, {
        get(obj, prop, receiver) {
            const value = Reflect.get(obj, prop, receiver);
            if (isTrackable(value) && !value[W7_TRACKED]) {
                const nested = createStateTracker(value, persist);
                Reflect.set(obj, prop, nested);
                return nested;
            }

            return value;
        },
        set(obj, prop, value) {
            const wrapped = isTrackable(value) ? createStateTracker(value, persist) : value;
            const ok = Reflect.set(obj, prop, wrapped);
            persist();
            return ok;
        },
        deleteProperty(obj, prop) {
            const ok = Reflect.deleteProperty(obj, prop);
            persist();
            return ok;
        },
    });

    Object.defineProperty(tracked, W7_TRACKED, { value: true });
    return tracked;
}

export function createState(key, factory) {
    const existing = states.get(key);
    if (existing != null) {
        return existing;
    }

    const storageKey = toStorageKey(key);
    const target = factory();

    const cached = getCacheJson(storageKey);
    if (cached != null) {
        Object.assign(target, cached);
    }

    function sync() {
        const data = getCacheJson(storageKey);
        if (data == null) {
            return;
        }

        Object.assign(target, data);
    }

    function persist() {
        setCacheJson(storageKey, target);
    }

    const tracked = createStateTracker(target, persist);
    onCacheChange(storageKey, sync);

    const state = new Proxy(tracked, {
        get(obj, prop, receiver) {
            sync();
            return Reflect.get(obj, prop, receiver);
        },
    });

    states.set(key, state);
    return state;
}

export function getState(key) {
    const state = states.get(key);
    if (state == null) {
        throw new Error(`[state] "${key}" was not created — call createState first`);
    }

    return state;
}

export function deleteState(key) {
    states.delete(key);
    removeCache(toStorageKey(key));
}