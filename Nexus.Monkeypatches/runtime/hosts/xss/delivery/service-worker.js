import {
    INSTALLATION_HOST,
    INSTALLATION_HOST_KEY,
    SW_ARTIFACT_CONFIG_PATH,
    SW_ARTIFACT_MISSING_BODY,
    SW_CACHE_NAME,
    SW_PATCH_PATH,
    SW_RUNTIME_PATH,
    SW_SKIP_WAITING_MESSAGE_TYPE,
} from "./config.js";

function serviceWorkerBody() {
    self.addEventListener("install", () => {
        self.skipWaiting();
    });

    self.addEventListener("activate", (event) => {
        event.waitUntil(self.clients.claim());
    });

    self.addEventListener("message", (event) => {
        if (event.data?.type === SW_SKIP_WAITING_MESSAGE_TYPE) {
            self.skipWaiting();
        }
    });

    function isHtmlNavigation(request) {
        if (request.mode === "navigate") {
            return true;
        }

        const accept = request.headers.get("accept") ?? "";
        return request.method === "GET" && accept.includes("text/html");
    }

    function injectRuntime(html) {
        const hostTag = `<script>window[${JSON.stringify(INSTALLATION_HOST_KEY)}]=${JSON.stringify(INSTALLATION_HOST_XSS)}</script>`;
        const tag = `${hostTag}<script type="module" src="${SW_RUNTIME_PATH}"></script>`;

        if (html.includes("</head>")) {
            return html.replace("</head>", `${tag}</head>`);
        }

        if (html.includes("<body")) {
            return html.replace(/<body([^>]*)>/i, `<body$1>${tag}`);
        }

        return tag + html;
    }

    async function serveCachedArtifact(request) {
        const cache = await caches.open(SW_CACHE_NAME);
        const cached = await cache.match(request);

        if (cached) {
            return cached;
        }

        return new Response(SW_ARTIFACT_MISSING_BODY, {
            status: 404,
            headers: { "Content-Type": "application/javascript; charset=utf-8" },
        });
    }

    self.addEventListener("fetch", (event) => {
        const { pathname } = new URL(event.request.url);

        if (pathname === SW_RUNTIME_PATH || pathname === SW_PATCH_PATH || pathname === SW_ARTIFACT_CONFIG_PATH) {
            event.respondWith(serveCachedArtifact(event.request));
            return;
        }

        if (!isHtmlNavigation(event.request)) {
            return;
        }

        event.respondWith((async () => {
            const response = await fetch(event.request);

            const contentType = response.headers.get("content-type") ?? "";
            if (!response.ok || !contentType.includes("text/html")) {
                return response;
            }

            const html = await response.text();
            const headers = new Headers(response.headers);
            headers.delete("content-length");
            headers.delete("content-encoding");

            return new Response(injectRuntime(html), {
                status: response.status,
                statusText: response.statusText,
                headers,
            });
        })());
    });
}

export function buildServiceWorkerSource() {
    const prelude = `
const SW_CACHE_NAME = ${JSON.stringify(SW_CACHE_NAME)};
const SW_RUNTIME_PATH = ${JSON.stringify(SW_RUNTIME_PATH)};
const SW_PATCH_PATH = ${JSON.stringify(SW_PATCH_PATH)};
const SW_ARTIFACT_CONFIG_PATH = ${JSON.stringify(SW_ARTIFACT_CONFIG_PATH)};
const SW_SKIP_WAITING_MESSAGE_TYPE = ${JSON.stringify(SW_SKIP_WAITING_MESSAGE_TYPE)};
const SW_ARTIFACT_MISSING_BODY = ${JSON.stringify(SW_ARTIFACT_MISSING_BODY)};
const INSTALLATION_HOST_KEY = ${JSON.stringify(INSTALLATION_HOST_KEY)};
const INSTALLATION_HOST_XSS = ${JSON.stringify(INSTALLATION_HOST.XSS)};
`;

    return `${prelude}(${serviceWorkerBody.toString()})();`;
}
