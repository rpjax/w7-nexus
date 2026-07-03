export function findPriceBoxContainer() {
    return document.getElementById('price-box-container');
}

export function findPriceBox() {
    return document.getElementById('price-box-container');
}

export function findPreviousPriceWrapper() {
    const priceBox = findPriceBox();
    if (!priceBox?.firstElementChild?.firstElementChild) {
        return null;
    }

    return priceBox.firstElementChild.firstElementChild.children[0] ?? null;
}

export function findCurrentPriceWrapper() {
    const priceBox = findPriceBox();
    if (!priceBox?.firstElementChild?.firstElementChild) {
        return null;
    }

    return priceBox.firstElementChild.firstElementChild.children[1] ?? null;
}

export function findPriceBoxInstallmentParagraph() {
    const priceBox = findPriceBox();
    if (!priceBox?.firstElementChild) {
        return null;
    }

    const installmentSection = priceBox.firstElementChild.children[1];
    if (!installmentSection) {
        return null;
    }

    return installmentSection.querySelector('p.font-semibold.typo-body-medium');
}

function findModalComponents() {
    return document.querySelectorAll(
        '[role="dialog"][aria-modal="true"][data-ds-component="DS-Modal"], [role="dialog"][aria-modal="true"].olx-modal-content, [role="dialog"][aria-modal="true"].olx-modal__dialog'
    );
}

function isInstallmentsModal(modal) {
    const title = modal.querySelector('h4.typo-title-small');
    if (title?.textContent.trim() !== 'Formas de pagamento') {
        return false;
    }

    const optionsHeading = [...modal.querySelectorAll('p.typo-body-medium.font-semibold')]
        .find((paragraph) => paragraph.textContent.trim() === 'Opções de parcelamento');
    if (!optionsHeading) {
        return false;
    }

    const creditCardLabel = [...modal.querySelectorAll('p.typo-body-small.font-semibold')]
        .find((paragraph) => paragraph.textContent.trim() === 'Parcelamento sem juros');
    if (!creditCardLabel) {
        return false;
    }

    const installmentList = modal.querySelector('[class*="installmentList"]');
    if (!installmentList) {
        return false;
    }

    const installmentItems = installmentList.querySelectorAll('[class*="installmentItem"]');
    if (installmentItems.length === 0) {
        return false;
    }

    return [...installmentItems].some((item) => /\d+x de R\$/i.test(item.textContent));
}

export function findInstallmentsModal() {
    for (const modal of findModalComponents()) {
        if (isInstallmentsModal(modal)) {
            return modal;
        }
    }

    return null;
}

export function findInstallmentsModalList() {
    const modal = findInstallmentsModal();
    if (!modal) {
        return null;
    }

    return modal.querySelector('[class*="installmentList"]');
}

export function findInitialDataScript() {
    return document.getElementById("initial-data");
}

export function findAlternateAdPageLink() {
    return document.querySelector('link[rel="alternate"][href^="olxapp://adpage/?id="]');
}

export function findCanonicalLink() {
    return document.querySelector('link[rel="canonical"]');
}