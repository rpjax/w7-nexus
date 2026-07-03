import { RUNTIME_VERSION } from "./env.js";

const mono = "font-family:ui-monospace,monospace;font-size:11px";

const phaseStyle = {
    init: `color:#58a6ff;font-weight:600;${mono}`,
    online: `color:#3fb950;font-weight:600;${mono}`,
    offline: `color:#d29922;font-weight:600;${mono}`,
    patch: `color:#bc8cff;font-weight:600;${mono}`,
};

export function logLifecycle(phase, fields = {}) {
    const rows = Object.entries({ version: RUNTIME_VERSION, ...fields });
    const phaseColor = phaseStyle[phase] ?? phaseStyle.init;

    let format = "%c w7-runtime %c " + phase.toUpperCase();
    const styles = [
        `background:#161b22;color:#8b949e;padding:2px 6px;border-radius:3px 0 0 3px;${mono};border:1px solid #30363d;border-right:none`,
        `${phaseColor};background:#21262d;padding:2px 8px;border-radius:0 3px 0 0;border:1px solid #30363d;border-left:none`,
    ];

    for (const [key, value] of rows) {
        format += `\n%c${key.padEnd(7)}%c ${value}`;
        styles.push(`color:#6e7681;${mono}`, `color:#c9d1d9;${mono}`);
    }

    console.info(format, ...styles);
}
