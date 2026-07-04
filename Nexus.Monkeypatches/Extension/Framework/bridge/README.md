# W7 Bridge (v2)

Event-driven protocol between **MAIN world**, **ISOLATED relay**, and the **service worker**.

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
| `bridge_core.js` | bundled everywhere | Protocol constants and live examples |
| `types.js` | editors only | JSDoc typedefs for message shapes |
| `isolated_world.js` | ISOLATED (injected) | Relay — self-contained, no imports |
| `service_worker.js` | SW (bootstrap bundle) | Dispatch, handlers, installers |
| `main_world.js` | MAIN (runtime bundle) | Client API + `installMainWorldBridge()` |

## Installation order

1. **Bootstrap** (`onCommitted`): `installIsolatedBridgeAsync(tabId)` — relay first.
2. **Bootstrap**: fetch `runtime.min.js`, `injectRuntimeInMainWorldAsync(tabId, source)`.
3. **Runtime** `init()`: `installMainWorldBridge()` — exposes `window.w7framework_bridge`.

## Message types

| `type` | Direction | Purpose |
|--------|-----------|---------|
| `w7bridge:invocation:request` | MAIN → SW | Start a privileged operation |
| `w7bridge:invocation:response` | SW → MAIN | Complete or fail an invocation |
| `w7bridge:relay_error` | ISOLATED → MAIN | `sendMessage` failed; carries `sourceMessage` |
| `w7bridge:network:event` | SW → MAIN | Push network observation (listeners ready; SW observe TBD) |

## Invocation methods

| `method` | Handler | Notes |
|----------|---------|-------|
| `fetch` | SW `fetch` | Returns serialised response (`body` as text) |
| `eval_in_main_world` | `executeScript` MAIN | `args.source` is async function body |
| `eval_in_isolated_world` | `executeScript` ISOLATED | Same shape as MAIN eval |
| `set_network_redirect` | `declarativeNetRequest` | `args.rules` — DNR redirect rules; serialised in SW to avoid ID races |
| `unset_network_redirect` | `declarativeNetRequest` | `args.all` or `args.ids`; serialised with set |

## MAIN-world API

After `installMainWorldBridge()`, `window.w7framework_bridge` exposes:

- `invokeAsync(method, args)`
- `evalInMainWorldAsync({ source, args })`
- `evalInIsolatedWorldAsync({ source, args })`
- `fetchAsync({ url })`
- `setNetworkRedirectAsync({ rules })`
- `unsetNetworkRedirectAsync({ all } | { ids })`
- `addNetworkEventListener(fn)` / `removeNetworkEventListener(fn)`

Modules may import the same functions directly from `main_world.js`.

## Error handling

- **Relay failure** (extension reload, SW asleep): isolated posts `RELAY_ERROR` with the original request in `sourceMessage`; MAIN rejects the matching `invocationId` promise.
- **Handler failure**: SW posts `INVOCATION_RESPONSE` with `isSuccess: false` and `error` message.
- **Tab gone** before response: SW swallows `tabs.sendMessage` errors silently.
- **Concurrent DNR updates**: `set_network_redirect` / `unset_network_redirect` run through a serial queue in the SW so parallel callers cannot read stale `getDynamicRules()` snapshots and collide on rule IDs.

## Example — eval in MAIN

```js
await window.w7framework_bridge.evalInMainWorldAsync({
    source: "return document.title;",
});
```

## Future work

- `webRequest` / network observe → `sendNetworkEventAsync` + MAIN listeners (types and API already in place).
