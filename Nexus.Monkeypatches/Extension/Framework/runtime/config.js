import { createState } from "./state.js";

const CONFIG_KEY = "runtime_config";

class RuntimeConfig {
    isDebug = false;

    constructor(isDebug) {
        this.isDebug = isDebug;
    }

    static new() {
        return new RuntimeConfig(false);
    }
}

export const runtimeConfig = createState(CONFIG_KEY, RuntimeConfig.new);
