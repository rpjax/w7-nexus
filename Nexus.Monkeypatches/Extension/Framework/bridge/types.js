/**
 * JSDoc typedefs for the W7 bridge message contract.
 * Runtime values live in `bridge_core.js`; shapes are documented here for editors and readers.
 */

/**
 * @typedef {object} W7BridgeEnvelope
 * @property {true} isW7BridgeMessage
 * @property {string} source - One of `TARGET_ID` values.
 * @property {string} target - One of `TARGET_ID` values.
 * @property {string} type - One of `MESSAGE_TYPE` values.
 */

/**
 * @typedef {W7BridgeEnvelope & {
 *   invocationId: number,
 *   method: string,
 *   args?: unknown,
 * }} InvocationRequestMessage
 */

/**
 * @typedef {W7BridgeEnvelope & {
 *   invocationId: number,
 *   isSuccess: boolean,
 *   result: unknown,
 *   error: string | null,
 * }} InvocationResponseMessage
 */

/**
 * @typedef {W7BridgeEnvelope & {
 *   sourceMessage: InvocationRequestMessage,
 *   error: string,
 * }} RelayErrorMessage
 */

/**
 * @typedef {W7BridgeEnvelope & Record<string, unknown>} NetworkEventMessage
 */

export {};
