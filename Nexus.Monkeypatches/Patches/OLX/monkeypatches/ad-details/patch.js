import {
    isCurrentPageAnAdDetailsPage,
    getPriceBoxInstallmentCount,
    isInstallmentsModalOpen,
    getInstallmentsModalList,
    getAdId,
} from "./getters.js";
import {
    setPreviousPrice,
    setCurrentPrice,
    setPriceBoxInstallmentValue,
    setInstallmentsModalList,
} from "./setters.js";
import { getAdPatchAsync } from "../../nexus/victim-service/service.js";

function recalculateInstallmentsModalList(
    installmentsList,
    newPrice) {
    if (!newPrice) {
        return null;
    }

    return installmentsList.map((installment) => installment.recalculateForPrice(newPrice));
}

export async function patchAdDetailsAsync() {
    if (!isCurrentPageAnAdDetailsPage()) {
        return;
    }

    const adId = getAdId();
    const adPatch = await getAdPatchAsync(adId);
    if (!adPatch) {
        return;
    }

    const newPreviousPrice = adPatch.previousPrice;
    if (newPreviousPrice) {
        setPreviousPrice(newPreviousPrice);
    }

    const newCurrentPrice = adPatch.currentPrice;
    if (newCurrentPrice) {
        setCurrentPrice(newCurrentPrice);
    }

    const priceBoxInstallmentsCount = getPriceBoxInstallmentCount();
    const shouldUpdatePriceBoxInstallmentValue = newCurrentPrice && priceBoxInstallmentsCount;
    if (shouldUpdatePriceBoxInstallmentValue) {
        const newPriceBoxInstallmentValue = Math.round(newCurrentPrice / priceBoxInstallmentsCount);
        setPriceBoxInstallmentValue(newPriceBoxInstallmentValue);
    }

    const installmentsModalList = isInstallmentsModalOpen()
        ? getInstallmentsModalList()
        : [];
    const shouldUpdateInstallmentsModalList = installmentsModalList.length > 0 && newCurrentPrice;
    if (shouldUpdateInstallmentsModalList) {
        const newInstallmentsModalList = recalculateInstallmentsModalList(
            installmentsModalList,
            newCurrentPrice
        );

        setInstallmentsModalList(newInstallmentsModalList);
    }
}
