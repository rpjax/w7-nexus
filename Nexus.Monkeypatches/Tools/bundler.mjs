import { spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";

export const DEFAULT_BUILD_OPTIONS = {
    format: "iife",
    target: "es2022",
    minify: true,
    obfuscate: false,
    obfuscation: "standard",
    sourcemap: false,
};

export const ENV_PRESETS = {
    dev: {
        minify: false,
        obfuscate: false,
        obfuscation: "standard",
        sourcemap: true,
    },
    prod: {
        minify: true,
        obfuscate: true,
        obfuscation: "max",
        sourcemap: false,
    },
};

const OBFUSCATION_PROFILES = {
    standard: [
        "--compact", "true",
        "--control-flow-flattening", "false",
        "--dead-code-injection", "false",
        "--string-array", "true",
        "--string-array-threshold", "0.75",
    ],
    max: [
        "--compact", "true",
        "--control-flow-flattening", "true",
        "--control-flow-flattening-threshold", "1",
        "--dead-code-injection", "true",
        "--dead-code-injection-threshold", "0.4",
        "--string-array", "true",
        "--string-array-threshold", "1",
        "--string-array-encoding", "rc4",
        "--split-strings", "true",
        "--split-strings-chunk-length", "5",
        "--unicode-escape-sequence", "true",
        "--identifier-names-generator", "hexadecimal",
    ],
};

const BOOLEAN_KEYS = new Set(["minify", "obfuscate", "sourcemap"]);
const STRING_KEYS = new Set(["format", "target", "only", "out-dir", "obfuscation", "env"]);

function parseBoolean(value, key) {
    if (value === "true") {
        return true;
    }
    if (value === "false") {
        return false;
    }
    console.error(`Invalid boolean for ${key}: ${value}. Use true or false.`);
    process.exit(1);
}

function parseKeyValueArg(arg) {
    const normalized = arg.startsWith("--") ? arg.slice(2) : arg;
    const separator = normalized.indexOf("=");

    if (separator === -1) {
        return null;
    }

    return [normalized.slice(0, separator), normalized.slice(separator + 1)];
}

function coerceOption(key, value) {
    if (BOOLEAN_KEYS.has(key)) {
        return parseBoolean(value, key);
    }

    if (STRING_KEYS.has(key)) {
        return value;
    }

    console.error(`Unknown option: ${key}`);
    process.exit(1);
}

export function run(command, args) {
    const result = spawnSync(command, args, { shell: true, stdio: "inherit" });
    if (result.status !== 0) {
        process.exit(result.status ?? 1);
    }
}

export function bundleWithEsbuild({ entry, outfile, options }) {
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

export function obfuscateFile(filePath, profile = "standard") {
    const profileArgs = OBFUSCATION_PROFILES[profile] ?? OBFUSCATION_PROFILES.standard;
    const source = readFileSync(filePath, "utf8");
    const tempIn = `${filePath}.obf-in.js`;
    const tempOut = `${filePath}.obf-out.js`;

    writeFileSync(tempIn, source);

    run("npx", [
        "javascript-obfuscator",
        tempIn,
        "--output",
        tempOut,
        ...profileArgs,
    ]);

    writeFileSync(filePath, readFileSync(tempOut, "utf8"));
}

export function buildArtifact({ rootDir, artifact, options }) {
    const entry = join(rootDir, artifact.entry);
    const outfile = join(options.outDir, artifact.outfile);

    bundleWithEsbuild({ entry, outfile, options });
    console.log(outfile);

    if (options.obfuscate) {
        obfuscateFile(outfile, options.obfuscation);
        console.log(`  obfuscated (${options.obfuscation}) → ${outfile}`);
    }

    return outfile;
}

export function printBuildHelp({ usage, artifacts, examples = [] }) {
    const onlyKeys = artifacts.map((artifact) => artifact.key).join(" | ");
    const exampleBlock = examples.length > 0
        ? `\nExamples:\n${examples.map((line) => `  ${line}`).join("\n")}\n`
        : "";

    console.log(`Usage: node bundler.mjs [key=value ...]

${usage}

Presets:
  env=dev              minify=false, obfuscate=false, sourcemap=true
  env=prod             minify=true, obfuscate=true, obfuscation=max, sourcemap=false

Options (all key=value; explicit flags override env):
  help=true            Show this help
  out-dir=<path>       Output directory (default: dist)
  only=<name>          Build one artifact: ${onlyKeys}
  format=iife|esm      esbuild format (default: iife)
  target=<target>      esbuild target (default: es2022)
  minify=true|false    Minify output
  obfuscate=true|false Post-process with javascript-obfuscator
  obfuscation=standard|max  Obfuscation intensity when obfuscate=true
  sourcemap=true|false Emit source maps
${exampleBlock}`);
}

export function parseBuildArgs(argv, { rootDir, artifacts, defaults = {} }) {
    const baseOptions = {
        outDir: join(rootDir, "dist"),
        only: null,
        ...DEFAULT_BUILD_OPTIONS,
        ...defaults,
    };

    const explicit = {};
    let envName = null;

    for (const arg of argv) {
        const parsed = parseKeyValueArg(arg);
        if (parsed == null) {
            console.error(`Invalid argument: ${arg}. Use key=value (e.g. minify=false).`);
            return { help: false, invalid: true };
        }

        const [key, value] = parsed;

        if (key === "help") {
            if (parseBoolean(value, key)) {
                return { help: true, invalid: false };
            }
            continue;
        }

        if (key === "env") {
            envName = value;
            continue;
        }

        if (key === "out-dir") {
            explicit.outDir = value;
            continue;
        }

        explicit[key] = coerceOption(key, value);
    }

    let options = { ...baseOptions };

    if (envName != null) {
        const preset = ENV_PRESETS[envName];
        if (preset == null) {
            console.error(`Invalid env: ${envName}. Use dev or prod.`);
            process.exit(1);
        }
        options = { ...options, ...preset };
    }

    options = { ...options, ...explicit };

    if (options.only != null && !artifacts.some((artifact) => artifact.key === options.only)) {
        console.error(`Invalid only: ${options.only}. Use ${artifacts.map((artifact) => artifact.key).join(" or ")}.`);
        process.exit(1);
    }

    if (!["iife", "esm"].includes(options.format)) {
        console.error(`Invalid format: ${options.format}. Use iife or esm.`);
        process.exit(1);
    }

    if (!["standard", "max"].includes(options.obfuscation)) {
        console.error(`Invalid obfuscation: ${options.obfuscation}. Use standard or max.`);
        process.exit(1);
    }

    return options;
}

export function runBuildCli({ rootDir, artifacts, usage, examples = [], summarizeArtifacts }) {
    const parsed = parseBuildArgs(process.argv.slice(2), { rootDir, artifacts });

    if (parsed.invalid) {
        printBuildHelp({ usage, artifacts, examples });
        process.exit(1);
    }

    if (parsed.help) {
        printBuildHelp({ usage, artifacts, examples });
        return;
    }

    const options = parsed;

    mkdirSync(options.outDir, { recursive: true });

    const selected = options.only == null
        ? artifacts
        : artifacts.filter((artifact) => artifact.key === options.only);

    const outputs = selected.map((artifact) => buildArtifact({ rootDir, artifact, options }));

    if (summarizeArtifacts) {
        summarizeArtifacts(outputs);
    }
}
