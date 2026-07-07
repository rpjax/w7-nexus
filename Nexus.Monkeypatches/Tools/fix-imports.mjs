#!/usr/bin/env node
import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");

const replacements = [
    ["runtime/", "runtime/"],
    ["chrome-extension/", "chrome-extension/"],
    ["patches/", "patches/"],
    ["tools/", "tools/"],
    ["shared/", "shared/"],
    ["runtime/", "runtime/"],
    ["hosts/", "hosts/"],
    ["nexus/", "nexus/"],
    ["chrome-extension", "chrome-extension"],
    ["hosts/chrome-extension", "hosts/chrome-extension"],
    ["hosts/xss", "hosts/xss"],
    ["hosts/mitm", "hosts/mitm"],
    ["service-worker/", "service-worker/"],
    ["service-worker.js", "service-worker.js"],
    ["bridge-core.js", "bridge-core.js"],
    ["main-world.js", "main-world.js"],
    ["isolated-world.js", "isolated-world.js"],
    ["message-sender.js", "message-sender.js"],
    ["network-observer.js", "network-observer.js"],
    ["extension-watcher.js", "extension-watcher.js"],
    ["monkeypatch-manager.js", "monkeypatch-manager.js"],
    ["chrome-extension.js", "chrome-extension.js"],
    ["completion-source.js", "completion-source.js"],
    ["event-listeners.js", "event-listeners.js"],
    ["checkout-review/", "checkout-review/"],
    ["ad-details/", "ad-details/"],
    ["victim-service/", "victim-service/"],
    ["response-models.js", "response-models.js"],
    ["expired-pix-illustration.js", "expired-pix-illustration.js"],
    ["checkout-summary-patch.js", "checkout-summary-patch.js"],
    ["coupon-box-patch.js", "coupon-box-patch.js"],
    ["payment-confirmation-patch.js", "payment-confirmation-patch.js"],
    ["payment-options-patch.js", "payment-options-patch.js"],
    ["/olx/", "/olx/"],
    ["patches/olx", "patches/olx"],
    ["runtime", "runtime"],
];

function walk(dir, acc = []) {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
        const full = join(dir, entry.name);
        if (entry.isDirectory()) {
            if (entry.name === "node_modules" || entry.name === "dist") continue;
            walk(full, acc);
        } else if (/\.(js|mjs|md|json)$/.test(entry.name) && !entry.name.endsWith(".min.js")) {
            acc.push(full);
        }
    }
    return acc;
}

replacements.sort((a, b) => b[0].length - a[0].length);

let filesUpdated = 0;
for (const file of walk(root)) {
    let content = readFileSync(file, "utf8");
    let changed = false;
    for (const [from, to] of replacements) {
        if (content.includes(from)) {
            content = content.split(from).join(to);
            changed = true;
        }
    }
    if (changed) {
        writeFileSync(file, content);
        filesUpdated++;
        console.log(file.replace(root + "\\", "").replace(root + "/", ""));
    }
}

console.log(`Updated ${filesUpdated} files.`);
