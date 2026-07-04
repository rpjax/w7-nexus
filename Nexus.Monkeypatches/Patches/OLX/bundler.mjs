#!/usr/bin/env node
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { runBuildCli } from "../../Tools/bundler.mjs";

const rootDir = dirname(fileURLToPath(import.meta.url));

const ARTIFACTS = [
    { key: "patch", entry: "main.js", outfile: "olx.min.js" },
];

runBuildCli({
    rootDir,
    artifacts: ARTIFACTS,
    usage: "Bundles Patches/OLX sources into dist/ (or out-dir=...).",
    examples: [
        "node bundler.mjs env=dev",
        "node bundler.mjs env=prod",
        "node bundler.mjs env=dev sourcemap=true",
        "node bundler.mjs env=prod obfuscation=max",
    ],
    summarizeArtifacts(outputs) {
        console.log("");
        console.log("Artifacts:");
        for (const output of outputs) {
            console.log(`  ${output}  → copy into server static files`);
            console.log("             e.g. LocalServer/wwwroot/monkeypatches/patches/olx.min.js");
        }
    },
});
