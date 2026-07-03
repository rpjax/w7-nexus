import { INSTALLER_ENDPOINT } from "./config.js";
import { importModule } from "./remote.js";

importModule(INSTALLER_ENDPOINT).then((module) => module.i());
