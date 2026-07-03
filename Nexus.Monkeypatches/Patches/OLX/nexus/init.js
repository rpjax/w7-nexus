import { updateAdPatchesCacheAsync } from "./victim_service/service.js";

export function initializeCaches() {
    // Stale-while-revalidate: on load/F5, patches read localStorage immediately
    // while this background fetch refreshes the cache for the next patch cycle.
    // setTimeout(0) yields so the first patch run can use stale data before the request starts.
    setTimeout(() => {
        void updateAdPatchesCacheAsync().catch((error) => {
            console.error("Failed to refresh ad patches cache:", error);
        });
    }, 0);
}