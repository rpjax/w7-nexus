# W7 Bridge (V1)

Event-driven protocol between **MAIN world**, **ISOLATED relay**, and the **service worker**.

**Status:** V1 (`1.0.0`) — protocol frozen. New capabilities belong in runtime or a future bridge V2.

## Topology

```
MAIN ──postMessage(target: SW)──► ISOLATED ──runtime.sendMessage──► SW
SW   ──tabs.sendMessage(target: MAIN)──► ISOLATED ──postMessage──► MAIN
```

- Every message is an envelope: `{ isW7BridgeMessage, source, target, type, ...payload }`.
- The isolated relay is **dumb** — it never awaits `sendMessage` return values.
- Invocation results arrive as **`INVOCATION_RESPONSE` events**, not RPC replies.

## Files

| File | Runs in | Role |
|------|---------|------|
| `bridge-core.js` | bundled everywhere | Protocol constants and live examples |
| `types.js` | editors only | JSDoc typedefs for message shapes |
| `isolated-world.js` | ISOLATED (injected) | Relay — self-contained, no imports |
| `main-world.js` | MAIN (runtime bundle) | Client API + `installChromeExtensionBridge()` |
| `service-worker/service-worker.js` | SW (bootstrap bundle) | Dispatch, handlers, installers |
| `service-worker/message-sender.js` | SW | Outbound channel SW → MAIN (`INVOCATION_RESPONSE`, `NETWORK_EVENT`) |
| `service-worker/network-observer.js` | SW | Passive `webRequest` observe of other extensions |
| `helpers/permissions.js` | SW | Invocation permission discovery (`runtime/hosts/chrome-extension/helpers/permissions.js`) |

## Installation order

1. **Bootstrap** (`onCommitted`): `installIsolatedBridgeAsync(tabId)` — relay first.
2. **Bootstrap**: `injectInstallationHostAsync(tabId, …, CHROME_EXTENSION)`.
3. **Bootstrap**: fetch `runtime.min.js`, `injectRuntimeInMainWorldAsync(tabId, source)`.
4. **Runtime** init (host `CHROME_EXTENSION`): `installChromeExtensionBridge()` → `window.w7runtime.chromeExtension.bridge`.

## Message types

| `type` | Direction | Purpose |
|--------|-----------|---------|
| `w7bridge:invocation:request` | MAIN → SW | Start a privileged operation |
| `w7bridge:invocation:response` | SW → MAIN | Complete or fail an invocation |
| `w7bridge:relay_error` | ISOLATED → MAIN | `sendMessage` failed; carries `sourceMessage` |
| `w7bridge:network:event` | SW → MAIN | Push passive network observation event |

## Invocation methods

| `method` | Handler | Notes |
|----------|---------|-------|
| `fetch` | SW `fetch` | Returns serialised response (`body` as text) |
| `eval_in_main_world` | `executeScript` MAIN | `args.source` is async function body |
| `eval_in_isolated_world` | `executeScript` ISOLATED | Same shape as MAIN eval |
| `set_network_redirect` | `declarativeNetRequest` | `args.rules` — DNR redirect rules; serialised in SW to avoid ID races |
| `unset_network_redirect` | `declarativeNetRequest` | `args.all` or `args.ids`; serialised with set |
| `start_network_observe` | `network-observer.js` | `args.extensionId` — subscribe tab to extension traffic |
| `stop_network_observe` | `network-observer.js` | `args.extensionId` — unsubscribe tab |

## Network observe

Requires manifest permissions `webRequest`, `extraHeaders`, and `<all_urls>` host access.

**Match criteria** (request belongs to watched `extensionId` if any):

- `args.extensionId` is normalised (trim + lowercase) and must match `[a-p]{32}`

- `details.initiator` is `chrome-extension://{id}/*`
- `details.documentUrl` is `chrome-extension://{id}/*`
- `details.tabId` points to a tab whose URL is `chrome-extension://{id}/*`

**Phases emitted** (correlate via `requestId` + `extensionId`):

| Phase | Listener | Extra fields |
|-------|----------|--------------|
| `before_request` | `onBeforeRequest` | `method`, `requestBody?` |
| `before_send_headers` | `onBeforeSendHeaders` | `requestHeaders` |
| `headers_received` | `onHeadersReceived` | `responseHeaders`, `statusCode` |
| `completed` | `onCompleted` | `statusCode` |
| `error` | `onErrorOccurred` | `error` |

**Routing:** all tabs subscribed to the same `extensionId` receive matching events.

**Subscription model:** one subscription per `(tabId, extensionId)`; `start` is idempotent (re-call after SW reload); `tabs.onRemoved` and `runtime.stop()` tear down watches.

### `NETWORK_EVENT` payload

| Field | Type | Notes |
|-------|------|-------|
| `eventId` | number | Monotonic in SW |
| `phase` | string | See phases table above |
| `extensionId` | string | Watched extension |
| `requestId` | string | Chrome webRequest id — same across all phases of one request |
| `url` | string | Request URL |
| `method` | string | HTTP method (when available) |
| `resourceType` | string | Resource type (xhr, fetch, etc.) |
| `tabId` | number | Source tab (-1 if none) |
| `timeStamp` | number | Event timestamp |
| `requestBody` | object | Present on `before_request` when Chrome exposes body (`formData`, `raw` as base64, `truncated`) |
| `requestHeaders` | object | Present on `before_send_headers` — keys lowercase |
| `responseHeaders` | object | Present on `headers_received` — keys lowercase |
| `statusCode` | number | Present on `headers_received` and `completed` |
| `error` | string | Present on `error` |

**Limitations:**

- Observes network visible to `webRequest` only — not JS execution inside third-party extension pages.
- **No response body** — Chrome does not expose it via passive `webRequest` (debugger would be required).
- Sensitive headers (`Cookie`, `Authorization`, `Set-Cookie`, etc.) require the `extraHeaders` manifest permission. Chrome may still redact some values in edge cases even with that permission.

## Invocation permissions

Before dispatching any invocation, the service worker calls `assertInvocationPermissions` (`helpers/permissions.js`). Missing permissions fail via `INVOCATION_RESPONSE` with `isSuccess: false` and an error like `"missing permission: webRequest"`.

| `method` | `permissions` | `origins` |
|----------|---------------|-----------|
| `fetch` | — | host of `args.url` (`${origin}/*`) |
| `eval_in_main_world` | `scripting` | caller tab origin |
| `eval_in_isolated_world` | `scripting` | caller tab origin |
| `set_network_redirect` | `declarativeNetRequest` | — |
| `unset_network_redirect` | `declarativeNetRequest` | — |
| `start_network_observe` | `webRequest`, `extraHeaders` | `<all_urls>` |
| `stop_network_observe` | `webRequest` | — |

Discovery helpers (SW-only): `getInvocationRequirements`, `checkInvocationPermissions`, `getBridgeCapabilitiesAsync`. The bridge does **not** call `chrome.permissions.request` — optional permission opt-in UI is out of scope.

## V1 scope (frozen)

| In scope | Out of scope (runtime or future bridge) |
|----------|----------------------------------------|
| Invocation dispatch + permission guards | Response body capture (needs debugger) |
| Passive network observe (5 phases) | `chrome.permissions.request` UI |
| DNR redirect set/unset (serial queue) | Prod shell permission matrix |
| Relay error → promise rejection | Breaking protocol changes |

Version constant: `BRIDGE_VERSION` in `bridge-core.js` (`"1.0.0"`).

## MAIN-world API

After runtime init on `CHROME_EXTENSION`, `window.w7runtime.chromeExtension.bridge` exposes:

- `invokeAsync(method, args)`
- `evalInMainWorldAsync({ source, args })`
- `evalInIsolatedWorldAsync({ source, args })`
- `fetchAsync({ url })`
- `setNetworkRedirectAsync({ rules })`
- `unsetNetworkRedirectAsync({ all } | { ids })`
- `startNetworkObserveAsync({ extensionId })`
- `stopNetworkObserveAsync({ extensionId })`
- `addNetworkEventListener(fn)` / `removeNetworkEventListener(fn)`

Runtime convenience: `window.w7runtime.chromeExtension.watchExtension(id)` / `unwatchExtension(id)`.

## Error handling

- **Relay failure** (extension reload, SW asleep): isolated posts `RELAY_ERROR` with the original request in `sourceMessage`; MAIN rejects the matching `invocationId` promise.
- **Handler failure**: SW posts `INVOCATION_RESPONSE` with `isSuccess: false` and `error` message.
- **Tab gone** before response: SW swallows `tabs.sendMessage` errors silently.
- **Concurrent DNR updates**: `set_network_redirect` / `unset_network_redirect` run through a serial queue in the SW so parallel callers cannot read stale `getDynamicRules()` snapshots and collide on rule IDs.

## Example — watch extension network

```js
window.w7runtime.chromeExtension.bridge.addNetworkEventListener((event) => {
    if (event.phase === "headers_received") {
        console.log(event.requestId, event.responseHeaders);
    }
});

await window.w7runtime.chromeExtension.watchExtension("abcdefghijklmnopabcdefghijklmnop");
// Open the target extension popup and generate traffic
// Sequence: before_request → before_send_headers → headers_received → completed
```

## Example — eval in MAIN

```js
await window.w7runtime.chromeExtension.bridge.evalInMainWorldAsync({
    source: "return document.title;",
});
```
