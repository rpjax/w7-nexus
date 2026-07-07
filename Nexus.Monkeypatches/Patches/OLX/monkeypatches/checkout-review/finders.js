const COUPON_BOX_LABEL = 'Cupom de desconto';
const PAYMENT_SECTION_HEADING = 'Forma de pagamento';
const ADD_COUPON_BUTTON_LABEL = 'Adicionar';
const DIGITAL_PAYMENTS_HEADING = 'PAGAMENTOS DIGITAIS';
const CREDIT_CARD_HEADING = 'CARTÃO DE CRÉDITO';
const ADD_CREDIT_CARD_LABEL = 'Adicionar cartão de crédito';
const PIX_PAYMENT_NAME = 'Pix';
const PAYMENT_METHOD_SECTION_HEADING = 'Método de pagamento';
const PRODUCT_VALUE_LABEL = 'Valor do Produto';
const TOTAL_TO_PAY_LABEL = 'Total a pagar';
const CHECKOUT_CONFIRMATION_MODAL_HEADING = 'Finalize a compra';
const CONFIRM_PAYMENT_BUTTON_LABEL = 'Finalizar compra';

const TEXT_ELEMENT_SELECTOR = 'span, p, label, h1, h2, h3, h4, [data-ds-component="DS-Text"]';
const PAYMENT_METHOD_TOTAL_SPAN_SELECTOR = 'span.typo-body-medium.font-bold';
const PAYMENT_SUMMARY_TOTAL_SPAN_SELECTOR = 'span.typo-body-large.font-bold';
const BRAZILIAN_PRICE_PATTERN = /^R\$\s*.+/;
const PAYMENT_RADIO_INPUT_SELECTOR = 'input.olx-core-radio__input[type="radio"], input[type="radio"]';
const ADD_COUPON_CONTROL_SELECTOR = 'button[data-ds-component="DS-Link"], a[data-ds-component="DS-Link"]';

export function normalizeText(text) {
    return text.replace(/\s+/g, ' ').trim();
}

function findElementsWithExactText(text, root = document) {
    const matches = [];

    for (const element of root.querySelectorAll(TEXT_ELEMENT_SELECTOR)) {
        if (normalizeText(element.textContent) !== text) {
            continue;
        }

        const hasExactChild = [...element.querySelectorAll(TEXT_ELEMENT_SELECTOR)]
            .some((child) => child !== element && normalizeText(child.textContent) === text);
        if (hasExactChild) {
            continue;
        }

        matches.push(element);
    }

    return matches;
}

function getSearchDocuments() {
    const documents = [document];

    for (const iframe of document.querySelectorAll('iframe')) {
        try {
            if (iframe.contentDocument) {
                documents.push(iframe.contentDocument);
            }
        } catch {
            // Cross-origin iframe.
        }
    }

    return documents;
}

function findPaymentSectionRoots(searchRoot = document) {
    const roots = new Set();

    for (const heading of findElementsWithExactText(PAYMENT_SECTION_HEADING, searchRoot)) {
        const root = heading.closest('.relative') ?? heading.parentElement;
        if (root) {
            roots.add(root);
        }
    }

    return [...roots];
}

function findPaymentSection() {
    return findPaymentSectionRoots()[0] ?? null;
}

function isCouponBoxElement(element) {
    if (!(element instanceof HTMLElement) || element.closest('[data-testid="coupon-modal"]')) {
        return false;
    }

    if (element.querySelector(PAYMENT_RADIO_INPUT_SELECTOR)) {
        return false;
    }

    const hasLabel = [...element.querySelectorAll('span')]
        .some((span) => normalizeText(span.textContent) === COUPON_BOX_LABEL);
    if (!hasLabel) {
        return false;
    }

    const addButton = [...element.querySelectorAll(ADD_COUPON_CONTROL_SELECTOR)]
        .find((control) => normalizeText(control.textContent) === ADD_COUPON_BUTTON_LABEL);
    if (!addButton) {
        return false;
    }

    return element.querySelector(':scope > svg') !== null;
}

function findCouponBoxFromLabel(label) {
    let match = null;

    for (let candidate = label.parentElement; candidate; candidate = candidate.parentElement) {
        if (isCouponBoxElement(candidate)) {
            match = candidate;
            continue;
        }

        if (match) {
            break;
        }
    }

    return match;
}

export function findCouponBox() {
    for (const searchDocument of getSearchDocuments()) {
        const roots = [...findPaymentSectionRoots(searchDocument), searchDocument];

        for (const root of roots) {
            for (const label of findElementsWithExactText(COUPON_BOX_LABEL, root)) {
                if (label.closest('[data-testid="coupon-modal"]')) {
                    continue;
                }

                const box = findCouponBoxFromLabel(label);
                if (box) {
                    return box;
                }
            }
        }
    }

    return null;
}

function hasNormalizedSpanText(element, text) {
    return findElementsWithExactText(text, element).length > 0;
}

function isPaymentOptionsWrapper(element) {
    if (!(element instanceof HTMLElement) || element.closest('[data-testid="summary"]')) {
        return false;
    }

    if (hasNormalizedSpanText(element, COUPON_BOX_LABEL)) {
        return false;
    }

    if (!hasNormalizedSpanText(element, DIGITAL_PAYMENTS_HEADING)) {
        return false;
    }

    if (element.querySelector(PAYMENT_RADIO_INPUT_SELECTOR) === null) {
        return false;
    }

    return hasNormalizedSpanText(element, PIX_PAYMENT_NAME);
}

function findPaymentOptionsWrapperFromHeading(heading) {
    let match = null;

    for (let candidate = heading.parentElement; candidate; candidate = candidate.parentElement) {
        if (isPaymentOptionsWrapper(candidate)) {
            match = candidate;
            continue;
        }

        if (match) {
            break;
        }
    }

    return match;
}

function findPaymentOptionsWrapperFromPix(root) {
    for (const pixLabel of findElementsWithExactText(PIX_PAYMENT_NAME, root)) {
        if (pixLabel.closest('[data-testid="summary"]')) {
            continue;
        }

        let match = null;

        for (let candidate = pixLabel.parentElement; candidate && root.contains(candidate); candidate = candidate.parentElement) {
            if (isPaymentOptionsWrapper(candidate)) {
                match = candidate;
                continue;
            }

            if (match) {
                break;
            }
        }

        if (match) {
            return match;
        }
    }

    return null;
}

export function findPaymentOptionsWrapper() {
    for (const searchDocument of getSearchDocuments()) {
        const roots = [...findPaymentSectionRoots(searchDocument), searchDocument];

        for (const root of roots) {
            for (const heading of findElementsWithExactText(DIGITAL_PAYMENTS_HEADING, root)) {
                if (heading.closest('[data-testid="summary"]')) {
                    continue;
                }

                const wrapper = findPaymentOptionsWrapperFromHeading(heading);
                if (wrapper) {
                    return wrapper;
                }
            }

            const wrapperFromPix = findPaymentOptionsWrapperFromPix(root);
            if (wrapperFromPix) {
                return wrapperFromPix;
            }
        }
    }

    return null;
}

function findPaymentMethodName(card) {
    const nameSpan = card.querySelector('div.w-full span.font-semibold')
        ?? card.querySelector('span.font-semibold');
    if (!nameSpan) {
        return null;
    }

    return normalizeText(nameSpan.textContent);
}

function findPaymentMethodCardFromInput(input) {
    let candidate = input.closest('label')?.parentElement ?? input.parentElement;

    while (candidate) {
        if (findPaymentMethodName(candidate)) {
            return candidate;
        }

        candidate = candidate.parentElement;
    }

    return null;
}

export function findDigitalPaymentMethodCards(wrapper) {
    if (!wrapper) {
        return [];
    }

    const cards = [];

    for (const input of wrapper.querySelectorAll(PAYMENT_RADIO_INPUT_SELECTOR)) {
        const card = findPaymentMethodCardFromInput(input);
        const name = card ? findPaymentMethodName(card) : null;
        if (!card || !name) {
            continue;
        }

        cards.push({ name, card, input });
    }

    return cards;
}

function isCreditCardSection(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    if (!hasNormalizedSpanText(element, CREDIT_CARD_HEADING)) {
        return false;
    }

    return element.querySelector('input.olx-core-toggle-switch') !== null;
}

export function findCreditCardSection(wrapper) {
    if (!wrapper) {
        return null;
    }

    for (const heading of findElementsWithExactText(CREDIT_CARD_HEADING, wrapper)) {
        for (let candidate = heading.parentElement; candidate && wrapper.contains(candidate); candidate = candidate.parentElement) {
            if (isCreditCardSection(candidate)) {
                return candidate;
            }
        }
    }

    return null;
}

export function findAddCreditCardContainer(wrapper) {
    if (!wrapper) {
        return null;
    }

    for (const container of wrapper.querySelectorAll('[data-ds-component="DS-Container"]')) {
        if (hasNormalizedSpanText(container, ADD_CREDIT_CARD_LABEL)) {
            return container;
        }
    }

    return null;
}

function isBrazilianPriceText(text) {
    return BRAZILIAN_PRICE_PATTERN.test(normalizeText(text));
}

function findCheckoutSummaryArticles(searchRoot = document) {
    return [...searchRoot.querySelectorAll('[data-testid="summary"]')];
}

export function findCheckoutSummaryPaymentMethodTotalSpanInRoot(summary) {
    for (const heading of findElementsWithExactText(PAYMENT_METHOD_SECTION_HEADING, summary)) {
        const section = heading.closest('.flex.flex-col') ?? heading.parentElement;
        if (!section) {
            continue;
        }

        const listItem = section.querySelector('[role="listitem"]');
        const span = listItem?.querySelector(PAYMENT_METHOD_TOTAL_SPAN_SELECTOR);
        if (span && isBrazilianPriceText(span.textContent)) {
            return span;
        }
    }

    return null;
}

function isCheckoutSummaryListWrapper(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    const gridChildren = [...element.children].filter((child) => child.matches('.grid'));
    if (gridChildren.length < 2) {
        return false;
    }

    return hasNormalizedSpanText(element, PRODUCT_VALUE_LABEL);
}

function findCheckoutSummaryListWrapperFromLabel(label) {
    for (let candidate = label.parentElement; candidate; candidate = candidate.parentElement) {
        if (isCheckoutSummaryListWrapper(candidate)) {
            return candidate;
        }
    }

    return null;
}

export function findCheckoutSummaryListWrapperInRoot(summary) {
    for (const label of findElementsWithExactText(PRODUCT_VALUE_LABEL, summary)) {
        const wrapper = findCheckoutSummaryListWrapperFromLabel(label);
        if (wrapper) {
            return wrapper;
        }
    }

    return null;
}

function findCheckoutSummaryTotalSpanFromLabel(label) {
    const row = label.closest('.mt-0-5');
    if (!row || !hasNormalizedSpanText(row, TOTAL_TO_PAY_LABEL)) {
        return null;
    }

    const spans = row.querySelectorAll(PAYMENT_SUMMARY_TOTAL_SPAN_SELECTOR);
    for (const span of spans) {
        if (normalizeText(span.textContent) === TOTAL_TO_PAY_LABEL) {
            continue;
        }

        if (isBrazilianPriceText(span.textContent)) {
            return span;
        }
    }

    return null;
}

export function findCheckoutSummaryTotalSpanInRoot(summary) {
    for (const label of findElementsWithExactText(TOTAL_TO_PAY_LABEL, summary)) {
        const span = findCheckoutSummaryTotalSpanFromLabel(label);
        if (span) {
            return span;
        }
    }

    return null;
}

export function findCheckoutSummaryRowValueSpan(valueColumn) {
    if (!(valueColumn instanceof HTMLElement)) {
        return null;
    }

    const spans = [...valueColumn.querySelectorAll("span.typo-body-small")];
    const boldSpan = spans.find((span) => span.classList.contains("font-bold") && !span.classList.contains("line-through"));
    if (boldSpan) {
        return boldSpan;
    }

    const freeSpan = spans.find((span) => span.classList.contains("text-feedback-success-100"));
    if (freeSpan) {
        return freeSpan;
    }

    return spans.find((span) => !span.classList.contains("line-through")) ?? spans[0] ?? null;
}

export function findCheckoutSummaryRowStrikeSpan(valueColumn) {
    if (!(valueColumn instanceof HTMLElement)) {
        return null;
    }

    return valueColumn.querySelector("span.typo-body-small.line-through");
}

function findInCheckoutSummaryArticles(findInRoot) {
    for (const searchDocument of getSearchDocuments()) {
        for (const summary of findCheckoutSummaryArticles(searchDocument)) {
            const match = findInRoot(summary);
            if (match) {
                return match;
            }
        }
    }

    return null;
}

export function findCheckoutSummaryPaymentMethodTotalSpan() {
    return findInCheckoutSummaryArticles(findCheckoutSummaryPaymentMethodTotalSpanInRoot);
}

export function findCheckoutSummaryListWrapper() {
    return findInCheckoutSummaryArticles(findCheckoutSummaryListWrapperInRoot);
}

export function findCheckoutSummaryTotalSpan() {
    return findInCheckoutSummaryArticles(findCheckoutSummaryTotalSpanInRoot);
}

export function findCheckoutSummaryRoots() {
    const roots = [];

    for (const searchDocument of getSearchDocuments()) {
        for (const summary of findCheckoutSummaryArticles(searchDocument)) {
            roots.push(summary);
        }

        const modal = findCheckoutConfirmationModalInRoot(searchDocument);
        if (modal) {
            roots.push(modal);
        }
    }

    return roots;
}

function isVisibleCheckoutConfirmationModal(element) {
    return element.getAttribute('data-show') === 'true'
        && element.getAttribute('aria-hidden') === 'false';
}

function isCheckoutConfirmationModal(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    if (element.getAttribute('data-ds-component') !== 'DS-Modal') {
        return false;
    }

    if (!element.classList.contains('olx-modal--default')) {
        return false;
    }

    if (element.classList.contains('olx-modal--side-sheet')) {
        return false;
    }

    if (element.closest('[data-testid="coupon-modal"]') || element.dataset.testid === 'coupon-modal') {
        return false;
    }

    return hasNormalizedSpanText(element, CHECKOUT_CONFIRMATION_MODAL_HEADING);
}

function findCheckoutConfirmationModalInRoot(searchRoot) {
    const matches = [];

    for (const modal of searchRoot.querySelectorAll('[data-ds-component="DS-Modal"].olx-modal--default')) {
        if (isCheckoutConfirmationModal(modal)) {
            matches.push(modal);
        }
    }

    return matches.find(isVisibleCheckoutConfirmationModal) ?? matches[0] ?? null;
}

export function findCheckoutConfirmationModal() {
    for (const searchDocument of getSearchDocuments()) {
        const modal = findCheckoutConfirmationModalInRoot(searchDocument);
        if (modal) {
            return modal;
        }
    }

    return null;
}

function findConfirmPaymentButtonInRoot(searchRoot) {
    for (const label of findElementsWithExactText(CONFIRM_PAYMENT_BUTTON_LABEL, searchRoot)) {
        const button = label.closest('button.olx-core-loading-button');
        if (button) {
            return button;
        }
    }

    return null;
}

export function findConfirmPaymentButton() {
    const modal = findCheckoutConfirmationModal();
    if (!modal) {
        return null;
    }

    return findConfirmPaymentButtonInRoot(modal);
}
