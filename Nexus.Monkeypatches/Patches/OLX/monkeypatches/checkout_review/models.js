function roundCurrency(value) {
    return Math.round(value * 100) / 100;
}

function normalizePrice(value) {
    if (!Number.isFinite(value) || value < 0) {
        return 0;
    }

    return roundCurrency(value);
}

function normalizeName(name) {
    if (typeof name !== "string") {
        return "";
    }

    return name.replace(/\s+/g, " ").trim();
}

function normalizeValueText(valueText) {
    if (typeof valueText !== "string") {
        return "";
    }

    return valueText.replace(/\s+/g, " ").trim();
}

export function isCheckoutSummaryDiscountRow(name, valueText = "") {
    const normalizedName = normalizeName(name).toLowerCase();

    if (normalizedName.includes("desconto")) {
        return true;
    }

    return normalizeValueText(valueText).startsWith("-");
}

export class CheckoutSummaryExtraCost {
    constructor(name, value, discountValue = value) {
        this.name = normalizeName(name);
        this.value = normalizePrice(value);
        this.discountValue = normalizePrice(discountValue);
    }

    getChargeValue() {
        return this.discountValue;
    }

    isFree() {
        return this.discountValue === 0;
    }

    hasDiscount() {
        return this.discountValue !== this.value;
    }

    static fromParsed(name, value, discountValue = value) {
        return new CheckoutSummaryExtraCost(name, value, discountValue);
    }
}

export class CheckoutSummary {
    constructor(productPrice, extraCosts) {
        this.productPrice = normalizePrice(productPrice);
        this.extraCosts = Array.isArray(extraCosts)
            ? extraCosts.filter((cost) => cost instanceof CheckoutSummaryExtraCost)
            : [];
    }

    getExtraCostsTotal() {
        return roundCurrency(
            this.extraCosts.reduce((total, cost) => total + cost.getChargeValue(), 0)
        );
    }

    getTotal() {
        return roundCurrency(this.productPrice + this.getExtraCostsTotal());
    }

    static fromParsed(productPrice, extraCosts) {
        const costs = Array.isArray(extraCosts)
            ? extraCosts
                .map((cost) => {
                    if (!cost || typeof cost.name !== "string") {
                        return null;
                    }

                    return CheckoutSummaryExtraCost.fromParsed(
                        cost.name,
                        cost.value,
                        cost.discountValue ?? cost.value
                    );
                })
                .filter((cost) => cost && cost.name !== "")
            : [];

        return new CheckoutSummary(productPrice, costs);
    }
}
