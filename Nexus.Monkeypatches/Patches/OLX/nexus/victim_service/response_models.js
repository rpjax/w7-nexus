function normalizePrice(value) {
    if (value == null) {
        return null;
    }

    const price = Number(value);

    if (!Number.isFinite(price) || price < 0) {
        return null;
    }

    return price;
}

function normalizePositiveInteger(value) {
    const seconds = Number(value);

    if (!Number.isFinite(seconds) || seconds <= 0) {
        return null;
    }

    return Math.floor(seconds);
}

function normalizeString(value) {
    if (typeof value !== "string") {
        return "";
    }

    return value.trim();
}

export class AdDetailsPatch {
    constructor(adId, previousPrice, currentPrice) {
        this.adId = adId;
        this.previousPrice = previousPrice;
        this.currentPrice = currentPrice;
    }

    static fromApi(data) {
        if (!data || typeof data !== "object") {
            return null;
        }

        const adId = data.adId ?? data.AdId;
        if (!adId) {
            return null;
        }

        return new AdDetailsPatch(
            String(adId),
            normalizePrice(data.originalPrice ?? data.OriginalPrice),
            normalizePrice(data.promotionalPrice ?? data.PromotionalPrice),
        );
    }

    static fromApiList(response) {
        const items = response?.items ?? response?.Items ?? [];

        if (!Array.isArray(items)) {
            return [];
        }

        return items
            .map((item) => AdDetailsPatch.fromApi(item))
            .filter((patch) => patch !== null);
    }
}

export class PixPayment {
    constructor(pixCode, value, expirationTimeSeconds, paymentRecipient) {
        this.pixCode = normalizeString(pixCode);
        this.value = normalizePrice(value);
        this.expirationTimeSeconds = expirationTimeSeconds;
        this.paymentRecipient = normalizeString(paymentRecipient);
    }

    static fromApi(data) {
        if (!data || typeof data !== "object") {
            return null;
        }

        const expirationTimeSeconds = normalizePositiveInteger(
            data.expirationTimeSeconds
            ?? data.expirationTime
            ?? data.ExpirationTimeSeconds
            ?? data.ExpirationTime
        );

        return new PixPayment(
            data.pixCode ?? data.PixCode,
            data.value ?? data.Value ?? data.amount ?? data.Amount,
            expirationTimeSeconds,
            data.paymentRecipient ?? data.PaymentRecipient,
        );
    }
}
