import { getCheckoutSummary } from "../getters.js";
import { CheckoutSummary } from "../models.js";
import { setCheckoutSummary } from "../setters.js";

const WARRANTY_RATE = 0.05;
const DELIVERY_LABEL = "Entrega";

function buildPatchedCheckoutSummary(originalSummary, adPatch) {
    const productPrice = adPatch.currentPrice;
    const warrantyValue = productPrice * WARRANTY_RATE;

    return CheckoutSummary.fromParsed(
        productPrice,
        originalSummary.extraCosts.map((cost) => {
            if (cost.name === DELIVERY_LABEL) {
                return {
                    name: cost.name,
                    value: cost.value,
                    discountValue: cost.discountValue,
                };
            }

            return {
                name: cost.name,
                value: warrantyValue,
                discountValue: warrantyValue,
            };
        })
    );
}

function patchCheckoutSummaryValues(adPatch) {
    const originalSummary = getCheckoutSummary();
    setCheckoutSummary(buildPatchedCheckoutSummary(originalSummary, adPatch));
}

export function patchCheckoutSummary(adPatch) {
    patchCheckoutSummaryValues(adPatch);
}
