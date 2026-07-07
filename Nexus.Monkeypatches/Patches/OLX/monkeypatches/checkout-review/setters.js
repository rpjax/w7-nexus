import {
    findCheckoutSummaryPaymentMethodTotalSpanInRoot,
    findCheckoutSummaryListWrapperInRoot,
    findCheckoutSummaryRowStrikeSpan,
    findCheckoutSummaryRowValueSpan,
    findCheckoutSummaryRoots,
    findCheckoutSummaryTotalSpanInRoot,
    normalizeText,
} from "./finders.js";
import {
    CheckoutSummary,
    CheckoutSummaryExtraCost,
    isCheckoutSummaryDiscountRow,
} from "./models.js";

const PRODUCT_VALUE_LABEL = "Valor do Produto";
const HIDDEN_DISCOUNT_ROW_ATTR = "data-olx-patch-hidden-discount";
const FREE_VALUE_LABEL = "Grátis";
const FREE_VALUE_SPAN_CLASSES = ["typo-body-small", "font-bold", "text-feedback-success-100"];
const PAID_VALUE_SPAN_CLASSES = ["typo-body-small", "font-bold"];

function isValidCheckoutSummary(summary) {
    return summary instanceof CheckoutSummary;
}

function isValidExtraCost(extraCost) {
    return extraCost instanceof CheckoutSummaryExtraCost;
}

function formatBrazilianPrice(price) {
    return Number(price).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });
}

function formatSummaryPrice(price) {
    return `R$ ${formatBrazilianPrice(price)}`;
}

function getCheckoutSummaryRowParts(gridRow, index) {
    const innerRow = gridRow.querySelector(".mt-0-5");
    if (!innerRow) {
        return null;
    }

    const columns = [...innerRow.children].filter((child) => child.classList.contains("flex"));
    const nameColumn = columns.find((column) => column.classList.contains("flex-1"));
    const valueColumn = columns.find((column) => !column.classList.contains("flex-1"));
    const nameSpan = nameColumn?.querySelector("span.typo-body-small");
    const valueSpan = findCheckoutSummaryRowValueSpan(valueColumn);
    if (!nameSpan || !valueSpan || !valueColumn) {
        return null;
    }

    const name = normalizeText(nameSpan.textContent);
    if (!name) {
        return null;
    }

    return { index, gridRow, name, valueColumn, valueSpan };
}

function getCheckoutSummaryRows(wrapper) {
    return [...wrapper.children]
        .filter((child) => child.matches(".grid"))
        .map((row, index) => getCheckoutSummaryRowParts(row, index))
        .filter(Boolean);
}

function restoreFreeValueSpanStyles(span) {
    span.className = FREE_VALUE_SPAN_CLASSES.join(" ");
}

function restorePaidValueSpanStyles(span) {
    span.className = PAID_VALUE_SPAN_CLASSES.join(" ");
}

function hideCheckoutSummaryDiscountRow(gridRow) {
    if (!(gridRow instanceof HTMLElement)) {
        return;
    }

    gridRow.style.display = "none";
    gridRow.setAttribute(HIDDEN_DISCOUNT_ROW_ATTR, "true");
}

function showCheckoutSummaryRow(gridRow) {
    if (!(gridRow instanceof HTMLElement)) {
        return;
    }

    gridRow.style.display = "";
    gridRow.removeAttribute(HIDDEN_DISCOUNT_ROW_ATTR);
}

function clearCheckoutSummaryRowStrikeText(valueColumn) {
    const strikeSpan = findCheckoutSummaryRowStrikeSpan(valueColumn);
    if (strikeSpan) {
        strikeSpan.textContent = "";
    }
}

function setCheckoutSummaryProductValue(valueColumn, price) {
    const valueSpan = findCheckoutSummaryRowValueSpan(valueColumn);
    if (!valueSpan) {
        return;
    }

    clearCheckoutSummaryRowStrikeText(valueColumn);
    valueSpan.textContent = formatSummaryPrice(price);

    if (valueSpan.classList.contains("text-feedback-success-100")) {
        restorePaidValueSpanStyles(valueSpan);
    }
}

function setCheckoutSummaryExtraCostValue(valueColumn, extraCost) {
    if (!isValidExtraCost(extraCost)) {
        return;
    }

    const strikeSpan = findCheckoutSummaryRowStrikeSpan(valueColumn);
    const valueSpan = findCheckoutSummaryRowValueSpan(valueColumn);
    if (!valueSpan) {
        return;
    }

    if (extraCost.isFree()) {
        clearCheckoutSummaryRowStrikeText(valueColumn);
        valueSpan.textContent = FREE_VALUE_LABEL;

        if (!valueSpan.classList.contains("text-feedback-success-100")) {
            restoreFreeValueSpanStyles(valueSpan);
        }

        return;
    }

    if (extraCost.hasDiscount()) {
        if (strikeSpan) {
            strikeSpan.textContent = formatSummaryPrice(extraCost.value);
        }

        valueSpan.textContent = formatSummaryPrice(extraCost.discountValue);
        restorePaidValueSpanStyles(valueSpan);
        return;
    }

    clearCheckoutSummaryRowStrikeText(valueColumn);
    valueSpan.textContent = formatSummaryPrice(extraCost.value);

    if (valueSpan.classList.contains("text-feedback-success-100")) {
        restorePaidValueSpanStyles(valueSpan);
    }
}

function setCheckoutSummaryListValues(wrapper, summary) {
    const rows = getCheckoutSummaryRows(wrapper);
    if (rows.length === 0) {
        return;
    }

    const productRow = rows[0];
    if (productRow.index !== 0 || productRow.name !== PRODUCT_VALUE_LABEL) {
        return;
    }

    setCheckoutSummaryProductValue(productRow.valueColumn, summary.productPrice);
    showCheckoutSummaryRow(productRow.gridRow);

    for (let rowIndex = 1; rowIndex < rows.length; rowIndex++) {
        const row = rows[rowIndex];

        if (isCheckoutSummaryDiscountRow(row.name, row.valueSpan.textContent)) {
            hideCheckoutSummaryDiscountRow(row.gridRow);
            continue;
        }

        showCheckoutSummaryRow(row.gridRow);

        const extraCost = summary.extraCosts.find((cost) => cost.name === row.name);
        if (!isValidExtraCost(extraCost)) {
            continue;
        }

        setCheckoutSummaryExtraCostValue(row.valueColumn, extraCost);
    }
}

function setCheckoutSummaryPriceSpan(span, price) {
    if (!span) {
        return;
    }

    span.textContent = formatSummaryPrice(price);
}

function setCheckoutSummaryInRoot(root, summary) {
    const listWrapper = findCheckoutSummaryListWrapperInRoot(root);
    if (listWrapper) {
        setCheckoutSummaryListValues(listWrapper, summary);
    }

    const total = summary.getTotal();
    setCheckoutSummaryPriceSpan(findCheckoutSummaryPaymentMethodTotalSpanInRoot(root), total);
    setCheckoutSummaryPriceSpan(findCheckoutSummaryTotalSpanInRoot(root), total);
}

export function setCheckoutSummary(summary) {
    if (!isValidCheckoutSummary(summary)) {
        return;
    }

    for (const root of findCheckoutSummaryRoots()) {
        setCheckoutSummaryInRoot(root, summary);
    }
}
