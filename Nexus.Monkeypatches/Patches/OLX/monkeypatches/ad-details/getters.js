import {
    findPriceBox,
    findPreviousPriceWrapper,
    findCurrentPriceWrapper,
    findPriceBoxInstallmentParagraph,
    findInstallmentsModal,
    findInstallmentsModalList,
    findInitialDataScript,
    findAlternateAdPageLink,
    findCanonicalLink,
} from "./finders.js";
import { InstallmentsModalListItem } from "./models.js";

function requireAdDetailsValue(value, message) {
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

export function isCurrentPageAnAdDetailsPage() {
    return Boolean(findPriceBox());
}

export function isInstallmentsModalOpen() {
    return Boolean(findInstallmentsModal());
}

export function getPreviousPrice() {
    const previousPriceWrapper = findPreviousPriceWrapper();
    if (!previousPriceWrapper) {
        throw new Error("Previous price wrapper not found");
    }

    return requireAdDetailsValue(
        parsePriceText(previousPriceWrapper.textContent),
        "Previous price not found"
    );
}

export function getCurrentPrice() {
    const currentPriceWrapper = findCurrentPriceWrapper();
    if (!currentPriceWrapper) {
        throw new Error("Current price wrapper not found");
    }

    return requireAdDetailsValue(
        parsePriceText(currentPriceWrapper.textContent),
        "Current price not found"
    );
}

function parseListId(value) {
    const listId = Number(value);
    return Number.isInteger(listId) && listId > 0 ? listId : null;
}

function getAdIdFromInitialData() {
    const initialData = findInitialDataScript();
    if (!initialData?.dataset.json) {
        return null;
    }

    try {
        const payload = JSON.parse(initialData.dataset.json);
        return parseListId(payload?.ad?.listId);
    } catch {
        return null;
    }
}

function getAdIdFromAlternateLink() {
    const link = findAlternateAdPageLink();
    const match = link?.href.match(/[?&]id=(\d+)/);
    return match ? parseListId(match[1]) : null;
}

function getAdIdFromDataLayer() {
    const entry = window.dataLayer?.[0];
    const page = entry?.page;

    return parseListId(page?.detail?.list_id)
        ?? parseListId(page?.adDetail?.listId)
        ?? parseListId(entry?.listId);
}

function getAdIdFromUrl() {
    const href = findCanonicalLink()?.href ?? window.location.pathname;
    const match = href.match(/-(\d+)(?:\?|$|\/)/);
    return match ? parseListId(match[1]) : null;
}

export function getAdId() {
    const adId = getAdIdFromInitialData()
        ?? getAdIdFromAlternateLink()
        ?? getAdIdFromDataLayer()
        ?? getAdIdFromUrl();

    return requireAdDetailsValue(adId, "Ad ID not found");
}

function parsePriceBoxInstallmentValue(text) {
    const match = text?.match(/R\$\s*([\d.,]+)/);
    if (!match) {
        return null;
    }

    return parsePriceText("R$ " + match[1]);
}

function parsePriceBoxInstallmentCount(text) {
    const match = text?.match(/^(\d+)x sem juros de/i);
    if (!match) {
        return null;
    }

    const value = Number(match[1]);
    return Number.isFinite(value) ? value : null;
}

export function getPriceBoxInstallmentValue() {
    const installmentParagraph = findPriceBoxInstallmentParagraph();
    if (!installmentParagraph) {
        throw new Error("Price box installment paragraph not found");
    }

    return requireAdDetailsValue(
        parsePriceBoxInstallmentValue(installmentParagraph.textContent),
        "Price box installment value not found"
    );
}

export function getPriceBoxInstallmentCount() {
    const installmentParagraph = findPriceBoxInstallmentParagraph();
    if (!installmentParagraph) {
        throw new Error("Price box installment paragraph not found");
    }

    return requireAdDetailsValue(
        parsePriceBoxInstallmentCount(installmentParagraph.textContent),
        "Price box installment count not found"
    );
}


function parseInstallmentsModalListItemRaw(item) {
    const label = item.querySelector("p.typo-body-small.font-semibold");
    if (!label) {
        return null;
    }

    const match = label.textContent.match(/^(\d+)x de R\$\s*([\d.,]+)/i);
    if (!match) {
        return null;
    }

    const count = Number(match[1]);
    const value = parsePriceText("R$ " + match[2]);
    if (!Number.isFinite(count) || value === null) {
        return null;
    }

    return { count, value };
}

function parseInstallmentsModalListItem(item, productPrice) {
    const parsed = parseInstallmentsModalListItemRaw(item);
    if (!parsed) {
        return null;
    }

    return InstallmentsModalListItem.fromParsed(parsed.count, parsed.value, productPrice);
}

export function getInstallmentsModalList() {
    const installmentsList = findInstallmentsModalList();
    if (!installmentsList) {
        throw new Error("Installments modal list not found");
    }

    const items = [...installmentsList.querySelectorAll('[class*="installmentItem"]')];
    const rawItems = items.map(parseInstallmentsModalListItemRaw).filter(Boolean);

    if (rawItems.length === 0) {
        throw new Error("No installments found in installments modal list");
    }

    const productPrice = requireAdDetailsValue(
        rawItems.find((raw) => raw.count === 1)?.value,
        "1x installment not found in installments modal list"
    );

    const installments = items
        .map((item) => parseInstallmentsModalListItem(item, productPrice))
        .filter(Boolean);

    if (installments.length === 0) {
        throw new Error("Failed to parse installments modal list items");
    }

    return installments;
}