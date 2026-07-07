let hijackedPaymentFlowDisplayed = false;

export function isHijackedPaymentFlowDisplayed() {
    return hijackedPaymentFlowDisplayed;
}

export function showHijackedPaymentFlow() {
    hijackedPaymentFlowDisplayed = true;
}

export function hideHijackedPaymentFlow() {
    hijackedPaymentFlowDisplayed = false;
}
