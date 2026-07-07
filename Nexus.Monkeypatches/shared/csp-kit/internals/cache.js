/** @type {Record<string, unknown>} */
const cache = {};

/**
 * @template T
 * @param {string} key
 * @param {() => T} factory
 * @returns {T}
 */
export function getCached(key, factory) {
    if (Object.hasOwn(cache, key)) {
        return /** @type {T} */ (cache[key]);
    }

    const value = factory();
    cache[key] = value;
    return value;
}

/**
 * @template T
 * @param {string} key
 * @param {() => T} factory
 * @returns {T}
 */
export function getCachedWhenTruthy(key, factory) {
    if (Object.hasOwn(cache, key)) {
        return /** @type {T} */ (cache[key]);
    }

    const value = factory();

    if (value) {
        cache[key] = value;
    }

    return value;
}

/**
 * @template T
 * @param {string} key
 * @param {() => Promise<T>} factory
 * @returns {Promise<T>}
 */
export function getCachedAsync(key, factory) {
    if (Object.hasOwn(cache, key)) {
        return /** @type {Promise<T>} */ (cache[key]);
    }

    const value = factory();
    cache[key] = value;
    return value;
}
