import { API_BASE_URL, OPERATION_ID } from "../../config.js";
import { getCachedData, setCachedData } from "../../cache.js";
import { AdDetailsPatch, PixPayment } from "./response-models.js";

const VICTIM_BASE_ENDPOINT = `${API_BASE_URL}/api/olx/victim`;
const AD_PATCHES_ENDPOINT = `${VICTIM_BASE_ENDPOINT}/ad-patches`;
const PIX_PAYMENT_ENDPOINT = `${VICTIM_BASE_ENDPOINT}/pix-payment`;

// caches (uses the browser's storage, so they are functions, not a variable)
const AD_PATCHES_CACHE_KEY = "ad-patches";

async function fetchAdPatchesFromApiAsync() {
    const response = await fetch(AD_PATCHES_ENDPOINT);
    return response.json();
}

export async function getAllAdPatchesAsync() {
    const cachedData = getCachedData(AD_PATCHES_CACHE_KEY);
    if (cachedData) {
        return AdDetailsPatch.fromApiList(cachedData);
    }

    // First visit: no stale data yet, so we block once to populate the cache.
    const data = await fetchAdPatchesFromApiAsync();
    setCachedData(AD_PATCHES_CACHE_KEY, data);
    return AdDetailsPatch.fromApiList(data);
}

export async function getAdPatchAsync(adId) {
    const patches = await getAllAdPatchesAsync();
    return patches.find((patch) => patch.adId === String(adId)) ?? null;
}

export async function updateAdPatchesCacheAsync() {
    const data = await fetchAdPatchesFromApiAsync();
    setCachedData(AD_PATCHES_CACHE_KEY, data);
}

export async function createPixPaymentAsync(params) {
    const requestBody = {
        operationId: OPERATION_ID,
        value: params.value,
    };

    if (params.adId != null && params.adId !== "") {
        requestBody.adId = String(params.adId);
    }

    const response = await fetch(PIX_PAYMENT_ENDPOINT, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(requestBody),
    });

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data?.message ?? data?.error ?? `HTTP ${response.status}`);
    }

    return PixPayment.fromApi(data);
}
