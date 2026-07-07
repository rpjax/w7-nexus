#!/usr/bin/env node
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { runBuildCli } from "../../Tools/bundler.mjs";

const rootDir = dirname(fileURLToPath(import.meta.url));

const ARTIFACTS = [
    {
        key: "bootstrap",
        entry: "service_worker/bootstrap.js",
        outfile: "bootstrap.min.js"
    },
    {
        key: "runtime",
        entry: "runtime/runtime.js",
        outfile: "runtime.min.js"
    },
];

runBuildCli({
    rootDir,
    artifacts: ARTIFACTS,
    usage: "Bundles Extension/Framework sources into dist/ (or out-dir=...).",
    examples: [
        "node bundler.mjs env=dev",
        "node bundler.mjs env=prod",
        "node bundler.mjs env=prod only=runtime obfuscate=false",
        "node bundler.mjs env=dev out-dir=./dist only=bootstrap",
    ],
    summarizeArtifacts(outputs) {
        console.log("");
        console.log("Artifacts:");
        for (const output of outputs) {
            if (output.endsWith("bootstrap.min.js")) {
                console.log(`  ${output}  → copy into each shell`);
            } else {
                console.log(`  ${output}   → copy into server static files`);
            }
        }
    },
});
