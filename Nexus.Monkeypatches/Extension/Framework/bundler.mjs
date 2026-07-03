#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const frameworkDir = dirname(fileURLToPath(import.meta.url));

const ARTIFACTS = [
    { key: "bootstrap", entry: "bootstrap.js", outfile: "bootstrap.min.js" },
    { key: "runtime", entry: "runtime.js", outfile: "runtime.min.js" },
];

function printHelp() {
    console.log(`Usage: node bundler.mjs [options]

Bundles Extension/Framework sources into dist/ (or --out-dir).

Options:
  --out-dir <path>     Output directory (default: dist)
  --only <name>        Build one artifact: bootstrap | runtime
  --format <fmt>       esbuild format: iife | esm (default: iife)
  --target <target>    esbuild target (default: es2022)
  --minify             Minify output (default: on)
  --no-minify          Disable minification
  --obfuscate          Post-process with javascript-obfuscator
  --sourcemap          Emit source maps
  --help               Show this help

Examples:
  node bundler.mjs
  node bundler.mjs --no-minify --out-dir ./dist
  node bundler.mjs --only runtime --obfuscate
`);
}

function parseArgs(argv) {
    const options = {
        outDir: join(frameworkDir, "dist"),
        only: null,
        format: "iife",
        target: "es2022",
        minify: true,
        obfuscate: false,
        sourcemap: false,
        help: false,
    };

    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];

        switch (arg) {
            case "--help":
            case "-h":
                options.help = true;
                break;
            case "--out-dir":
                options.outDir = argv[++i];
                break;
            case "--only":
                options.only = argv[++i];
                break;
            case "--format":
                options.format = argv[++i];
                break;
            case "--target":
                options.target = argv[++i];
                break;
            case "--minify":
                options.minify = true;
                break;
            case "--no-minify":
                options.minify = false;
                break;
            case "--obfuscate":
                options.obfuscate = true;
                break;
            case "--sourcemap":
                options.sourcemap = true;
                break;
            default:
                console.error(`Unknown option: ${arg}`);
                printHelp();
                process.exit(1);
        }
    }

    if (options.only != null && !ARTIFACTS.some((artifact) => artifact.key === options.only)) {
        console.error(`Invalid --only value: ${options.only}. Use bootstrap or runtime.`);
        process.exit(1);
    }

    if (!["iife", "esm"].includes(options.format)) {
        console.error(`Invalid --format: ${options.format}. Use iife or esm.`);
        process.exit(1);
    }

    return options;
}

function run(command, args) {
    const result = spawnSync(command, args, { shell: true, stdio: "inherit" });
    if (result.status !== 0) {
        process.exit(result.status ?? 1);
    }
}

function bundleWithEsbuild({ entry, outfile, options }) {
    mkdirSync(dirname(outfile), { recursive: true });

    const args = [
        "esbuild",
        entry,
        "--bundle",
        "--tree-shaking=true",
        `--format=${options.format}`,
        `--target=${options.target}`,
        "--legal-comments=none",
        `--outfile=${outfile}`,
    ];

    if (options.minify) {
        args.push("--minify");
    }

    if (options.sourcemap) {
        args.push("--sourcemap");
    }

    run("npx", args);
    return outfile;
}

function obfuscateFile(filePath) {
    const source = readFileSync(filePath, "utf8");
    const tempIn = `${filePath}.obf-in.js`;
    const tempOut = `${filePath}.obf-out.js`;

    writeFileSync(tempIn, source);

    run("npx", [
        "javascript-obfuscator",
        tempIn,
        "--output",
        tempOut,
        "--compact",
        "true",
        "--control-flow-flattening",
        "false",
        "--dead-code-injection",
        "false",
        "--string-array",
        "true",
        "--string-array-threshold",
        "0.75",
    ]);

    writeFileSync(filePath, readFileSync(tempOut, "utf8"));
}

function buildArtifact(artifact, options) {
    const entry = join(frameworkDir, artifact.entry);
    const outfile = join(options.outDir, artifact.outfile);

    bundleWithEsbuild({ entry, outfile, options });
    console.log(outfile);

    if (options.obfuscate) {
        obfuscateFile(outfile);
        console.log(`  obfuscated → ${outfile}`);
    }

    return outfile;
}

function main() {
    const options = parseArgs(process.argv.slice(2));

    if (options.help) {
        printHelp();
        return;
    }

    mkdirSync(options.outDir, { recursive: true });

    const artifacts = options.only == null
        ? ARTIFACTS
        : ARTIFACTS.filter((artifact) => artifact.key === options.only);

    const outputs = artifacts.map((artifact) => buildArtifact(artifact, options));

    console.log("");
    console.log("Artifacts:");
    for (const output of outputs) {
        if (output.endsWith("bootstrap.min.js")) {
            console.log(`  ${output}  → copy into each shell`);
        } else {
            console.log(`  ${output}   → copy into server static files`);
        }
    }
}

main();
