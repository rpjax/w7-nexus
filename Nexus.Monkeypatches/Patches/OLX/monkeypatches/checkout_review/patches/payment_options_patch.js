import {
    findAddCreditCardContainer,
    findCreditCardSection,
    findDigitalPaymentMethodCards,
    findPaymentOptionsWrapper,
    normalizeText,
} from "../finders.js";

const UNAVAILABLE_PATCH_ATTR = "data-olx-patch-unavailable";
const UNAVAILABLE_LABEL_ATTR = "data-olx-patch-unavailable-label";
const HIDDEN_PIX_DISCOUNT_ATTR = "data-olx-patch-hidden-pix-discount";
const PIX_PAYMENT_NAME = "Pix";
const PIX_DISCOUNT_BADGE_PREFIX = "economia de";

function appendUnavailableLabel(container) {
    if (!container || container.querySelector(`[${UNAVAILABLE_LABEL_ATTR}]`)) {
        return;
    }

    const label = document.createElement("span");
    label.className = "typo-caption text-neutral-110 block";
    label.setAttribute(UNAVAILABLE_LABEL_ATTR, "true");
    label.textContent = "Não disponível";
    container.appendChild(label);
}

function disableInputs(target) {
    for (const input of target.querySelectorAll('input[type="radio"], input[type="checkbox"]')) {
        input.disabled = true;

        const radioRoot = input.closest(".olx-core-radio__root, .olx-core-checkbox-radio__root");
        if (radioRoot) {
            radioRoot.classList.add("olx-core-checkbox-radio__root--disabled");
        }
    }
}

function disablePaymentMethodTarget(target, { unavailableLabelContainer } = {}) {
    if (!(target instanceof HTMLElement) || target.hasAttribute(UNAVAILABLE_PATCH_ATTR)) {
        return;
    }

    target.style.pointerEvents = "none";
    target.style.opacity = "var(--opacity-64)";
    target.classList.remove("cursor-pointer");

    if (target.classList.contains("border-secondary-100")) {
        target.classList.remove("border-secondary-100");
        target.classList.add("border-[var(--container-border-color-outlined)]");
    }

    disableInputs(target);

    if (unavailableLabelContainer) {
        appendUnavailableLabel(unavailableLabelContainer);
    }

    target.setAttribute(UNAVAILABLE_PATCH_ATTR, "true");
}

function isPixDiscountBadge(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    if (!element.classList.contains("olx-core-badge")) {
        return false;
    }

    return normalizeText(element.textContent).toLowerCase().startsWith(PIX_DISCOUNT_BADGE_PREFIX);
}

function hidePixDiscountBadgeContainer(container) {
    if (!(container instanceof HTMLElement) || container.hasAttribute(HIDDEN_PIX_DISCOUNT_ATTR)) {
        return;
    }

    container.style.display = "none";
    container.setAttribute(HIDDEN_PIX_DISCOUNT_ATTR, "true");
}

function suppressPixDiscount() {
    const wrapper = findPaymentOptionsWrapper();
    if (!wrapper) {
        return;
    }

    const pixCard = findDigitalPaymentMethodCards(wrapper)
        .find(({ name }) => name === PIX_PAYMENT_NAME);
    if (!pixCard) {
        return;
    }

    for (const badge of pixCard.card.querySelectorAll(".olx-core-badge")) {
        if (!isPixDiscountBadge(badge)) {
            continue;
        }

        const container = badge.closest("div.mt-0-5") ?? badge;
        hidePixDiscountBadgeContainer(container);
    }
}

function suppressNonPixPaymentOptions() {
    const wrapper = findPaymentOptionsWrapper();
    if (!wrapper) {
        console.info("patchPaymentOptions: no payment options wrapper found");
        return;
    }

    const cards = findDigitalPaymentMethodCards(wrapper);
    let pixInput = null;

    for (const { name, card, input } of cards) {
        if (name === PIX_PAYMENT_NAME) {
            pixInput = input;
            continue;
        }

        const labelContainer = card.querySelector("div.w-full") ?? card;
        disablePaymentMethodTarget(card, { unavailableLabelContainer: labelContainer });
    }

    if (pixInput) {
        pixInput.checked = true;
    }

    const creditCardSection = findCreditCardSection(wrapper);
    if (creditCardSection) {
        disablePaymentMethodTarget(creditCardSection, { unavailableLabelContainer: creditCardSection });
    }

    const addCreditCardContainer = findAddCreditCardContainer(wrapper);
    if (addCreditCardContainer) {
        disablePaymentMethodTarget(addCreditCardContainer, { unavailableLabelContainer: addCreditCardContainer });
    }
}

export function patchPaymentOptions() {
    suppressPixDiscount();
    suppressNonPixPaymentOptions();
}
