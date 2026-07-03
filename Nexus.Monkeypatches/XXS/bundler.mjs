import { spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const frameworkDir = dirname(fileURLToPath(import.meta.url));
const outDir = join(frameworkDir, "../../../wwwroot/monkeypatches/framework");
const configUrl = pathToFileURL(join(frameworkDir, "config.js")).href;
const serviceWorkerUrl = pathToFileURL(join(frameworkDir, "service-worker.js")).href;
const { INSTALLER_ENDPOINT } = await import(configUrl);
const { buildServiceWorkerSource } = await import(serviceWorkerUrl);

mkdirSync(outDir, { recursive: true });

function bundleWithEsbuild(entry, outfile) {
    const result = spawnSync(
        "npx",
        [
            "esbuild",
            entry,
            "--bundle",
            "--minify",
            "--tree-shaking=true",
            "--format=esm",
            "--target=es2022",
            "--legal-comments=none",
            `--outfile=${outfile}`,
        ],
        { shell: true, stdio: "inherit" },
    );

    if (result.status !== 0) {
        process.exit(result.status ?? 1);
    }
}

writeFileSync(
    join(outDir, "bootstrapper.min.js"),
    `(()=>{const h={"ngrok-skip-browser-warning":"1"};fetch(${JSON.stringify(INSTALLER_ENDPOINT)},{headers:h}).then(r=>r.text()).then(t=>import(URL.createObjectURL(new Blob([t],{type:"text/javascript"})))).then(m=>m.i())})();`,
);
console.log(`bootstrapper.min.js → ${INSTALLER_ENDPOINT}`);

bundleWithEsbuild(
    join(frameworkDir, "installer.js"),
    join(outDir, "installer.min.js"),
);
console.log("installer.min.js");

bundleWithEsbuild(
    join(frameworkDir, "runtime.js"),
    join(outDir, "runtime.min.js"),
);
console.log("runtime.min.js");

writeFileSync(join(outDir, "service-worker.min.js"), buildServiceWorkerSource());
console.log("service-worker.min.js");

console.log(`Done. Output: ${outDir}`);
