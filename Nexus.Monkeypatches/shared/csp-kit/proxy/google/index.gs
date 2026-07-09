/**
 * CSP-KIT Proxy — Google Apps Script
 *
 * Spec: proxy/ENVELOPE.md
 * Resposta: <div id="csp-kit-proxy-envelope">{json}</div>
 *
 * Deploy: Implantar > Aplicativo da Web > Executar como: Eu > Qualquer pessoa
 *
 * GET  ?url=https://api.exemplo.com/path
 * GET  ?path=/___internal__/ping
 * POST body JSON { "url", "method", "headers", "body" }
 */

var ALLOWED_METHODS = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"];
var ENVELOPE_VERSION = 1;
var ENVELOPE_ID = "csp-kit-proxy-envelope";
var INTERNAL_PREFIX = "___internal__";

/**
 * @param {GoogleAppsScript.Events.DoGet} e
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function doGet(e) {
  return dispatch_(e, "GET");
}

/**
 * @param {GoogleAppsScript.Events.DoPost} e
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function doPost(e) {
  return dispatch_(e, "POST");
}

/**
 * @param {GoogleAppsScript.Events.DoGet|GoogleAppsScript.Events.DoPost} e
 * @param {string} method
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function dispatch_(e, method) {
  var internalRoute = resolveInternalRoute_(e);
  if (internalRoute) {
    return handleInternalRoute_(internalRoute, method);
  }

  return handleProxy_(e, method);
}

/**
 * @param {string} route
 * @param {string} method
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function handleInternalRoute_(route, method) {
  if (route === "ping") {
    if (method !== "GET") {
      return writeError_(405, "Method not allowed for /___internal__/ping");
    }

    return writeEnvelope_(200, "text/plain", "PONG!");
  }

  return writeError_(404, "Unknown internal route: " + route);
}

/**
 * @param {GoogleAppsScript.Events.DoGet|GoogleAppsScript.Events.DoPost} e
 * @param {string} method
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function handleProxy_(e, method) {
  try {
    var request = parseRequest_(e, method);

    if (!request.url) {
      return writeError_(400, "Missing url parameter");
    }

    assertUrlAllowed_(request.url);

    var fetchOptions = {
      method: request.method,
      muteHttpExceptions: true,
      followRedirects: true,
      headers: request.headers,
    };

    if (request.body != null && request.method !== "GET" && request.method !== "HEAD") {
      fetchOptions.payload = request.body;
      if (request.contentType) {
        fetchOptions.contentType = request.contentType;
      }
    }

    var upstream = UrlFetchApp.fetch(request.url, fetchOptions);
    return writeUpstream_(upstream);
  } catch (error) {
    return writeError_(500, String(error.message || error));
  }
}

/**
 * @param {GoogleAppsScript.Events.DoGet|GoogleAppsScript.Events.DoPost} e
 * @returns {string|null}
 */
function resolveInternalRoute_(e) {
  var path = resolveRequestPath_(e);
  if (!path) {
    return null;
  }

  var normalized = normalizePath_(path);
  var prefix = INTERNAL_PREFIX + "/";

  if (normalized.indexOf(prefix) !== 0) {
    return null;
  }

  return normalized.slice(prefix.length);
}

/**
 * @param {GoogleAppsScript.Events.DoGet|GoogleAppsScript.Events.DoPost} e
 * @returns {string|null}
 */
function resolveRequestPath_(e) {
  if (e.pathInfo) {
    return e.pathInfo;
  }

  var params = e.parameter || {};

  if (params.path) {
    return params.path;
  }

  if (params.route) {
    return params.route;
  }

  return null;
}

/**
 * @param {string} path
 * @returns {string}
 */
function normalizePath_(path) {
  return String(path).replace(/^\/+/, "").replace(/\/+$/, "");
}

/**
 * @param {GoogleAppsScript.Events.DoGet|GoogleAppsScript.Events.DoPost} e
 * @param {string} defaultMethod
 * @returns {{ url: string, method: string, headers: Object.<string, string>, body: (string|null), contentType: (string|null) }}
 */
function parseRequest_(e, defaultMethod) {
  var params = e.parameter || {};
  var method = String(params.method || defaultMethod).toUpperCase();
  var headers = {};
  var body = null;
  var contentType = null;

  if (params.headers) {
    headers = JSON.parse(params.headers);
  }

  if (e.postData && e.postData.contents) {
    var postType = String(e.postData.type || "").toLowerCase();

    if (postType.indexOf("application/json") !== -1) {
      var json = JSON.parse(e.postData.contents);
      if (json.url) {
        params.url = json.url;
      }
      if (json.method) {
        method = String(json.method).toUpperCase();
      }
      if (json.headers) {
        headers = json.headers;
      }
      if (json.body != null) {
        body = typeof json.body === "string" ? json.body : JSON.stringify(json.body);
        contentType = "application/json";
      }
    } else {
      body = e.postData.contents;
      contentType = e.postData.type || null;
    }
  }

  if (!ALLOWED_METHODS.includes(method)) {
    throw new Error("Method not allowed: " + method);
  }

  return {
    url: params.url,
    method: method,
    headers: headers,
    body: body,
    contentType: contentType,
  };
}

/**
 * @param {string} url
 */
function assertUrlAllowed_(url) {
  var parsed = parseHttpUrl_(url);

  if (!parsed) {
    throw new Error("Invalid url: " + url);
  }

  var allowlist = getAllowlist_();
  if (allowlist.length === 0) {
    return;
  }

  var host = parsed.hostname;
  var allowed = allowlist.some(function (entry) {
    entry = entry.toLowerCase();
    return host === entry || host.endsWith("." + entry);
  });

  if (!allowed) {
    throw new Error("Host not allowed: " + host);
  }
}

/**
 * @param {string} url
 * @returns {{ hostname: string }|null}
 */
function parseHttpUrl_(url) {
  var value = String(url).trim();
  var match = value.match(/^https?:\/\/([^\/?#:]+)(?::\d+)?/i);

  if (!match) {
    return null;
  }

  return {
    hostname: match[1].toLowerCase(),
  };
}

/**
 * @returns {string[]}
 */
function getAllowlist_() {
  var raw = PropertiesService.getScriptProperties().getProperty("ALLOWED_HOSTS");
  if (!raw) {
    return [];
  }

  return raw.split(",").map(function (entry) {
    return entry.trim();
  }).filter(Boolean);
}

/**
 * @param {GoogleAppsScript.URL_Fetch.HTTPResponse} response
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function writeUpstream_(response) {
  var status = response.getResponseCode();
  var contentType = response.getHeaders()["Content-Type"] || "text/plain";
  var body = response.getContentText();
  var error = status >= 400 ? "Upstream request failed" : null;

  return writeEnvelope_(status, contentType, body, error);
}

/**
 * @param {number} status
 * @param {string} message
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function writeError_(status, message) {
  return writeEnvelope_(
    status,
    "application/json",
    JSON.stringify({ error: message, status: status }),
    message
  );
}

/**
 * @param {number} status
 * @param {string} contentType
 * @param {string} body
 * @param {string|null} [error]
 * @returns {GoogleAppsScript.HTML.HtmlOutput}
 */
function writeEnvelope_(status, contentType, body, error) {
  var envelope = {
    v: ENVELOPE_VERSION,
    status: status,
    contentType: contentType,
    body: Utilities.base64Encode(String(body), Utilities.Charset.UTF_8),
  };

  if (error) {
    envelope.error = error;
  }

  var html = [
    "<div id=\"", ENVELOPE_ID, "\">",
    escapeHtmlText_(JSON.stringify(envelope)),
    "</div>",
  ].join("");

  return HtmlService.createHtmlOutput(html)
    .setXFrameOptionsMode(HtmlService.XFrameOptionsMode.ALLOWALL);
}

/**
 * @param {string} value
 * @returns {string}
 */
function escapeHtmlText_(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}
