import "../../shared/libs/signalr.min.js";
import { NEXUS_HUB_URL } from "../runtime/env.js";
const {
    HubConnectionBuilder,
    HubConnectionState,
    HttpTransportType,
    LogLevel,
} = globalThis.signalR;

const START_RETRY_MS = 5_000;

/** @typedef {"idle" | "connecting" | "connected" | "reconnecting" | "stopped"} NexusUplinkState */

/** @type {NexusUplinkState} */
let state = "idle";

/** @type {import("@microsoft/signalr").HubConnection | null} */
let connection = null;

/** @type {Promise<void> | null} */
let startTask = null;

/** @type {ReturnType<typeof setTimeout> | null} */
let startRetryTimer = null;

let stopRequested = false;

function log(phase, fields = {}) {
    const suffix = Object.keys(fields).length > 0 ? ` ${JSON.stringify(fields)}` : "";
    console.info(`[w7-nexus] ${phase}${suffix}`);
}

function clearStartRetryTimer() {
    if (startRetryTimer != null) {
        clearTimeout(startRetryTimer);
        startRetryTimer = null;
    }
}

function scheduleStartRetry() {
    if (stopRequested || startRetryTimer != null) {
        return;
    }

    startRetryTimer = setTimeout(() => {
        startRetryTimer = null;
        void connectAsync();
    }, START_RETRY_MS);
}

function mapConnectionState(hubState) {
    switch (hubState) {
        case HubConnectionState.Connected:
            return "connected";
        case HubConnectionState.Connecting:
            return "connecting";
        case HubConnectionState.Reconnecting:
            return "reconnecting";
        case HubConnectionState.Disconnecting:
            return "idle";
        default:
            return stopRequested ? "stopped" : "idle";
    }
}

function createConnection() {
    const hub = new HubConnectionBuilder()
        .withUrl(NEXUS_HUB_URL, { transport: HttpTransportType.WebSockets })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

    hub.onreconnecting((error) => {
        state = "reconnecting";
        log("reconnecting", { error: error?.message });
    });

    hub.onreconnected((connectionId) => {
        state = "connected";
        log("reconnected", { connectionId });
    });

    hub.onclose((error) => {
        connection = null;

        if (stopRequested) {
            state = "stopped";
            log("stopped");
            return;
        }

        state = "idle";
        log("disconnected", { error: error?.message });
        scheduleStartRetry();
    });

    return hub;
}

function ensureConnection() {
    if (connection == null) {
        connection = createConnection();
    }

    return connection;
}

async function connectAsync() {
    if (stopRequested) {
        return;
    }

    const hub = ensureConnection();
    const hubState = hub.state;

    if (hubState === HubConnectionState.Connected) {
        state = "connected";
        return;
    }

    if (
        hubState === HubConnectionState.Connecting ||
        hubState === HubConnectionState.Reconnecting ||
        hubState === HubConnectionState.Disconnecting
    ) {
        return;
    }

    state = "connecting";
    log("connecting", { hub: NEXUS_HUB_URL });

    try {
        await hub.start();

        if (stopRequested) {
            await hub.stop();
            connection = null;
            state = "stopped";
            return;
        }

        state = "connected";
        log("connected", { connectionId: hub.connectionId });
    } catch (error) {
        connection = null;
        state = "idle";
        log("connect_failed", {
            error: error instanceof Error ? error.message : String(error),
        });
        scheduleStartRetry();
    }
}

/** @returns {NexusUplinkState} */
export function getNexusUplinkState() {
    if (connection != null) {
        return mapConnectionState(connection.state);
    }

    return state;
}

/** @returns {boolean} */
export function isNexusUplinkConnected() {
    return connection?.state === HubConnectionState.Connected;
}

/**
 * Starts the singleton uplink if it is not already running.
 * Safe to call multiple times ÔÇö subsequent calls await the in-flight dial.
 */
export async function startNexusUplinkAsync() {
    stopRequested = false;
    clearStartRetryTimer();

    if (isNexusUplinkConnected()) {
        return;
    }

    if (startTask != null) {
        return startTask;
    }

    startTask = connectAsync().finally(() => {
        startTask = null;
    });

    return startTask;
}

/**
 * Stops the uplink, cancels pending retries, and closes the active connection.
 */
export async function stopNexusUplinkAsync() {
    stopRequested = true;
    clearStartRetryTimer();

    const hub = connection;
    connection = null;

    if (hub != null) {
        try {
            await hub.stop();
        } catch (error) {
            log("stop_failed", {
                error: error instanceof Error ? error.message : String(error),
            });
        }
    }

    state = "stopped";
    log("stop");
}

export function setNexusEventListener(eventType, listener) {
    if (connection == null) {
        return;
    }

    connection.on(eventType, listener);
}
