#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const patchDir = dirname(fileURLToPath(import.meta.url));

const ARTIFACTS = [
    { key: "patch", entry: "main.js", outfile: "olx.min.js" },
];

function printHelp() {
    console.log(`Usage: node bundler.mjs [options]

Bundles Patches/OLX sources into dist/ (or --out-dir).

Options:
  --out-dir <path>     Output directory (default: dist)
  --format <fmt>       esbuild format: iife | esm (default: iife)
  --target <target>    esbuild target (default: es2022)
  --minify             Minify output (default: on)
  --no-minify          Disable minification
  --obfuscate          Post-process with javascript-obfuscator
  --sourcemap          Emit source maps
  --help               Show this help

Examples:
  node bundler.mjs
  node bundler.mjs --no-minify
  node bundler.mjs --obfuscate --sourcemap
`);
}

function parseArgs(argv) {
    const options = {
        outDir: join(patchDir, "dist"),
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

function main() {
    const options = parseArgs(process.argv.slice(2));

    if (options.help) {
        printHelp();
        return;
    }

    mkdirSync(options.outDir, { recursive: true });

    const outputs = ARTIFACTS.map((artifact) => {
        const entry = join(patchDir, artifact.entry);
        const outfile = join(options.outDir, artifact.outfile);

        bundleWithEsbuild({ entry, outfile, options });
        console.log(outfile);

        if (options.obfuscate) {
            obfuscateFile(outfile);
            console.log(`  obfuscated → ${outfile}`);
        }

        return outfile;
    });

    console.log("");
    console.log("Artifacts:");
    for (const output of outputs) {
        console.log(`  ${output}  → copy into server static files`);
        console.log(`             e.g. LocalServer/wwwroot/monkeypatches/patches/olx.min.js`);
    }
}

main();
