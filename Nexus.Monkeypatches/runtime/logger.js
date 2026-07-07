const mono = "font-family:ui-monospace,monospace;font-size:11px";

const palette = {
    bg: "#161b22",
    panel: "#21262d",
    border: "#30363d",
    muted: "#6e7681",
    text: "#c9d1d9",
    badge: "#8b949e",
};

const badgeStyle = `background:${palette.bg};color:${palette.badge};padding:2px 6px;border-radius:3px 0 0 3px;${mono};border:1px solid ${palette.border};border-right:none`;

const phaseStyle = {
    init: `color:#58a6ff;font-weight:600;${mono}`,
    online: `color:#3fb950;font-weight:600;${mono}`,
    offline: `color:#d29922;font-weight:600;${mono}`,
    patch: `color:#bc8cff;font-weight:600;${mono}`,
};

function pillStyle(color, radius = "0 3px 0 0") {
    return `${color};background:${palette.panel};padding:2px 8px;border-radius:${radius};border:1px solid ${palette.border};border-left:none`;
}

function formatValue(value) {
    if (value instanceof Error) {
        return value.stack ?? value.message;
    }
    if (typeof value === "object" && value !== null) {
        return JSON.stringify(value);
    }
    return String(value);
}

function appendFieldRows(format, styles, fields) {
    for (const [key, value] of Object.entries(fields)) {
        format += `\n%c${key.padEnd(7)}%c ${formatValue(value)}`;
        styles.push(`color:${palette.muted};${mono}`, `color:${palette.text};${mono}`);
    }
    return format;
}

function emit(writer, format, styles) {
    writer.call(console, format, ...styles);
}

/** Runtime lifecycle transition — phase header plus caller-supplied context rows. */
export function logLifecycle(phase, fields = {}) {
    const phaseColor = phaseStyle[phase] ?? phaseStyle.init;

    let format = "%c w7-runtime %c " + phase.toUpperCase();
    const styles = [badgeStyle, pillStyle(phaseColor)];

    format = appendFieldRows(format, styles, fields);
    emit(console.info, format, styles);
}

/** Operational note — inline message with optional detail rows. */
export function logInfo(message, fields = {}) {
    let format = "%c w7-runtime %c INFO %c " + message;
    const styles = [
        badgeStyle,
        pillStyle(`color:#58a6ff;font-weight:600;${mono}`, "0"),
        `color:${palette.text};${mono}`,
    ];

    format = appendFieldRows(format, styles, fields);
    emit(console.info, format, styles);
}

/** Recoverable issue — warning marker and amber message emphasis. */
export function logWarn(message, fields = {}) {
    let format = "%c w7-runtime %c WARN\n%c ! %c " + message;
    const styles = [
        badgeStyle,
        pillStyle(`color:#9e6a03;font-weight:600;background:#3d2e00;${mono}`),
        `color:#d29922;font-weight:700;${mono}`,
        `color:#e3b341;${mono}`,
    ];

    format = appendFieldRows(format, styles, fields);
    emit(console.warn, format, styles);
}

/** Failure — fault marker, red message emphasis, Error-aware field values. */
export function logError(message, fields = {}) {
    let format = "%c w7-runtime %c ERROR\n%c × %c " + message;
    const styles = [
        badgeStyle,
        pillStyle(`color:#ff7b72;font-weight:600;background:#3d1418;${mono}`),
        `color:#f85149;font-weight:700;${mono}`,
        `color:#ffa198;${mono}`,
    ];

    format = appendFieldRows(format, styles, fields);
    emit(console.error, format, styles);
}
