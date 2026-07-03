#!/usr/bin/env node
import { createServer } from "node:http";
import { existsSync } from "node:fs";
import { readFile } from "node:fs/promises";
import { dirname, extname, join, normalize } from "node:path";
import { fileURLToPath } from "node:url";

const localServerDir = dirname(fileURLToPath(import.meta.url));

const MIME_TYPES = {
    ".js": "application/javascript; charset=utf-8",
    ".mjs": "application/javascript; charset=utf-8",
    ".json": "application/json; charset=utf-8",
    ".html": "text/html; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".map": "application/json; charset=utf-8",
    ".txt": "text/plain; charset=utf-8",
};

const ORIGIN_PATCH_ROUTES = {
    "https://www.olx.com.br": "monkeypatches/patches/olx.min.js",
    "https://olx.com.br": "monkeypatches/patches/olx.min.js",
};

function resolvePatchPath(originParam) {
    if (originParam == null || originParam === "") {
        return null;
    }

    const exact = ORIGIN_PATCH_ROUTES[originParam];
    if (exact != null) {
        return exact;
    }

    try {
        const hostname = new URL(originParam).hostname.toLowerCase();
        if (hostname.includes("olx")) {
            return "monkeypatches/patches/olx.min.js";
        }
    } catch {
        return null;
    }

    return null;
}

function printHelp() {
    console.log(`Usage: node server.mjs [options]

Serves static files for local monkeypatch runtime development.

Options:
  --host <host>   Bind host (default: 127.0.0.1)
  --port <port>   Bind port (default: 444)
  --root <path>   Static root (default: ./wwwroot)
  --help          Show this help

Example:
  node server.mjs
  node server.mjs --port 444 --root ./wwwroot
`);
}

function parseArgs(argv) {
    const options = {
        host: "127.0.0.1",
        port: 444,
        root: join(localServerDir, "wwwroot"),
        help: false,
    };

    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];

        switch (arg) {
            case "--help":
            case "-h":
                options.help = true;
                break;
            case "--host":
                options.host = argv[++i];
                break;
            case "--port":
                options.port = Number(argv[++i]);
                break;
            case "--root":
                options.root = argv[++i];
                break;
            default:
                console.error(`Unknown option: ${arg}`);
                printHelp();
                process.exit(1);
        }
    }

    if (!Number.isInteger(options.port) || options.port <= 0) {
        console.error("Invalid --port");
        process.exit(1);
    }

    return options;
}

function applyCorsHeaders(response, origin) {
    if (origin) {
        response.setHeader("Access-Control-Allow-Origin", origin);
        response.setHeader("Access-Control-Allow-Credentials", "true");
        response.setHeader("Vary", "Origin");
    } else {
        response.setHeader("Access-Control-Allow-Origin", "*");
    }

    response.setHeader("Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
    response.setHeader("Access-Control-Allow-Headers", "*");
    response.setHeader("Access-Control-Max-Age", "86400");
    response.setHeader("Access-Control-Allow-Private-Network", "true");
    response.setHeader("Private-Network-Access-Name", "nexus-w7");
    response.setHeader("Private-Network-Access-ID", "7e:00:00:00:00:01");
    response.setHeader("Cross-Origin-Resource-Policy", "cross-origin");
}

function resolveFilePath(rootDir, requestPath) {
    const decoded = decodeURIComponent(requestPath.split("?")[0]);
    const relative = normalize(decoded).replace(/^(\.\.(\/|\\|$))+/, "");
    const filePath = normalize(join(rootDir, relative));

    if (!filePath.startsWith(normalize(rootDir))) {
        return null;
    }

    return filePath;
}

async function serveFile(filePath, response) {
    const content = await readFile(filePath);
    const type = MIME_TYPES[extname(filePath).toLowerCase()] ?? "application/octet-stream";

    response.statusCode = 200;
    response.setHeader("Content-Type", type);
    response.setHeader("Cache-Control", "no-cache");
    response.end(content);
}

function main() {
    const options = parseArgs(process.argv.slice(2));

    if (options.help) {
        printHelp();
        return;
    }

    const rootDir = normalize(options.root);

    if (!existsSync(rootDir)) {
        console.error(`Root not found: ${rootDir}`);
        process.exit(1);
    }

    const server = createServer(async (request, response) => {
        const origin = request.headers.origin;
        applyCorsHeaders(response, origin);

        if (request.method === "OPTIONS") {
            response.statusCode = 204;
            response.end();
            return;
        }

        if (request.method !== "GET" && request.method !== "HEAD") {
            response.statusCode = 405;
            response.end("Method Not Allowed");
            return;
        }

        const url = new URL(request.url ?? "/", `http://${options.host}`);

        if (url.pathname === "/monkeypatches") {
            const pageOrigin = url.searchParams.get("origin");
            const patchRelativePath = resolvePatchPath(pageOrigin);

            if (patchRelativePath == null) {
                response.statusCode = 404;
                response.end(`No patch registered for origin: ${pageOrigin ?? "(missing)"}`);
                console.log(`404 ${request.method} /monkeypatches?origin=${pageOrigin ?? ""}`);
                return;
            }

            const patchFilePath = resolveFilePath(rootDir, `/${patchRelativePath}`);

            if (patchFilePath == null || !existsSync(patchFilePath)) {
                response.statusCode = 404;
                response.end("Patch file not found");
                console.log(`404 ${request.method} /monkeypatches → ${patchRelativePath}`);
                return;
            }

            try {
                if (request.method === "HEAD") {
                    response.statusCode = 200;
                    response.setHeader("Content-Type", MIME_TYPES[".js"]);
                    response.end();
                } else {
                    await serveFile(patchFilePath, response);
                }

                console.log(`200 ${request.method} /monkeypatches?origin=${pageOrigin} → ${patchRelativePath}`);
            } catch (error) {
                console.error(`500 /monkeypatches`, error);
                response.statusCode = 500;
                response.end("Internal Server Error");
            }

            return;
        }

        const filePath = resolveFilePath(rootDir, url.pathname === "/" ? "/index.html" : url.pathname);

        if (filePath == null || !existsSync(filePath)) {
            response.statusCode = 404;
            response.end("Not Found");
            console.log(`404 ${request.method} ${url.pathname}`);
            return;
        }

        try {
            if (request.method === "HEAD") {
                response.statusCode = 200;
                response.setHeader("Content-Type", MIME_TYPES[extname(filePath).toLowerCase()] ?? "application/octet-stream");
                response.end();
            } else {
                await serveFile(filePath, response);
            }

            console.log(`200 ${request.method} ${url.pathname}`);
        } catch (error) {
            console.error(`500 ${url.pathname}`, error);
            response.statusCode = 500;
            response.end("Internal Server Error");
        }
    });

    server.on("error", (error) => {
        if (error.code === "EADDRINUSE") {
            console.error(`Port ${options.port} is already in use on ${options.host}.`);
            console.error("Stop the other process or run with a different --port.");
            process.exit(1);
        }

        throw error;
    });

    server.listen(options.port, options.host, () => {
        console.log(`LocalServer running at http://${options.host}:${options.port}/`);
        console.log(`Serving: ${rootDir}`);
        console.log("");
        console.log("URLs:");
        console.log(`  runtime  http://${options.host}:${options.port}/monkeypatches/framework/runtime.min.js`);
        console.log(`  patch    http://${options.host}:${options.port}/monkeypatches?origin=<page-origin>`);
        console.log(`           e.g. ?origin=https://www.olx.com.br → patches/olx.min.js`);
    });
}

main();
