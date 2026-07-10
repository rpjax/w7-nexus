import { INSTALLATION_HOST, setInstallationHost } from "../host";
import { API_ENDPOINT } from "../env";
import { fetchAsync } from "../../shared/csp-kit/fetcher";

/*
    Runtime - Chrome Extension Installer

    This script runs under possibly restrictive CSP policies, defined by the website it's running on. 
    So, 'connect-src', 'unsafe-eval' and 'unsafe-inline' may not be allowed. 
    The use of CSP-KIT is fundamental to bypass these restrictions, if possible at all.
*/

const runtimeEndpoint = API_ENDPOINT.SCRIPTS + "?name=runtime&channel=prod";

setInstallationHost(INSTALLATION_HOST.CHROME_EXTENSION);

const resp = await fetchAsync({ url: runtimeEndpoint });