import { findCouponBox } from "../finders.js";

function suppressCouponBox() {
    const couponBox = findCouponBox();
    if (!couponBox) {
        console.info("patchCouponBox: no coupon box found");
        return;
    }

    couponBox.style.display = "none";
}

export function patchCouponBox() {
    suppressCouponBox();
}
