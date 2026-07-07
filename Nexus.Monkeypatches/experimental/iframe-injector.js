/**
 * Cole no console do DevTools para envolver a página atual em um visualizador debug.
 * Substitui o documento in-place (mesma aba, mesma URL/origin) e carrega o site
 * original dentro de um iframe sandboxed.
 */
(function iframeInjector() {
    const originalUrl = location.href;
    const originalTitle = document.title;
    const debugTitle = `[Debug] ${originalTitle}`;

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }

    const safeUrl = escapeHtml(originalUrl);
    const safeTitle = escapeHtml(originalTitle || "Original page");
    const safeDebugTitle = escapeHtml(debugTitle);

    document.open();
    document.write(`<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>${safeDebugTitle}</title>
    <style>
        html, body {
            margin: 0;
            padding: 0;
            width: 100%;
            height: 100%;
            overflow: hidden;
            font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
            background: #0f1117;
            color: #e6edf3;
        }

        #nexus-debug-shell {
            display: flex;
            flex-direction: column;
            width: 100%;
            height: 100%;
        }

        #nexus-debug-header {
            flex: 0 0 auto;
            display: flex;
            align-items: center;
            gap: 12px;
            min-height: 44px;
            padding: 8px 12px;
            border-bottom: 1px solid #30363d;
            background: #161b22;
            box-sizing: border-box;
        }

        #nexus-debug-badge {
            flex: 0 0 auto;
            padding: 2px 8px;
            border-radius: 999px;
            background: #238636;
            color: #fff;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.04em;
            text-transform: uppercase;
        }

        #nexus-debug-title {
            flex: 0 0 auto;
            font-size: 12px;
            font-weight: 600;
            color: #8b949e;
        }

        #nexus-debug-url {
            flex: 1 1 auto;
            min-width: 0;
            padding: 6px 10px;
            border: 1px solid #30363d;
            border-radius: 6px;
            background: #0d1117;
            color: #c9d1d9;
            font: inherit;
            font-size: 12px;
            outline: none;
        }

        #nexus-debug-url:focus {
            border-color: #58a6ff;
            box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.2);
        }

        #nexus-debug-actions {
            flex: 0 0 auto;
            display: flex;
            gap: 8px;
        }

        .nexus-debug-btn {
            border: 1px solid #30363d;
            border-radius: 6px;
            background: #21262d;
            color: #e6edf3;
            padding: 6px 10px;
            font: inherit;
            font-size: 12px;
            cursor: pointer;
        }

        .nexus-debug-btn:hover {
            background: #30363d;
        }

        #nexus-debug-frame-wrap {
            flex: 1 1 auto;
            min-height: 0;
            position: relative;
            background: #010409;
        }

        #nexus-debug-frame {
            display: block;
            width: 100%;
            height: 100%;
            border: 0;
            background: #fff;
        }
    </style>
</head>
<body>
    <div id="nexus-debug-shell">
        <header id="nexus-debug-header">
            <span id="nexus-debug-badge">Debug</span>
            <span id="nexus-debug-title">iframe viewer</span>
            <input id="nexus-debug-url" type="text" spellcheck="false" value="${safeUrl}" />
            <div id="nexus-debug-actions">
                <button class="nexus-debug-btn" type="button" data-action="reload">Reload</button>
            </div>
        </header>
        <div id="nexus-debug-frame-wrap">
            <iframe
                id="nexus-debug-frame"
                name="nexus-debug-frame"
                title="${safeTitle}"
                src="${safeUrl}"
                sandbox="allow-scripts allow-same-origin allow-forms allow-modals"
                referrerpolicy="strict-origin-when-cross-origin"
            ></iframe>
        </div>
    </div>
</body>
</html>`);
    document.close();

    const frame = document.getElementById("nexus-debug-frame");
    const urlInput = document.getElementById("nexus-debug-url");
    const reloadBtn = document.querySelector('[data-action="reload"]');

    function navigateTo(url) {
        const nextUrl = url.trim();
        if (!nextUrl) {
            return;
        }

        frame.src = nextUrl;
        urlInput.value = nextUrl;
    }

    function lockDownFrameWindow(frameWindow) {
        if (!frameWindow) {
            return;
        }

        frameWindow.open = () => null;
    }

    function syncFrameState() {
        try {
            const frameWindow = frame.contentWindow;
            const frameUrl = frameWindow?.location.href;

            if (frameUrl && frameUrl !== "about:blank") {
                urlInput.value = frameUrl;
            }

            lockDownFrameWindow(frameWindow);

            const sameOrigin = window.origin === frameWindow?.origin;
            console.info("[iframe-injector] same-origin check:", sameOrigin, {
                parentOrigin: window.origin,
                frameOrigin: frameWindow?.origin,
            });
        } catch {
            console.warn("[iframe-injector] iframe cross-origin; URL sync and open lock skipped.");
        }
    }

    urlInput.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            navigateTo(urlInput.value);
        }
    });

    reloadBtn.addEventListener("click", () => {
        try {
            frame.contentWindow.location.reload();
        } catch {
            frame.src = urlInput.value;
        }
    });

    frame.addEventListener("load", syncFrameState);

    console.info("[iframe-injector] Debug shell ativo.", {
        originalUrl,
        parentOrigin: window.origin,
        parentHref: location.href,
    });
})();
