#!/usr/bin/env node
import { spawn } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const deployDir = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(deployDir, "..");
const SCRIPT_NAME = "deploy.mjs";
const TAIL_LINES = 24;

// ---------------------------------------------------------------------------
// Logger
// ---------------------------------------------------------------------------

const useColor = process.stdout.isTTY && !process.env.NO_COLOR;

const palette = {
  reset: useColor ? "\x1b[0m" : "",
  dim: useColor ? "\x1b[2m" : "",
  bold: useColor ? "\x1b[1m" : "",
  cyan: useColor ? "\x1b[36m" : "",
  green: useColor ? "\x1b[32m" : "",
  yellow: useColor ? "\x1b[33m" : "",
  red: useColor ? "\x1b[31m" : "",
  magenta: useColor ? "\x1b[35m" : "",
};

const levelStyles = {
  INFO: palette.cyan,
  OK: palette.green,
  WARN: palette.yellow,
  ERROR: palette.red,
  STEP: palette.magenta,
  DEBUG: palette.dim,
};

class Logger {
  constructor() {
    this.startedAt = Date.now();
    this.records = [];
  }

  timestamp() {
    return new Date().toISOString().slice(11, 23);
  }

  write(level, phase, message, { detail } = {}) {
    const style = levelStyles[level] ?? "";
    const phaseLabel = phase ? `${palette.bold}[${phase}]${palette.reset} ` : "";
    const line = `${palette.dim}${this.timestamp()}${palette.reset} ${style}${level.padEnd(5)}${palette.reset} ${phaseLabel}${message}`;
    console.log(line);

    if (detail) {
      for (const row of detail.split("\n")) {
        console.log(`${palette.dim}       │ ${row}${palette.reset}`);
      }
    }

    this.records.push({ level, phase, message, at: Date.now() });
  }

  info(phase, message, opts) {
    this.write("INFO", phase, message, opts);
  }

  ok(phase, message, opts) {
    this.write("OK", phase, message, opts);
  }

  warn(phase, message, opts) {
    this.write("WARN", phase, message, opts);
  }

  error(phase, message, opts) {
    this.write("ERROR", phase, message, opts);
  }

  step(phase, message) {
    this.write("STEP", phase, message);
  }

  banner() {
    console.log("");
    console.log(`${palette.bold}Nexus Deploy${palette.reset}  ${palette.dim}${SCRIPT_NAME}${palette.reset}`);
    console.log(`${palette.dim}────────────────────────────────────────${palette.reset}`);
  }

  section(title) {
    console.log("");
    console.log(`${palette.bold}── ${title} ──${palette.reset}`);
  }

  fatal(phase, message, { cause, hint, detail } = {}) {
    this.section("Deploy failed");
    this.error(phase, message, { detail });

    if (cause) {
      this.error(phase, `Cause: ${cause}`);
    }
    if (hint) {
      this.warn(phase, `Hint: ${hint}`);
    }

    const elapsed = ((Date.now() - this.startedAt) / 1000).toFixed(1);
    console.log(`${palette.dim}Elapsed: ${elapsed}s${palette.reset}`);
    process.exit(1);
  }

  summary({ env, namespace, built, pushed, artifacts, elapsedSec }) {
    this.section("Summary");
    this.ok("DONE", `Environment: ${env}`);
    this.ok("DONE", `Namespace:   ${namespace}`);
    if (built.length) {
      this.ok("DONE", `Built:       ${built.join(", ")}`);
    }
    if (pushed.length) {
      this.ok("DONE", `Pushed:      ${pushed.join(", ")}`);
    }
    for (const file of artifacts) {
      this.ok("DONE", `Artifact:    ${file}`);
    }
    this.ok("DONE", `Completed in ${elapsedSec}s`);
    console.log("");
    this.info(
      "DONE",
      `VPS: copy out/${env}/ then run: docker compose pull && docker compose up -d`,
    );
    console.log("");
  }
}

const log = new Logger();

// ---------------------------------------------------------------------------
// Errors
// ---------------------------------------------------------------------------

class DeployError extends Error {
  constructor(phase, message, { cause, hint, detail } = {}) {
    super(message);
    this.name = "DeployError";
    this.phase = phase;
    this.hint = hint;
    this.detail = detail;
    this.causeText = cause instanceof Error ? cause.message : cause;
  }
}

function fail(phase, message, opts = {}) {
  throw new DeployError(phase, message, opts);
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

function printHelp() {
  console.log(`Usage: node ${SCRIPT_NAME} env=<name> [only=<container-id>]

Run from the deploy/ directory:
  cd deploy
  node ${SCRIPT_NAME} env=prod

Root keys in nexus.config.json are environment names (e.g. dev, prod).

Phases:
  1. Preflight  — Docker daemon, working directory, Hub auth
  2. Config     — load and validate nexus.config.json
  3. Build      — docker build --build-arg ENV=<env>
  4. Push       — docker push <namespace>/<image>:<env>
  5. Generate   — out/<env>/docker-compose.yml + .env
  6. Validate   — docker compose config

Examples:
  node ${SCRIPT_NAME} env=prod
  node ${SCRIPT_NAME} env=dev only=backend
`);
}

function listEnvNames(config) {
  return Object.keys(config);
}

function isHelpRequest(argv) {
  return argv.some((arg) => arg === "help" || arg === "--help" || arg === "-h");
}

function parseArgs(argv, config) {
  const result = { env: null, only: null };
  const validEnvs = new Set(listEnvNames(config));

  for (const arg of argv) {
    if (arg.startsWith("env=")) {
      result.env = arg.slice("env=".length).trim();
    } else if (arg.startsWith("only=")) {
      result.only = arg.slice("only=".length).trim();
    } else {
      fail("CLI", `Unknown argument: ${arg}`, {
        hint: `Run: node ${SCRIPT_NAME} help`,
      });
    }
  }

  if (!result.env) {
    const available = [...validEnvs].join(" | ") || "(none)";
    fail("CLI", `Missing required argument env=<name>.`, {
      detail: `Available environments: ${available}`,
      hint: `Example: node ${SCRIPT_NAME} env=prod`,
    });
  }
  if (!validEnvs.has(result.env)) {
    const available = [...validEnvs].join(" | ") || "(none)";
    fail("CLI", `Unknown env="${result.env}".`, {
      detail: `Available environments: ${available}`,
    });
  }

  return result;
}

function assertDeployDirectory() {
  const cwd = resolve(process.cwd());
  if (cwd !== deployDir) {
    fail("INIT", "This script must be run from the deploy/ directory.", {
      detail: `Current directory: ${cwd}\nExpected:          ${deployDir}`,
      hint: `Run: cd deploy && node ${SCRIPT_NAME} env=prod`,
    });
  }
}

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

function loadConfig() {
  const configPath = join(deployDir, "nexus.config.json");
  if (!existsSync(configPath)) {
    fail("CONFIG", `Missing nexus.config.json.`, {
      detail: `Expected file: ${configPath}`,
      hint: "Run: cp nexus.config.example.json nexus.config.json",
    });
  }

  let raw;
  try {
    raw = readFileSync(configPath, "utf8");
  } catch (err) {
    fail("CONFIG", "Unable to read nexus.config.json.", { cause: err });
  }

  try {
    return JSON.parse(raw);
  } catch (err) {
    fail("CONFIG", "nexus.config.json is not valid JSON.", {
      cause: err,
      hint: "Validate the file with a JSON linter.",
    });
  }
}

function validateConfig(config) {
  if (!config || typeof config !== "object" || Array.isArray(config)) {
    fail("CONFIG", "nexus.config.json root must be an object.");
  }

  const envNames = listEnvNames(config);
  if (envNames.length === 0) {
    fail("CONFIG", "nexus.config.json must define at least one environment at the root.");
  }

  for (const env of envNames) {
    validateEnvironment(config, env);
  }

  log.ok("CONFIG", `Validated ${envNames.length} environment(s): ${envNames.join(", ")}`);
}

function validateEnvironment(config, env) {
  const environment = config[env];
  if (!environment || typeof environment !== "object" || Array.isArray(environment)) {
    fail("CONFIG", `"${env}" must be an object.`);
  }

  if (!environment.namespace?.trim()) {
    fail("CONFIG", `"${env}".namespace is required.`);
  }

  const containers = environment.containers;
  if (!Array.isArray(containers) || containers.length === 0) {
    fail("CONFIG", `"${env}".containers must be a non-empty array.`);
  }

  const ids = new Set();
  for (const container of containers) {
    if (!container.id?.trim()) {
      fail("CONFIG", `Every container in "${env}" needs a non-empty "id".`);
    }
    if (!container.image?.trim()) {
      fail("CONFIG", `Container "${container.id}" in "${env}" needs an "image" name.`);
    }
    if (ids.has(container.id)) {
      fail("CONFIG", `Duplicate container id "${container.id}" in "${env}".`);
    }
    ids.add(container.id);

    if (container.dependsOn) {
      for (const dep of container.dependsOn) {
        if (!ids.has(dep) && !containers.some((c) => c.id === dep)) {
          fail(
            "CONFIG",
            `Container "${container.id}" depends on unknown service "${dep}" in "${env}".`,
          );
        }
      }
    }

    if (container.context?.trim()) {
      const contextPath = join(repoRoot, container.context);
      if (!existsSync(contextPath)) {
        fail("CONFIG", `Build context not found for "${container.id}".`, {
          detail: `Missing path: ${contextPath}`,
        });
      }
      const dockerfile = join(contextPath, container.dockerfile ?? "Dockerfile");
      if (!existsSync(dockerfile)) {
        fail("CONFIG", `Dockerfile not found for "${container.id}".`, {
          detail: `Missing file: ${dockerfile}`,
        });
      }
    }
  }
}

function getEnvironment(config, env) {
  const environment = config[env];
  const network = environment.network?.trim() || "nexus";
  return {
    namespace: environment.namespace.trim(),
    network,
    containers: environment.containers,
  };
}

// ---------------------------------------------------------------------------
// Preflight
// ---------------------------------------------------------------------------

function tailText(text, lines = TAIL_LINES) {
  if (!text?.trim()) {
    return "";
  }
  return text.trimEnd().split("\n").slice(-lines).join("\n");
}

function runCommand(command, args, { phase, label, inherit = true } = {}) {
  const cmdLine = [command, ...args].join(" ");
  log.step(phase, label ?? cmdLine);

  return new Promise((resolvePromise, rejectPromise) => {
    const child = spawn(command, args, {
      shell: process.platform === "win32",
      env: process.env,
      cwd: deployDir,
    });

    let stdout = "";
    let stderr = "";

    child.stdout?.on("data", (chunk) => {
      const text = chunk.toString();
      stdout += text;
      if (inherit) {
        process.stdout.write(chunk);
      }
    });

    child.stderr?.on("data", (chunk) => {
      const text = chunk.toString();
      stderr += text;
      if (inherit) {
        process.stderr.write(chunk);
      }
    });

    child.on("error", (err) => {
      rejectPromise(
        new DeployError(phase, `Failed to start command: ${cmdLine}`, {
          cause: err,
          detail: err.code === "ENOENT" ? `Is "${command}" installed and on PATH?` : undefined,
        }),
      );
    });

    child.on("close", (code, signal) => {
      if (code === 0) {
        resolvePromise({ stdout, stderr });
        return;
      }

      const tail = tailText(stderr || stdout);
      rejectPromise(
        new DeployError(phase, `Command failed (${label ?? command}).`, {
          detail: [
            `Command: ${cmdLine}`,
            `Exit code: ${code ?? "unknown"}`,
            signal ? `Signal: ${signal}` : null,
            tail ? `Last output:\n${tail}` : "No output captured.",
          ]
            .filter(Boolean)
            .join("\n"),
        }),
      );
    });
  });
}

async function preflight(namespace) {
  log.section("Preflight");

  await runCommand("docker", ["version"], { phase: "PREFLIGHT", label: "Docker client/server" });
  log.ok("PREFLIGHT", "Docker is available.");

  const info = await runCommand("docker", ["info", "--format", "{{.ServerVersion}}"], {
    phase: "PREFLIGHT",
    label: "Docker daemon",
    inherit: false,
  });
  log.ok("PREFLIGHT", `Docker daemon reachable (server ${info.stdout.trim()}).`);

  const dockerConfig = join(homedir(), ".docker", "config.json");
  if (!existsSync(dockerConfig)) {
    log.warn(
      "PREFLIGHT",
      "Docker config not found — push may fail if you are not logged in.",
      {
        detail: `Expected: ${dockerConfig}`,
      },
    );
  } else {
    try {
      const cfg = JSON.parse(readFileSync(dockerConfig, "utf8"));
      const auths = cfg.auths ?? {};
      const hasHubAuth = Object.keys(auths).some((k) => k.includes("docker.io"));
      if (hasHubAuth) {
        log.ok("PREFLIGHT", "Docker Hub credentials found.");
      } else {
        log.warn("PREFLIGHT", "No Docker Hub credentials found.", {
          detail: `Push target namespace: ${namespace}`,
          hint: "Run: docker login",
        });
      }
    } catch {
      log.warn("PREFLIGHT", "Could not parse Docker config — skipping Hub auth check.");
    }
  }

  log.ok("PREFLIGHT", `Working directory: ${deployDir}`);
  log.ok("PREFLIGHT", `Repository root:   ${repoRoot}`);
}

// ---------------------------------------------------------------------------
// Docker build / push
// ---------------------------------------------------------------------------

function imageTag(namespace, container, env) {
  return `${namespace}/${container.image}:${env}`;
}

function hasBuildContext(container) {
  return Boolean(container.context?.trim());
}

async function buildContainer(namespace, container, env) {
  if (!hasBuildContext(container)) {
    log.info("BUILD", `Skipping "${container.id}" — no build context.`);
    return null;
  }

  const context = join(repoRoot, container.context);
  const dockerfileName = container.dockerfile ?? "Dockerfile";
  const dockerfile = join(context, dockerfileName);
  const tag = imageTag(namespace, container, env);

  log.section(`Build: ${container.id}`);
  log.info("BUILD", `Image:  ${tag}`);
  log.info("BUILD", `Context: ${context}`);

  await runCommand(
    "docker",
    ["build", "--build-arg", `ENV=${env}`, "-t", tag, "-f", dockerfile, context],
    { phase: "BUILD", label: `docker build ${container.id}` },
  );

  log.ok("BUILD", `Built ${tag}`);
  return tag;
}

async function pushContainer(namespace, container, env) {
  if (!hasBuildContext(container)) {
    return null;
  }

  const tag = imageTag(namespace, container, env);
  log.section(`Push: ${container.id}`);
  log.info("PUSH", `Image: ${tag}`);

  await runCommand("docker", ["push", tag], {
    phase: "PUSH",
    label: `docker push ${container.id}`,
  });

  log.ok("PUSH", `Pushed ${tag}`);
  return tag;
}

// ---------------------------------------------------------------------------
// Compose generation
// ---------------------------------------------------------------------------

function yamlList(items, indent) {
  return items.map((item) => `${indent}- ${item}`).join("\n");
}

function collectNamedVolumes(containers) {
  const names = new Set();
  for (const container of containers) {
    for (const volume of container.volumes ?? []) {
      if (volume.name) {
        names.add(volume.name);
      }
    }
  }
  return [...names];
}

function renderContainerService(container, network) {
  const lines = [
    `  ${container.id}:`,
    `    image: \${DOCKER_NAMESPACE}/${container.image}:\${DOCKER_TAG}`,
    `    container_name: ${container.id}`,
    "    restart: unless-stopped",
  ];

  if (container.ports?.length) {
    lines.push("    ports:");
    for (const port of container.ports) {
      lines.push(`      - "${port.host}:${port.container}"`);
    }
  }

  if (container.expose?.length) {
    lines.push("    expose:");
    for (const port of container.expose) {
      lines.push(`      - "${port}"`);
    }
  }

  if (container.env?.length) {
    lines.push("    environment:");
    for (const entry of container.env) {
      const value = JSON.stringify(String(entry.value ?? ""));
      lines.push(`      ${entry.name}: ${value}`);
    }
  }

  if (container.volumes?.length) {
    lines.push("    volumes:");
    for (const volume of container.volumes) {
      if (volume.name) {
        lines.push(`      - ${volume.name}:${volume.container}`);
      } else if (volume.host) {
        lines.push(`      - ${volume.host}:${volume.container}`);
      } else {
        fail("GENERATE", `Container "${container.id}" has a volume without name or host.`);
      }
    }
  }

  if (container.dependsOn?.length) {
    lines.push("    depends_on:");
    lines.push(yamlList(container.dependsOn, "      "));
  }

  lines.push("    networks:");
  lines.push(`      - ${network}`);

  return lines.join("\n");
}

function generateComposeArtifacts(config, env) {
  log.section("Generate artifacts");

  const { namespace, network, containers } = getEnvironment(config, env);
  const outDir = join(deployDir, "out", env);
  mkdirSync(outDir, { recursive: true });

  const serviceBlocks = containers.map((container) =>
    renderContainerService(container, network),
  );

  const namedVolumes = collectNamedVolumes(containers);
  const volumesBlock =
    namedVolumes.length > 0
      ? `volumes:\n${namedVolumes.map((name) => `  ${name}:`).join("\n")}`
      : "";

  const compose = [
    "services:",
    serviceBlocks.join("\n\n"),
    "",
    "networks:",
    `  ${network}:`,
    `    name: ${network}`,
    "",
    volumesBlock,
  ]
    .filter((section, index, array) => section !== "" || index < array.length - 1)
    .join("\n")
    .trimEnd()
    .concat("\n");

  const envFile = [
    "# Generated by deploy.mjs — do not edit by hand",
    `# env=${env}`,
    `DOCKER_NAMESPACE=${namespace}`,
    `DOCKER_TAG=${env}`,
    "",
  ].join("\n");

  const composePath = join(outDir, "docker-compose.yml");
  const envPath = join(outDir, ".env");

  writeFileSync(composePath, compose, "utf8");
  writeFileSync(envPath, envFile, "utf8");

  log.ok("GENERATE", `Wrote ${composePath}`);
  log.ok("GENERATE", `Wrote ${envPath}`);

  return { composePath, envPath, outDir };
}

async function validateComposeArtifacts({ composePath, envPath }) {
  log.section("Validate compose");
  await runCommand(
    "docker",
    ["compose", "-f", composePath, "--env-file", envPath, "config"],
    { phase: "VALIDATE", label: "docker compose config", inherit: false },
  );
  log.ok("VALIDATE", "docker-compose.yml is valid.");
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const startedAt = Date.now();
  const argv = process.argv.slice(2);
  log.banner();

  assertDeployDirectory();

  if (isHelpRequest(argv)) {
    printHelp();
    return;
  }

  const config = loadConfig();
  validateConfig(config);

  const args = parseArgs(argv, config);
  const { namespace, containers } = getEnvironment(config, args.env);

  let buildTargets = containers;
  if (args.only) {
    buildTargets = containers.filter((container) => container.id === args.only);
    if (buildTargets.length === 0) {
      const available = containers.map((c) => c.id).join(", ");
      fail("CLI", `No container with id "${args.only}" in "${args.env}".`, {
        detail: `Available container ids: ${available}`,
      });
    }
  }

  log.info("INIT", `Environment: ${args.env}`);
  log.info("INIT", `Namespace:   ${namespace}`);
  log.info("INIT", `Targets:     ${buildTargets.map((c) => c.id).join(", ")}`);

  await preflight(namespace);

  const built = [];
  const pushed = [];

  for (const container of buildTargets) {
    const tag = await buildContainer(namespace, container, args.env);
    if (tag) built.push(container.id);
  }

  for (const container of buildTargets) {
    const tag = await pushContainer(namespace, container, args.env);
    if (tag) pushed.push(container.id);
  }

  const artifacts = generateComposeArtifacts(config, args.env);
  await validateComposeArtifacts(artifacts);

  const elapsedSec = ((Date.now() - startedAt) / 1000).toFixed(1);
  log.summary({
    env: args.env,
    namespace,
    built,
    pushed,
    artifacts: [artifacts.composePath, artifacts.envPath],
    elapsedSec,
  });
}

main().catch((err) => {
  if (err instanceof DeployError) {
    log.fatal(err.phase, err.message, {
      cause: err.causeText,
      hint: err.hint,
      detail: err.detail,
    });
  }

  log.fatal("RUNTIME", "Unexpected error.", {
    cause: err instanceof Error ? err.message : String(err),
    detail: err instanceof Error && err.stack ? err.stack.split("\n").slice(0, 6).join("\n") : undefined,
  });
});
