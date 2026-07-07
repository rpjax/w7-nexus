import { getCacheJson, onCacheChange, setCacheJson } from "./cache.js";

const STATE_KEY_PREFIX = "state:";

const W7_TRACKED = Symbol("w7_tracked");
const states = new Map();

function toStateKey(cacheKey) {
    return `${STATE_KEY_PREFIX}${cacheKey}`;
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

export function createState(cacheKey, factory) {
    const existing = states.get(cacheKey);
    if (existing != null) {
        return existing;
    }

    const key = toStateKey(cacheKey);
    const target = factory();

    const cached = getCacheJson(key);
    if (cached != null) {
        Object.assign(target, cached);
    }

    function sync() {
        const data = getCacheJson(key);
        if (data == null) {
            return;
        }

        Object.assign(target, data);
    }

    function persist() {
        setCacheJson(key, target);
    }

    const tracked = createStateTracker(target, persist);
    onCacheChange(key, sync);

    const state = new Proxy(tracked, {
        get(obj, prop, receiver) {
            sync();
            return Reflect.get(obj, prop, receiver);
        },
    });

    states.set(cacheKey, state);
    return state;
}

export function getState(cacheKey) {
    const state = states.get(cacheKey);
    if (state == null) {
        throw new Error(`[state] "${cacheKey}" was not created — call createState first`);
    }

    return state;
}