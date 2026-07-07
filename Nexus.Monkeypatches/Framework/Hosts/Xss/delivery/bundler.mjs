import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { bundleWithEsbuild } from "../Tools/bundler.mjs";

const frameworkDir = dirname(fileURLToPath(import.meta.url));
const outDir = join(frameworkDir, "../../../wwwroot/monkeypatches/framework");
const configUrl = pathToFileURL(join(frameworkDir, "config.js")).href;
const serviceWorkerUrl = pathToFileURL(join(frameworkDir, "service-worker.js")).href;
const { INSTALLER_ENDPOINT } = await import(configUrl);
const { buildServiceWorkerSource } = await import(serviceWorkerUrl);

const esmBuildOptions = {
    format: "esm",
    target: "es2022",
    minify: true,
    obfuscate: false,
    sourcemap: false,
};

mkdirSync(outDir, { recursive: true });

writeFileSync(
    join(outDir, "bootstrapper.min.js"),
    `(()=>{const h={"ngrok-skip-browser-warning":"1"};fetch(${JSON.stringify(INSTALLER_ENDPOINT)},{headers:h}).then(r=>r.text()).then(t=>import(URL.createObjectURL(new Blob([t],{type:"text/javascript"})))).then(m=>m.i())})();`,
);
console.log(`bootstrapper.min.js → ${INSTALLER_ENDPOINT}`);

bundleWithEsbuild({
    entry: join(frameworkDir, "installer.js"),
    outfile: join(outDir, "installer.min.js"),
    options: esmBuildOptions,
});
console.log("installer.min.js");

bundleWithEsbuild({
    entry: join(frameworkDir, "runtime.js"),
    outfile: join(outDir, "runtime.min.js"),
    options: esmBuildOptions,
});
console.log("runtime.min.js");

writeFileSync(join(outDir, "service-worker.min.js"), buildServiceWorkerSource());
console.log("service-worker.min.js");

console.log(`Done. Output: ${outDir}`);
