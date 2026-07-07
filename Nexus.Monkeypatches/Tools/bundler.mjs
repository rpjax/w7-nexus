import { spawnSync } from "node:child_process";
import { copyFileSync, mkdirSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, isAbsolute, join, relative } from "node:path";
import { pathToFileURL } from "node:url";

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
            explicit.outDir = isAbsolute(value) ? value : join(rootDir, value);
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

// ── bundle.json discovery build ─────────────────────────────────────────────

export const DEFAULT_DIST_DIR = "dist";

const WALK_SKIP_DIRS = new Set(["node_modules", "dist", ".git"]);

/**
 * @param {string} rootDir
 * @returns {string[]}
 */
export function discoverBundleManifests(rootDir) {
    /** @type {string[]} */
    const manifests = [];

    function walk(dir) {
        for (const entry of readdirSync(dir, { withFileTypes: true })) {
            if (entry.isDirectory()) {
                if (!WALK_SKIP_DIRS.has(entry.name)) {
                    walk(join(dir, entry.name));
                }
                continue;
            }

            if (entry.isFile() && entry.name === "bundle.json") {
                manifests.push(join(dir, entry.name));
            }
        }
    }

    walk(rootDir);
    return manifests.sort();
}

/**
 * @param {string} manifestPath
 */
export function loadBundleManifest(manifestPath) {
    const raw = readFileSync(manifestPath, "utf8");
    const manifest = JSON.parse(raw);

    if (!Array.isArray(manifest.bundles)) {
        throw new Error(`${manifestPath}: "bundles" must be an array`);
    }

    for (const bundle of manifest.bundles) {
        if (typeof bundle.name !== "string" || bundle.name.length === 0) {
            throw new Error(`${manifestPath}: each bundle requires a non-empty "name"`);
        }

        const type = bundle.type ?? "esbuild";

        if (type === "esbuild") {
            if (typeof bundle.entry !== "string" || bundle.entry.length === 0) {
                throw new Error(`${manifestPath}: bundle "${bundle.name}" requires "entry"`);
            }

            if (bundle.outfile == null && !Array.isArray(bundle.outfiles)) {
                throw new Error(`${manifestPath}: bundle "${bundle.name}" requires "outfile" or "outfiles"`);
            }
        } else if (type === "generator") {
            if (typeof bundle.module !== "string" || bundle.module.length === 0) {
                throw new Error(`${manifestPath}: bundle "${bundle.name}" requires "module"`);
            }

            if (typeof bundle.export !== "string" || bundle.export.length === 0) {
                throw new Error(`${manifestPath}: bundle "${bundle.name}" requires "export"`);
            }

            if (typeof bundle.outfile !== "string" || bundle.outfile.length === 0) {
                throw new Error(`${manifestPath}: bundle "${bundle.name}" requires "outfile"`);
            }
        } else {
            throw new Error(`${manifestPath}: bundle "${bundle.name}" has unsupported type "${type}"`);
        }
    }

    return manifest;
}

/**
 * @param {string} manifestDir
 * @param {string} path
 */
export function resolveBundlePath(manifestDir, path) {
    return isAbsolute(path) ? path : join(manifestDir, path);
}

/**
 * @param {{ outfile?: string, outfiles?: string[] }} bundle
 * @param {string} distDir
 * @returns {string[]}
 */
function resolveOutfiles(bundle, distDir) {
    if (Array.isArray(bundle.outfiles)) {
        return bundle.outfiles.map((path) => resolveBundlePath(distDir, path));
    }

    return [resolveBundlePath(distDir, bundle.outfile)];
}

/**
 * @param {object} bundle
 * @param {string} manifestDir
 * @param {string} distDir
 * @param {ReturnType<typeof DEFAULT_BUILD_OPTIONS> & { only?: string | null }} options
 * @returns {string[]}
 */
export async function buildBundle(bundle, manifestDir, distDir, options) {
    const type = bundle.type ?? "esbuild";
    const outfiles = resolveOutfiles(bundle, distDir);
    const bundleOptions = {
        ...options,
        format: bundle.format ?? options.format,
    };

    if (type === "generator") {
        const modulePath = resolveBundlePath(manifestDir, bundle.module);
        const moduleUrl = pathToFileURL(modulePath).href;
        const loaded = await import(moduleUrl);
        const generator = loaded[bundle.export];

        if (typeof generator !== "function") {
            throw new Error(`Bundle "${bundle.name}": export "${bundle.export}" is not a function in ${bundle.module}`);
        }

        const source = generator();
        mkdirSync(dirname(outfiles[0]), { recursive: true });
        writeFileSync(outfiles[0], source);
        console.log(outfiles[0]);
        return outfiles;
    }

    const entry = resolveBundlePath(manifestDir, bundle.entry);
    const primaryOutfile = outfiles[0];

    bundleWithEsbuild({ entry, outfile: primaryOutfile, options: bundleOptions });
    console.log(primaryOutfile);

    if (bundleOptions.obfuscate) {
        obfuscateFile(primaryOutfile, bundleOptions.obfuscation);
        console.log(`  obfuscated (${bundleOptions.obfuscation}) → ${primaryOutfile}`);
    }

    for (let index = 1; index < outfiles.length; index++) {
        mkdirSync(dirname(outfiles[index]), { recursive: true });
        copyFileSync(primaryOutfile, outfiles[index]);

        if (bundleOptions.sourcemap) {
            const mapPath = `${primaryOutfile}.map`;
            const targetMapPath = `${outfiles[index]}.map`;
            copyFileSync(mapPath, targetMapPath);
        }

        console.log(outfiles[index]);
    }

    return outfiles;
}

function printDiscoverBuildHelp(bundleNames) {
    const onlyKeys = bundleNames.join(" | ");

    console.log(`Usage: node build.mjs [key=value ...]

Discovers every bundle.json under Nexus.Monkeypatches and builds declared artifacts.
Output paths in bundle.json are relative to ${DEFAULT_DIST_DIR}/ at the project root.

Presets:
  env=dev              minify=false, obfuscate=false, sourcemap=true
  env=prod             minify=true, obfuscate=true, obfuscation=max, sourcemap=false

Options (all key=value; explicit flags override env):
  help=true            Show this help
  only=<name>          Build one bundle: ${onlyKeys}
  format=iife|esm      esbuild format (default: iife)
  target=<target>      esbuild target (default: es2022)
  minify=true|false    Minify output
  obfuscate=true|false Post-process with javascript-obfuscator
  obfuscation=standard|max  Obfuscation intensity when obfuscate=true
  sourcemap=true|false Emit source maps

Examples:
  node build.mjs env=dev
  node build.mjs env=prod
  node build.mjs only=runtime env=dev
`);
}

function parseDiscoverBuildArgs(argv) {
    const baseOptions = {
        only: null,
        ...DEFAULT_BUILD_OPTIONS,
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
            console.error('Option "out-dir" is not supported by bundle.json discovery builds.');
            process.exit(1);
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

/**
 * @param {{ rootDir: string, argv?: string[] }} params
 */
export async function runDiscoverBuildCli({ rootDir, argv = process.argv.slice(2) }) {
    const distDir = join(rootDir, DEFAULT_DIST_DIR);
    const manifestPaths = discoverBundleManifests(rootDir);
    const bundleEntries = manifestPaths.flatMap((manifestPath) => {
        const manifestDir = dirname(manifestPath);
        const manifest = loadBundleManifest(manifestPath);
        const manifestLabel = relative(rootDir, manifestPath);

        return manifest.bundles.map((bundle) => ({
            bundle,
            manifestDir,
            manifestLabel,
        }));
    });

    const bundleNames = bundleEntries.map((entry) => entry.bundle.name);
    const parsed = parseDiscoverBuildArgs(argv);

    if (parsed.invalid) {
        printDiscoverBuildHelp(bundleNames);
        process.exit(1);
    }

    if (parsed.help) {
        printDiscoverBuildHelp(bundleNames);
        return;
    }

    const options = parsed;
    const selected = options.only == null
        ? bundleEntries
        : bundleEntries.filter((entry) => entry.bundle.name === options.only);

    if (options.only != null && selected.length === 0) {
        console.error(`Invalid only: ${options.only}. Use ${bundleNames.join(" or ")}.`);
        process.exit(1);
    }

    /** @type {string[]} */
    const outputs = [];

    for (const entry of selected) {
        console.log(`${entry.manifestLabel} → ${entry.bundle.name}`);
        const built = await buildBundle(entry.bundle, entry.manifestDir, distDir, options);
        outputs.push(...built);
    }

    console.log("");
    console.log(`Built ${outputs.length} artifact(s).`);
}
