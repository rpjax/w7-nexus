import { RUNTIME_VERSION } from "./config.js";
import { bindRuntimeToWindow } from "./runtime_api.js";

let started = false;

function start() {
    if (started) {
        return;
    }

    started = true;

    console.log(
        "%c W7 Monkeypatch Runtime %c v" + RUNTIME_VERSION + " %c",
        "background:#111;color:#7ee787;font-weight:bold;padding:4px 8px;border-radius:4px 0 0 4px",
        "background:#21262d;color:#ffa657;font-weight:bold;padding:4px 8px",
        "background:#111;color:#8b949e;padding:4px 8px;border-radius:0 4px 4px 0",
        "\n  status: alive",
        "\n  patch:  (none yet — runtime is vibing)",
        "\n  mood:   dangerously operational",
    );
}

function stop() {
    if (!started) {
        return;
    }

    started = false;
    // logs stop message
    console.log("W7 Monkeypatch Runtime stopped");
}

bindRuntimeToWindow({
    startRuntime: start,
    stopRuntime: stop,
});

start();
