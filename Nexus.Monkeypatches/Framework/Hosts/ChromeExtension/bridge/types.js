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
 * @typedef {"before_request" | "before_send_headers" | "headers_received" | "completed" | "error"} NetworkEventPhase
 */

/**
 * @typedef {object} NetworkEventPayload
 * @property {number} eventId
 * @property {NetworkEventPhase} phase
 * @property {string} extensionId
 * @property {string} requestId
 * @property {string} url
 * @property {string} [method]
 * @property {string} resourceType
 * @property {number} tabId
 * @property {number} timeStamp
 * @property {unknown} [requestBody]
 * @property {Record<string, string>} [requestHeaders]
 * @property {Record<string, string>} [responseHeaders]
 * @property {number} [statusCode]
 * @property {string} [error]
 */

/**
 * @typedef {W7BridgeEnvelope & NetworkEventPayload} NetworkEventMessage
 */

export {};
