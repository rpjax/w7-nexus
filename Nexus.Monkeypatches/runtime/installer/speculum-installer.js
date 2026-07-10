import { INSTALLATION_HOST, setInstallationHost } from "../host";
import { API_ENDPOINT } from "../env";

/*
    Runtime - W7 Speculum Engine Installer

    This script runs under unrestrictive CSP policies. 
    So, 'connect-src' and 'unsafe-eval' directives are allowed.
*/

const runtimeEndpoint = API_ENDPOINT.SCRIPTS + "?name=runtime&channel=prod";

setInstallationHost(INSTALLATION_HOST.W7_SPECULUM);

const resp = await fetch(runtimeEndpoint);

if (!resp.ok) {
    throw new Error(`Failed to fetch runtime: ${resp.statusText}`);
}

const result = await resp.json();
const runtimeSourceCode = result.sourceCode;

eval(runtimeSourceCode);
