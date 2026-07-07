import { findPreviousPriceWrapper, findCurrentPriceWrapper, findPriceBoxInstallmentParagraph, findInstallmentsModalList } from "./finders.js";
import { InstallmentsModalListItem } from "./models.js";

const PREVIOUS_PRICE_WRAPPER_HTML = '<div class="flex gap-1 items-center"><span class="typo-body-medium font-semibold text-neutral-100" style="text-decoration: line-through;"></span></div>';

const PREVIOUS_PRICE_SPAN_CLASSES = ["typo-body-medium", "font-semibold", "text-neutral-100"];
const CURRENT_PRICE_SPAN_SELECTOR = ".typo-display-large, .typo-title-medium";

function formatPrice(price) {
    return "R$ " + price;
}

function formatBrazilianPrice(price) {
    return Number(price).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

function restorePreviousPriceSpanStyles(span) {
    for (const className of PREVIOUS_PRICE_SPAN_CLASSES) {
        span.classList.add(className);
    }

    span.style.textDecoration = "line-through";
}

function hasPreviousPriceStyles(span) {
    const hasClasses = PREVIOUS_PRICE_SPAN_CLASSES.every((className) => span.classList.contains(className));
    const hasStrike = span.style.textDecoration.includes("line-through");

    return hasClasses && hasStrike;
}

function getPreviousPriceSpan(wrapper) {
    if (!wrapper.hasChildNodes()) {
        wrapper.innerHTML = PREVIOUS_PRICE_WRAPPER_HTML;
    }

    return wrapper.querySelector("span.typo-body-medium") ?? wrapper.querySelector("span");
}

function getCurrentPriceSpan(wrapper) {
    const styledSpan = wrapper.querySelector(CURRENT_PRICE_SPAN_SELECTOR);
    if (styledSpan) {
        return styledSpan;
    }

    const outerSpan = wrapper.querySelector("span");
    if (!outerSpan) {
        return null;
    }

    return outerSpan.querySelector("span") ?? outerSpan;
}

export function setPreviousPrice(price) {
    const wrapper = findPreviousPriceWrapper();
    if (!wrapper) {
        return;
    }

    const span = getPreviousPriceSpan(wrapper);
    if (!span) {
        return;
    }

    span.textContent = formatPrice(price);

    if (!hasPreviousPriceStyles(span)) {
        restorePreviousPriceSpanStyles(span);
    }
}

export function setCurrentPrice(price) {
    const wrapper = findCurrentPriceWrapper();
    if (!wrapper) {
        return;
    }

    const span = getCurrentPriceSpan(wrapper);
    if (!span) {
        return;
    }

    const originalClassName = span.className;

    span.textContent = formatPrice(price);
    span.className = originalClassName;
}

export function setPriceBoxInstallmentValue(value) {
    const installmentParagraph = findPriceBoxInstallmentParagraph();
    if (!installmentParagraph) {
        return;
    }

    const prefixMatch = installmentParagraph.textContent.match(/^(\d+x sem juros de R\$\s*)/i);
    if (!prefixMatch) {
        return;
    }

    installmentParagraph.textContent = prefixMatch[1] + formatBrazilianPrice(value);
}

export function setPriceBoxInstallmentCount(count) {
    const installmentParagraph = findPriceBoxInstallmentParagraph();
    if (!installmentParagraph) {
        return;
    }

    const suffixMatch = installmentParagraph.textContent.match(/^\d+x( sem juros de R\$\s*.+)$/i);
    if (!suffixMatch) {
        return;
    }

    installmentParagraph.textContent = count + "x" + suffixMatch[1];
}

function isValidInstallmentsModalItem(installment) {
    return installment instanceof InstallmentsModalListItem;
}

function resolveInstallmentsModalItemClassName(list) {
    const existingItem = list.querySelector('[class*="installmentItem"]');
    if (existingItem) {
        return existingItem.className;
    }

    const modalItem = list.closest('[role="dialog"]')?.querySelector('[class*="installmentItem"]');
    return modalItem?.className ?? "";
}

function buildInstallmentsModalListItem(installment, itemClassName) {
    const item = document.createElement("div");
    item.className = itemClassName;

    const leftColumn = document.createElement("div");

    const label = document.createElement("p");
    label.className = "typo-body-small font-semibold";
    label.textContent = `${installment.count}x de R$\u00a0${formatBrazilianPrice(installment.value)}`;

    const interestLabel = document.createElement("span");
    interestLabel.className = "typo-caption";
    interestLabel.textContent = installment.hasInterest() ? "" : "Sem Juros";

    const totalLabel = document.createElement("span");
    totalLabel.className = "typo-caption";
    totalLabel.textContent = `R$\u00a0${formatBrazilianPrice(installment.getTotal())}`;

    leftColumn.appendChild(label);
    leftColumn.appendChild(interestLabel);
    item.appendChild(leftColumn);
    item.appendChild(totalLabel);

    return item;
}

export function setInstallmentsModalList(installments) {
    if (!Array.isArray(installments) || !installments.every(isValidInstallmentsModalItem)) {
        return;
    }

    const installmentsList = findInstallmentsModalList();
    if (!installmentsList) {
        return;
    }

    const itemClassName = resolveInstallmentsModalItemClassName(installmentsList);
    installmentsList.replaceChildren();

    for (const installment of installments) {
        installmentsList.appendChild(buildInstallmentsModalListItem(installment, itemClassName));
    }
}