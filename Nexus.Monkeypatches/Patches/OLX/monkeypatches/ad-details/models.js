const INTEREST_VALUE_EPSILON = 0.01;

function roundCurrency(value) {
    return Math.round(value * 100) / 100;
}

function normalizeInterestValue(interestValue) {
    if (interestValue <= INTEREST_VALUE_EPSILON) {
        return 0;
    }

    return roundCurrency(interestValue);
}

export class InstallmentsModalListItem {
    constructor(count, value, interestValue) {
        this.count = count;
        this.value = roundCurrency(value);
        this.interestValue = normalizeInterestValue(interestValue);
    }

    getTotal() {
        return roundCurrency(this.count * this.value);
    }

    hasInterest() {
        return this.interestValue > 0;
    }

    getInterestRate() {
        const baseValue = this.value - this.interestValue;

        if (this.interestValue <= 0 || baseValue <= 0) {
            return 0;
        }

        return this.interestValue / baseValue;
    }

    recalculateForPrice(newPrice) {
        const baseValue = roundCurrency(newPrice / this.count);
        const newValue = roundCurrency(baseValue + baseValue * this.getInterestRate());
        const newInterestValue = newValue - baseValue;

        return new InstallmentsModalListItem(this.count, newValue, newInterestValue);
    }

    static fromParsed(count, value, productPrice) {
        const baseValue = productPrice ? roundCurrency(productPrice / count) : value;
        const interestValue = Math.max(0, value - baseValue);

        return new InstallmentsModalListItem(count, value, interestValue);
    }
}