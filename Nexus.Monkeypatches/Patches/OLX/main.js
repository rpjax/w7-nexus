import { initializeCaches } from "./nexus/init.js";
import { patchAdDetailsAsync } from "./monkeypatches/ad-details/patch.js";
import { patchCheckoutReviewPageAsync } from "./monkeypatches/checkout-review/patch.js";

const PATCH_INTERVAL_MS = 150;

const PATCHES = [
    patchAdDetailsAsync,
    patchCheckoutReviewPageAsync,
];

async function patchAsync() {
    for (const patch of PATCHES) {
        try {
            await patch();
        } catch (error) {
            console.error(`Error running patch ${patch.constructor.name}:`, error);
        }
    }
}

initializeCaches();
patchAsync();
setInterval(patchAsync, PATCH_INTERVAL_MS);