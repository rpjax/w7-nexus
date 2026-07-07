#!/usr/bin/env node
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { runDiscoverBuildCli } from "./tools/bundler.mjs";

runDiscoverBuildCli({
    rootDir: dirname(fileURLToPath(import.meta.url)),
    argv: process.argv.slice(2),
});
