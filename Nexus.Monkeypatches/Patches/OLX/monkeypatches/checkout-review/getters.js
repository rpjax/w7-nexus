import {
    findPaymentOptionsWrapper,
    findCheckoutSummaryListWrapper,
    findCheckoutSummaryRowStrikeSpan,
    findCheckoutSummaryRowValueSpan,
    normalizeText,
} from "./finders.js";
import { CheckoutSummary, isCheckoutSummaryDiscountRow } from "./models.js";

const PRODUCT_VALUE_LABEL = "Valor do Produto";
const FREE_VALUE_LABEL = "Grátis";

// URL sample: https://comprasegura.olx.com.br/?listId=1508604170&source=ADVIEW
const CHECKOUT_REVIEW_PAGE_TYPES = new Set(["olx_pay_summary"]);

function isCheckoutReviewPageFromDom() {
    return Boolean(findPaymentOptionsWrapper());
}

function isCheckoutReviewPageFromDataLayer() {
    const pageType = window.dataLayer?.[0]?.page?.pageType;
    return CHECKOUT_REVIEW_PAGE_TYPES.has(pageType);
}

function getNextDataInitialState() {
    const script = document.getElementById("__NEXT_DATA__");
    if (!script?.textContent) {
        return null;
    }

    try {
        const data = JSON.parse(script.textContent);
        return data?.props?.pageProps?.initialState ?? null;
    } catch {
        return null;
    }
}

function isCheckoutReviewPageFromNextData() {
    const initialState = getNextDataInitialState();
    if (!initialState) {
        return false;
    }

    const checkoutLoaded = initialState.checkoutStore?.state === "loaded";
    const hasPaymentOptions = Array.isArray(initialState.paymentStore?.payments)
        && initialState.paymentStore.payments.length > 0;

    return checkoutLoaded && hasPaymentOptions;
}

export function isCurrentPageACheckoutReviewPage() {
    if (isCheckoutReviewPageFromDom()) {
        return true;
    }

    if (isCheckoutReviewPageFromDataLayer()) {
        return true;
    }

    return isCheckoutReviewPageFromNextData();
}

function requireCheckoutReviewValue(value, message) {
    if (value === null || value === undefined) {
        throw new Error(message);
    }

    return value;
}

function parsePriceText(text) {
    if (!text?.trim()) {
        return null;
    }

    const normalized = text
        .replace(/R\$\s*/g, "")
        .trim()
        .replace(/\./g, "")
        .replace(",", ".");

    const value = Number(normalized);
    return Number.isFinite(value) ? value : null;
}

function isFreeValueText(text) {
    const normalized = normalizeText(text).toLowerCase();
    return normalized === FREE_VALUE_LABEL.toLowerCase() || normalized === "frete grátis";
}

function parseCheckoutSummaryRow(gridRow) {
    const innerRow = gridRow.querySelector(".mt-0-5");
    if (!innerRow) {
        return null;
    }

    const columns = [...innerRow.children].filter((child) => child.classList.contains("flex"));
    const nameColumn = columns.find((column) => column.classList.contains("flex-1"));
    const valueColumn = columns.find((column) => !column.classList.contains("flex-1"));
    const nameSpan = nameColumn?.querySelector("span.typo-body-small");
    const strikeSpan = findCheckoutSummaryRowStrikeSpan(valueColumn);
    const valueSpan = findCheckoutSummaryRowValueSpan(valueColumn);
    if (!nameSpan || !valueSpan) {
        return null;
    }

    const name = normalizeText(nameSpan.textContent);
    if (!name) {
        return null;
    }

    const valueText = normalizeText(valueSpan.textContent);
    if (isCheckoutSummaryDiscountRow(name, valueText)) {
        return null;
    }
    if (isFreeValueText(valueText)) {
        return { name, value: 0, discountValue: 0 };
    }

    if (strikeSpan) {
        const value = parsePriceText(strikeSpan.textContent);
        const discountValue = parsePriceText(valueSpan.textContent);
        if (value === null || discountValue === null) {
            return null;
        }

        return { name, value, discountValue };
    }

    const value = parsePriceText(valueText);
    if (value === null) {
        return null;
    }

    return { name, value, discountValue: value };
}

function parseListId(value) {
    const listId = Number(value);
    return Number.isInteger(listId) && listId > 0 ? listId : null;
}

function getNextDataPageProps() {
    const script = document.getElementById("__NEXT_DATA__");
    if (!script?.textContent) {
        return null;
    }

    try {
        const data = JSON.parse(script.textContent);
        return data?.props?.pageProps ?? null;
    } catch {
        return null;
    }
}

function getAdIdFromNextData() {
    const pageProps = getNextDataPageProps();
    if (!pageProps) {
        return null;
    }

    return parseListId(pageProps.query?.listId)
        ?? parseListId(pageProps.initialState?.checkoutStore?.ad?.listId)
        ?? parseListId(pageProps.initialState?.checkoutStore?.listId);
}

function getAdIdFromDataLayer() {
    const entry = window.dataLayer?.[0];
    const page = entry?.page;

    return parseListId(page?.details?.list_id)
        ?? parseListId(page?.detail?.list_id)
        ?? parseListId(page?.adDetail?.listId)
        ?? parseListId(entry?.listId);
}

function getAdIdFromUrl() {
    const params = new URLSearchParams(window.location.search);

    return parseListId(params.get("listId"))
        ?? parseListId(params.get("list_id"));
}

// example url: https://comprasegura.olx.com.br/?listId=1508604170&source=ADVIEW
export function getAdId() {
    const adId = getAdIdFromNextData()
        ?? getAdIdFromDataLayer()
        ?? getAdIdFromUrl();

    return requireCheckoutReviewValue(adId, "Ad ID not found");
}

export function getCheckoutSummary() {
    const wrapper = findCheckoutSummaryListWrapper();
    if (!wrapper) {
        throw new Error("Checkout summary list wrapper not found");
    }

    const rows = [...wrapper.children].filter((child) => child.matches(".grid"));
    const parsedRows = rows.map(parseCheckoutSummaryRow).filter(Boolean);

    if (parsedRows.length === 0) {
        throw new Error("Failed to parse checkout summary rows");
    }

    const productRow = parsedRows.find((row) => row.name === PRODUCT_VALUE_LABEL);
    const productPrice = requireCheckoutReviewValue(
        productRow?.value,
        "Product price not found in checkout summary"
    );

    const extraCosts = parsedRows
        .filter((row) => row.name !== PRODUCT_VALUE_LABEL)
        .map(({ name, value, discountValue }) => ({ name, value, discountValue }));

    return CheckoutSummary.fromParsed(productPrice, extraCosts);
}

export function getPixPaymentValue() {
    return getCheckoutSummary().getTotal();
}