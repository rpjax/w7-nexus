import {
    getState as internalGetState,
    createState as internalCreateState,
    deleteState as internalDeleteState,
} from "../../state.js";

/**
 * @param {string} key
 * @returns {any}
 */
export function getState(key) {
    return internalGetState(key);
}

/**
 * @param {string} key
 * @param {() => object} factory
 * @returns {any}
 */
export function createState(key, factory) {
    return internalCreateState(key, factory);
}

/**
 * @param {string} key
 */
export function deleteState(key) {
    return internalDeleteState(key);
}
