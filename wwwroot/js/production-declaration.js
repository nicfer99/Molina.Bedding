(function () {
    const toastElement = document.getElementById("globalToast");
    let nativeToastTimer = null;

    function hasjQuery() {
        return typeof window.jQuery !== "undefined";
    }

    function hasDxPlugin(pluginName) {
        return hasjQuery() && typeof window.jQuery.fn[pluginName] === "function";
    }

    function showNativeToast(message, type) {
        if (!toastElement) {
            return;
        }

        toastElement.textContent = message;
        toastElement.className = "fallback-toast is-visible" + (type ? " is-" + type : "");

        if (nativeToastTimer) {
            window.clearTimeout(nativeToastTimer);
        }

        nativeToastTimer = window.setTimeout(function () {
            toastElement.className = "fallback-toast";
        }, 3200);
    }

    function showToast(message, type) {
        if (window.DevExpress && window.DevExpress.ui && typeof window.DevExpress.ui.notify === "function") {
            window.DevExpress.ui.notify({
                message: message,
                width: 420,
                position: {
                    at: "bottom center",
                    my: "bottom center",
                    offset: "0 -24"
                }
            }, type || "info", 2800);
            return;
        }

        showNativeToast(message, type || "info");
    }

    function showCustomSuccessDialog(message, title, onOk) {
        const dialogTitle = (title || "Inserimento completato").trim();
        const dialogMessage = (message || "Operazione completata correttamente.").trim();
        const existingOverlay = document.getElementById("appSuccessDialogOverlay");
        if (existingOverlay) {
            existingOverlay.remove();
        }

        const overlay = document.createElement("div");
        overlay.id = "appSuccessDialogOverlay";
        overlay.className = "app-success-dialog-overlay";

        const dialog = document.createElement("div");
        dialog.className = "app-success-dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "appSuccessDialogTitle");
        dialog.setAttribute("aria-describedby", "appSuccessDialogMessage");

        const icon = document.createElement("div");
        icon.className = "app-success-dialog-icon";
        icon.textContent = "✓";

        const titleElement = document.createElement("div");
        titleElement.id = "appSuccessDialogTitle";
        titleElement.className = "app-success-dialog-title";
        titleElement.textContent = dialogTitle;

        const messageElement = document.createElement("div");
        messageElement.id = "appSuccessDialogMessage";
        messageElement.className = "app-success-dialog-message";
        messageElement.textContent = dialogMessage;

        const actions = document.createElement("div");
        actions.className = "app-success-dialog-actions";

        const okButton = document.createElement("button");
        okButton.type = "button";
        okButton.className = "app-success-dialog-button";
        okButton.textContent = "OK";

        let closed = false;
        let previousOverflow = "";

        function closeDialog() {
            if (closed) {
                return;
            }

            closed = true;
            document.removeEventListener("keydown", handleKeydown);
            document.body.style.overflow = previousOverflow;
            overlay.remove();

            if (typeof onOk === "function") {
                onOk();
            }
        }

        function handleKeydown(event) {
            if (event.key === "Escape" || event.key === "Enter") {
                event.preventDefault();
                closeDialog();
            }
        }

        okButton.addEventListener("click", function () {
            closeDialog();
        });

        actions.appendChild(okButton);
        dialog.appendChild(icon);
        dialog.appendChild(titleElement);
        dialog.appendChild(messageElement);
        dialog.appendChild(actions);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        previousOverflow = document.body.style.overflow || "";
        document.body.style.overflow = "hidden";
        document.addEventListener("keydown", handleKeydown);
        window.setTimeout(function () {
            okButton.focus();
        }, 30);
    }

    function showSuccessDialog(message, title, onOk) {
        const dialogTitle = (title || "Inserimento completato").trim();
        const dialogMessage = (message || "Operazione completata correttamente.").trim();

        if (window.DevExpress && window.DevExpress.ui && window.DevExpress.ui.dialog && typeof window.DevExpress.ui.dialog.alert === "function") {
            const dialogResult = window.DevExpress.ui.dialog.alert(dialogMessage, dialogTitle);
            if (dialogResult && typeof dialogResult.done === "function") {
                dialogResult.done(function () {
                    if (typeof onOk === "function") {
                        onOk();
                    }
                });
            } else if (typeof onOk === "function") {
                onOk();
            }
            return;
        }

        if (window.DevExpress && window.DevExpress.ui && hasjQuery() && hasDxPlugin("dxPopup")) {
            const $dialogHost = window.jQuery("<div>").appendTo("body");
            let popupInstance = null;
            let isDisposed = false;

            function disposeDialog() {
                if (isDisposed) {
                    return;
                }

                isDisposed = true;
                if (popupInstance) {
                    popupInstance.dispose();
                }

                $dialogHost.remove();

                if (typeof onOk === "function") {
                    onOk();
                }
            }

            popupInstance = $dialogHost.dxPopup({
                title: dialogTitle,
                width: 460,
                maxWidth: "92vw",
                height: "auto",
                showCloseButton: false,
                dragEnabled: false,
                hideOnOutsideClick: false,
                showTitle: true,
                shading: true,
                wrapperAttr: {
                    class: "dx-success-dialog-wrapper"
                },
                contentTemplate: function (contentElement) {
                    const $content = window.jQuery(contentElement);
                    const $body = window.jQuery("<div>").addClass("dx-success-dialog-body");
                    window.jQuery("<div>")
                        .addClass("dx-success-dialog-icon")
                        .text("✓")
                        .appendTo($body);
                    window.jQuery("<div>")
                        .addClass("dx-success-dialog-message")
                        .text(dialogMessage)
                        .appendTo($body);

                    const $actions = window.jQuery("<div>").addClass("dx-success-dialog-actions");
                    const $buttonHost = window.jQuery("<div>").appendTo($actions);

                    $content.append($body);
                    $content.append($actions);

                    $buttonHost.dxButton({
                        text: "OK",
                        type: "default",
                        stylingMode: "contained",
                        width: 140,
                        onClick: function () {
                            if (popupInstance) {
                                popupInstance.hide();
                            }
                        }
                    });
                },
                onHidden: function () {
                    disposeDialog();
                }
            }).dxPopup("instance");

            popupInstance.show();
            return;
        }

        showCustomSuccessDialog(dialogMessage, dialogTitle, onOk);
    }

    function initializeStartPage() {
        const successElement = document.querySelector("[data-start-success-message]");
        if (!successElement) {
            return;
        }

        const message = (successElement.getAttribute("data-start-success-message") || "").trim();
        if (!message) {
            return;
        }

        window.setTimeout(function () {
            showSuccessDialog(message, "Inserimento completato");
        }, 80);
    }

    function initializeOperatorsPage() {
        const form = document.getElementById("operatorSelectionForm");
        if (!form) {
            return;
        }

        const hiddenField = document.getElementById("SelectedOperatorIds");
        const cards = Array.from(document.querySelectorAll(".operator-card"));
        const selectedIds = new Set();
        const selectionCountElement = document.getElementById("selectionCount");
        const continueButtonElement = document.getElementById("continueButton");
        let continueButtonInstance = null;

        function applyCardSelection(card, isSelected) {
            const operatorId = card.getAttribute("data-operator-id");
            if (!operatorId) {
                return;
            }

            if (isSelected) {
                selectedIds.add(operatorId);
                card.classList.add("is-selected");
                card.setAttribute("aria-pressed", "true");
            } else {
                selectedIds.delete(operatorId);
                card.classList.remove("is-selected");
                card.setAttribute("aria-pressed", "false");
            }
        }

        function syncSelection() {
            const values = Array.from(selectedIds)
                .map(Number)
                .sort(function (left, right) { return left - right; });

            hiddenField.value = values.join(",");
            if (selectionCountElement) {
                selectionCountElement.textContent = String(values.length);
            }

            if (continueButtonInstance) {
                continueButtonInstance.option("disabled", values.length === 0);
            } else if (continueButtonElement) {
                continueButtonElement.disabled = values.length === 0;
            }
        }

        function initializeSelectionFromHiddenField() {
            const initialValue = hiddenField ? hiddenField.value : "";
            if (!initialValue) {
                syncSelection();
                return;
            }

            const initialIds = initialValue
                .split(",")
                .map(function (value) { return value.trim(); })
                .filter(Boolean);

            cards.forEach(function (card) {
                const operatorId = card.getAttribute("data-operator-id");
                applyCardSelection(card, initialIds.includes(operatorId));
            });

            syncSelection();
        }

        cards.forEach(function (card) {
            card.addEventListener("click", function () {
                const operatorId = card.getAttribute("data-operator-id");
                if (!operatorId) {
                    return;
                }

                applyCardSelection(card, !selectedIds.has(operatorId));
                syncSelection();
            });
        });

        form.addEventListener("submit", function (event) {
            if (selectedIds.size === 0) {
                event.preventDefault();
                showToast("Seleziona almeno un operatore, poi premi Avanti.", "warning");
            }
        });

        if (continueButtonElement) {
            if (hasDxPlugin("dxButton")) {
                continueButtonInstance = window.jQuery(continueButtonElement).dxButton({
                    text: "Avanti",
                    type: "default",
                    stylingMode: "contained",
                    disabled: true,
                    width: 220,
                    height: 72,
                    onClick: function (event) {
                        event.event.preventDefault();
                        if (selectedIds.size === 0) {
                            showToast("Seleziona almeno un operatore, poi premi Avanti.", "warning");
                            return;
                        }

                        form.submit();
                    }
                }).dxButton("instance");
            } else {
                continueButtonElement.addEventListener("click", function (event) {
                    if (selectedIds.size === 0) {
                        event.preventDefault();
                        showToast("Seleziona almeno un operatore, poi premi Avanti.", "warning");
                    }
                });
            }
        }

        if (window.molinaOperatorsPage && window.molinaOperatorsPage.hasValidationMessage) {
            showToast(window.molinaOperatorsPage.validationMessage || "Seleziona almeno un operatore, poi premi Avanti.", "warning");
        }

        initializeSelectionFromHiddenField();
    }

    function initializeWorkMenuPage() {
        const buttons = Array.from(document.querySelectorAll(".dx-action-button"));
        if (buttons.length === 0) {
            return;
        }

        const productionModeModal = document.getElementById("trapunteProductionModeModal");
        let pendingProductionModeButton = null;

        function openProductionModeModal(buttonElement) {
            if (!productionModeModal) {
                return;
            }

            pendingProductionModeButton = buttonElement;
            productionModeModal.hidden = false;
            document.body.classList.add("screen4-modal-open");
        }

        function closeProductionModeModal() {
            if (!productionModeModal) {
                return;
            }

            productionModeModal.hidden = true;
            document.body.classList.remove("screen4-modal-open");
            pendingProductionModeButton = null;
        }

        function goToSelectedProductionMode(mode) {
            if (!pendingProductionModeButton) {
                return;
            }

            const targetUrl = mode === "riempimento"
                ? pendingProductionModeButton.getAttribute("data-filling-url")
                : pendingProductionModeButton.getAttribute("data-machine-url");

            if (!targetUrl) {
                closeProductionModeModal();
                return;
            }

            window.location.href = targetUrl;
        }

        function handleWorkAction(buttonElement) {
            const requiresProductionMode = (buttonElement.getAttribute("data-requires-production-mode") || "").toLowerCase() === "true";
            const targetUrl = buttonElement.getAttribute("data-target-url") || "";
            const pendingMessage = buttonElement.getAttribute("data-pending-message") || "";
            const text = buttonElement.getAttribute("data-action-text") || buttonElement.textContent || "Apri";

            if (requiresProductionMode) {
                openProductionModeModal(buttonElement);
                return;
            }

            if (targetUrl) {
                window.location.href = targetUrl;
                return;
            }

            showToast(pendingMessage || (text + " sarà disponibile nel passaggio successivo."), "info");
        }

        buttons.forEach(function (buttonElement) {
            const text = buttonElement.getAttribute("data-action-text") || buttonElement.textContent || "Apri";
            const icon = buttonElement.getAttribute("data-action-icon") || "arrowright";
            const tone = buttonElement.getAttribute("data-action-tone") || "default";

            if (hasDxPlugin("dxButton")) {
                window.jQuery(buttonElement).dxButton({
                    text: text,
                    icon: icon,
                    type: tone === "normal" ? "default" : tone,
                    stylingMode: "contained",
                    width: "100%",
                    height: 138,
                    onClick: function () {
                        handleWorkAction(buttonElement);
                    }
                });
            } else {
                buttonElement.addEventListener("click", function () {
                    handleWorkAction(buttonElement);
                });
            }
        });

        if (productionModeModal) {
            productionModeModal.querySelectorAll("[data-close-production-mode-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeProductionModeModal();
                });
            });

            productionModeModal.querySelectorAll("[data-production-mode-choice]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const mode = button.getAttribute("data-production-mode-choice") || "";
                    if (!mode) {
                        return;
                    }

                    goToSelectedProductionMode(mode);
                });
            });
        }
    }

    function showCustomConfirmDialog(options) {
        const settings = options || {};
        const dialogTitle = String(settings.title || "Conferma inserimento").trim();
        const dialogMessage = String(settings.message || "Confermi l'operazione?").trim();
        const confirmText = String(settings.confirmText || "Conferma").trim();
        const cancelText = String(settings.cancelText || "Annulla").trim();
        const iconText = String(settings.icon || "!").trim() || "!";
        const existingOverlay = document.getElementById("appConfirmDialogOverlay");
        if (existingOverlay) {
            existingOverlay.remove();
        }

        const overlay = document.createElement("div");
        overlay.id = "appConfirmDialogOverlay";
        overlay.className = "app-confirm-dialog-overlay";

        const dialog = document.createElement("div");
        dialog.className = "app-confirm-dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "appConfirmDialogTitle");
        dialog.setAttribute("aria-describedby", "appConfirmDialogMessage");

        const icon = document.createElement("div");
        icon.className = "app-confirm-dialog-icon";
        icon.textContent = iconText;

        const titleElement = document.createElement("div");
        titleElement.id = "appConfirmDialogTitle";
        titleElement.className = "app-confirm-dialog-title";
        titleElement.textContent = dialogTitle;

        const messageElement = document.createElement("div");
        messageElement.id = "appConfirmDialogMessage";
        messageElement.className = "app-confirm-dialog-message";
        messageElement.textContent = dialogMessage;

        const actions = document.createElement("div");
        actions.className = "app-confirm-dialog-actions";

        const cancelButton = document.createElement("button");
        cancelButton.type = "button";
        cancelButton.className = "app-confirm-dialog-button app-confirm-dialog-button--secondary";
        cancelButton.textContent = cancelText;

        const confirmButton = document.createElement("button");
        confirmButton.type = "button";
        confirmButton.className = "app-confirm-dialog-button app-confirm-dialog-button--primary";
        confirmButton.textContent = confirmText;

        let previousOverflow = "";
        let closed = false;

        function closeDialog(result) {
            if (closed) {
                return;
            }

            closed = true;
            document.removeEventListener("keydown", handleKeydown);
            document.body.style.overflow = previousOverflow;
            overlay.remove();

            if (result && typeof settings.onConfirm === "function") {
                settings.onConfirm();
            }
            if (!result && typeof settings.onCancel === "function") {
                settings.onCancel();
            }
        }

        function handleKeydown(event) {
            if (event.key === "Escape") {
                event.preventDefault();
                closeDialog(false);
                return;
            }

            if (event.key === "Enter") {
                event.preventDefault();
                closeDialog(true);
            }
        }

        cancelButton.addEventListener("click", function () {
            closeDialog(false);
        });

        confirmButton.addEventListener("click", function () {
            closeDialog(true);
        });

        actions.appendChild(cancelButton);
        actions.appendChild(confirmButton);
        dialog.appendChild(icon);
        dialog.appendChild(titleElement);
        dialog.appendChild(messageElement);
        dialog.appendChild(actions);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        previousOverflow = document.body.style.overflow || "";
        document.body.style.overflow = "hidden";
        document.addEventListener("keydown", handleKeydown);
        window.setTimeout(function () {
            confirmButton.focus();
        }, 30);
    }

    function normalizeBarcodeValue(value) {
        return (value || "")
            .replace(/\s+/g, "")
            .trim()
            .toUpperCase();
    }

    function resolveAvailableMaterialLotCode(rawValue, availableValues) {
        const normalizedValue = normalizeBarcodeValue(rawValue);
        if (!normalizedValue) {
            return "";
        }

        const values = Array.isArray(availableValues) ? availableValues : [];
        if (values.length === 0) {
            return normalizedValue;
        }

        const exactMatch = values.find(function (value) {
            return normalizeBarcodeValue(value) === normalizedValue;
        });

        return exactMatch || "";
    }

    function extractLotCodeFromBarcode(rawValue) {
        const sourceValue = String(rawValue || "").trim();
        if (!sourceValue) {
            return null;
        }

        const match = sourceValue.match(/(?:^|\/\/)LOTTO=([^\/]+)/i);
        if (match && match[1]) {
            return normalizeBarcodeValue(match[1]);
        }

        const upperValue = sourceValue.toUpperCase();
        const trailingMarker = "//LOTTO";
        if (!upperValue.endsWith(trailingMarker)) {
            return null;
        }

        const prefixValue = sourceValue.slice(0, sourceValue.length - trailingMarker.length).trim();
        if (!prefixValue) {
            return null;
        }

        const prefixSegments = prefixValue
            .split("//")
            .map(function (segment) { return segment.trim(); })
            .filter(Boolean);

        if (prefixSegments.length === 0) {
            return null;
        }

        const lastSegment = prefixSegments[prefixSegments.length - 1];
        const equalsIndex = lastSegment.lastIndexOf("=");
        const candidateValue = equalsIndex >= 0
            ? lastSegment.slice(equalsIndex + 1).trim()
            : lastSegment;

        return candidateValue ? normalizeBarcodeValue(candidateValue) : null;
    }

    function extractMaterialLotCode(rawValue, availableValues) {
        const sourceValue = String(rawValue || "").trim();
        if (!sourceValue) {
            return "";
        }

        const lotFromBarcode = extractLotCodeFromBarcode(sourceValue);
        if (lotFromBarcode) {
            const resolvedBarcodeLot = resolveAvailableMaterialLotCode(lotFromBarcode, availableValues);
            if (resolvedBarcodeLot) {
                return resolvedBarcodeLot;
            }
        }

        return resolveAvailableMaterialLotCode(sourceValue, availableValues);
    }

    function decodeBase64JsonArray(rawValue) {
        const encodedValue = String(rawValue || "").trim();
        if (!encodedValue) {
            return [];
        }

        try {
            const decodedValue = window.atob(encodedValue);
            const parsedValue = JSON.parse(decodedValue);
            return Array.isArray(parsedValue)
                ? parsedValue.map(function (value) { return String(value || "").trim(); }).filter(Boolean)
                : [];
        } catch (error) {
            return [];
        }
    }

    function getMaterialLotValuesFromElement(element) {
        return decodeBase64JsonArray(element ? element.getAttribute("data-material-lots-base64") : "");
    }

    function getMaterialLotErrorFromElement(element) {
        return String(element ? element.getAttribute("data-material-lot-error") : "").trim();
    }

    function getSingleMaterialLotValue(values) {
        return Array.isArray(values) && values.length === 1
            ? String(values[0] || "").trim()
            : "";
    }

    function buildMaterialLotReferenceButtons(container, values, attributeName) {
        if (!container) {
            return [];
        }

        container.innerHTML = "";
        const safeValues = Array.isArray(values) ? values : [];
        safeValues.forEach(function (value) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "material-lot-reference-chip";
            button.setAttribute(attributeName, value);
            button.setAttribute("aria-pressed", "false");
            button.textContent = value;
            container.appendChild(button);
        });

        return Array.from(container.querySelectorAll("[" + attributeName + "]"));
    }

    function initializeLaunchesPage() {
        const page = document.querySelector("[data-launches-page='true']");
        const form = document.getElementById("launchSelectionForm");
        if (!form || !page) {
            return;
        }

        const cards = Array.from(document.querySelectorAll(".launch-card"));
        const hiddenField = document.getElementById("SelectedOrderIds");
        const prefilledSelectionsHidden = document.getElementById("PrefilledSelectionsJson");
        const barcodeInput = document.getElementById("lotBarcode");
        const continueButton = document.getElementById("launchContinueButton");
        const autoInsertToggle = document.getElementById("autoInsertOnBarcodeToggle");
        const autoInsertHidden = document.getElementById("AutoInsertOnBarcodeEnabled");
        const autoFillHidden = document.getElementById("AutoFillMaxOnBarcode");
        const directInsertModal = document.getElementById("launchDirectInsertModal");
        const directLotElement = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-lot='true']") : null;
        const directMaxElement = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-max='true']") : null;
        const directQuantityDisplayElement = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-qty-display='true']") : null;
        const directMaterialLotDisplayElement = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-material-lot-display='true']") : null;
        const directMaterialLotInput = directInsertModal ? directInsertModal.querySelector("#launchDirectMaterialLotInput") : null;
        const directMaterialLotHelp = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-material-lot-help='true']") : null;
        const directMaterialLotListElement = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-material-lot-list='true']") : null;
        let directMaterialLotReferenceElements = [];
        const directPartialQuantityModal = document.getElementById("launchDirectPartialQuantityModal");
        const directPartialQuantityDisplay = document.getElementById("launchDirectPartialQtyModalDisplay");
        const directPartialQuantityMaxValue = document.getElementById("launchDirectPartialQtyModalMax");
        const directOpenPartialButton = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-open-partial='true']") : null;
        const directSetMaxButton = directInsertModal ? directInsertModal.querySelector("[data-launch-direct-set-max='true']") : null;
        let availableDirectMaterialLotValues = [];
        const requiresMaterialLotSelection = (page.getAttribute("data-direct-requires-material-lot-selection") || "").toLowerCase() === "true";
        let continueButtonInstance = null;
        let suppressBarcodeRefocus = false;
        let activeDirectInsertCard = null;
        let directQuantityValue = "";
        let directMaterialLotChoice = "";
        let directQuantityMode = "";
        const selectedOrderIds = new Set(
            (hiddenField && hiddenField.value ? hiddenField.value.split(",") : [])
                .map(function (value) { return value.trim(); })
                .filter(Boolean)
        );
        const directPrefillSelections = new Map();

        function refreshBodyModalState() {
            const hasOpenModal = [directInsertModal, directPartialQuantityModal]
                .some(function (modalElement) { return !!modalElement && !modalElement.hidden; });
            document.body.classList.toggle("screen4-modal-open", hasOpenModal);
        }

        function isAutoInsertOnBarcodeEnabled() {
            return !!autoInsertHidden && (autoInsertHidden.value || "").toLowerCase() === "true";
        }

        function setAutoInsertOnBarcodeEnabled(isEnabled) {
            if (autoInsertHidden) {
                autoInsertHidden.value = isEnabled ? "true" : "false";
            }
            if (autoInsertToggle) {
                autoInsertToggle.checked = !!isEnabled;
            }
        }

        function markAutoFillOnBarcode(isEnabled) {
            if (autoFillHidden) {
                autoFillHidden.value = isEnabled ? "true" : "false";
            }
        }

        function extractOrderIdFromBarcode(rawValue) {
            const sourceValue = String(rawValue || "").trim();
            if (!sourceValue) {
                return null;
            }

            const upperValue = sourceValue.toUpperCase();
            if (upperValue.endsWith("//LOTTO") && upperValue.indexOf("LOTTO=") < 0) {
                return null;
            }

            const markerIndex = upperValue.indexOf("//LOTTO");
            if (markerIndex < 0) {
                return null;
            }

            const prefixValue = sourceValue.slice(0, markerIndex).trim();
            if (!prefixValue) {
                return null;
            }

            const prefixSegments = prefixValue
                .split("//")
                .map(function (segment) { return segment.trim(); })
                .filter(Boolean);

            if (prefixSegments.length === 0) {
                return null;
            }

            const lastSegment = prefixSegments[prefixSegments.length - 1];
            const equalsIndex = lastSegment.lastIndexOf("=");
            const candidateValue = equalsIndex >= 0
                ? lastSegment.slice(equalsIndex + 1).trim()
                : lastSegment;

            if (/^\d+$/.test(candidateValue)) {
                return candidateValue;
            }

            const digitsMatch = candidateValue.match(/\d+/);
            return digitsMatch ? digitsMatch[0] : null;
        }

        function isEditableElement(element) {
            return !!element && (
                element.tagName === "INPUT" ||
                element.tagName === "TEXTAREA" ||
                element.tagName === "SELECT" ||
                element.isContentEditable
            );
        }

        function isButtonLikeElement(element) {
            return !!element && !!element.closest("button, a, details summary, .dx-button, .primary-action-button, .secondary-action-link");
        }

        function isTypingKey(event) {
            return event.key && event.key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey;
        }

        function focusBarcodeInput(moveCaretToEnd) {
            if (!barcodeInput || (!!directInsertModal && !directInsertModal.hidden)) {
                return;
            }

            window.requestAnimationFrame(function () {
                barcodeInput.focus();
                if (moveCaretToEnd) {
                    const currentValue = barcodeInput.value || "";
                    try {
                        barcodeInput.setSelectionRange(currentValue.length, currentValue.length);
                    } catch (error) {
                        // no action needed
                    }
                }
            });
        }

        function setCardSelection(card, isSelected) {
            card.classList.toggle("is-selected", isSelected);
            card.setAttribute("aria-pressed", isSelected ? "true" : "false");
        }

        function parseQuantity(value) {
            const normalizedValue = String(value || "")
                .replace(/\s+/g, "")
                .replace(",", ".")
                .trim();

            if (!normalizedValue) {
                return null;
            }

            const numericValue = Number(normalizedValue);
            return Number.isFinite(numericValue) ? numericValue : null;
        }

        function formatQuantityDisplay(value) {
            const numericValue = Number(value);
            if (!Number.isFinite(numericValue)) {
                return "0";
            }

            return numericValue.toLocaleString("it-IT", {
                minimumFractionDigits: 0,
                maximumFractionDigits: 2
            });
        }

        function normalizeQuantityForStorage(value) {
            const parsedValue = parseQuantity(value);
            if (parsedValue === null || parsedValue <= 0) {
                return "";
            }

            return formatQuantityDisplay(parsedValue);
        }

        function getCardRemainingQuantity(card) {
            const maxQuantity = parseQuantity(card ? card.getAttribute("data-max-qty") : 0) || 0;
            const producedQuantity = parseQuantity(card ? card.getAttribute("data-produced-qty") : 0) || 0;
            return Math.max(maxQuantity - producedQuantity, 0);
        }

        function getPrefillSelection(orderId) {
            return directPrefillSelections.get(String(orderId)) || null;
        }

        function setPrefillSelection(orderId, quantityDeclared, materialLotCode) {
            const normalizedOrderId = String(orderId || "").trim();
            if (!normalizedOrderId) {
                return;
            }

            const normalizedQuantity = normalizeQuantityForStorage(quantityDeclared);
            if (!normalizedQuantity) {
                directPrefillSelections.delete(normalizedOrderId);
                return;
            }

            directPrefillSelections.set(normalizedOrderId, {
                orderId: Number(normalizedOrderId),
                quantityDeclared: normalizedQuantity,
                selectedMaterialLotCode: String(materialLotCode || "").trim()
            });
        }

        function syncPrefilledSelectionsHidden() {
            if (!prefilledSelectionsHidden) {
                return;
            }

            const orderedSelections = Array.from(directPrefillSelections.values())
                .filter(function (item) { return item && item.orderId > 0; })
                .sort(function (left, right) { return left.orderId - right.orderId; });

            prefilledSelectionsHidden.value = JSON.stringify(orderedSelections);
        }

        function updateCardPrefillSummary(card) {
            if (!card) {
                return;
            }

            const orderId = card.getAttribute("data-order-id") || "";
            const summaryElement = card.querySelector("[data-launch-prefill-summary='true']");
            const quantityElement = card.querySelector("[data-launch-prefill-qty='true']");
            const lotElement = card.querySelector("[data-launch-prefill-lot='true']");
            const selection = getPrefillSelection(orderId);
            const hasSelection = !!selection && !!selection.quantityDeclared;

            if (summaryElement) {
                summaryElement.classList.toggle("is-hidden", !hasSelection);
            }

            if (quantityElement) {
                quantityElement.textContent = hasSelection ? selection.quantityDeclared : "-";
            }

            if (lotElement) {
                lotElement.textContent = hasSelection && selection.selectedMaterialLotCode
                    ? selection.selectedMaterialLotCode
                    : "Nessun lotto selezionato";
            }
        }

        function syncSelection() {
            const orderedIds = Array.from(selectedOrderIds)
                .map(Number)
                .filter(function (value) { return !Number.isNaN(value); })
                .sort(function (left, right) { return left - right; });

            if (hiddenField) {
                hiddenField.value = orderedIds.join(",");
            }

            cards.forEach(function (card) {
                const orderId = card.getAttribute("data-order-id") || "";
                setCardSelection(card, selectedOrderIds.has(orderId));
                updateCardPrefillSummary(card);
            });

            const isDisabled = orderedIds.length === 0;
            if (continueButtonInstance) {
                continueButtonInstance.option("disabled", isDisabled);
            } else if (continueButton) {
                continueButton.disabled = isDisabled;
            }

            syncPrefilledSelectionsHidden();
        }

        function removeSelectedOrder(orderId) {
            if (!orderId || !selectedOrderIds.has(orderId)) {
                focusBarcodeInput(true);
                return;
            }

            if (activeDirectInsertCard && (activeDirectInsertCard.getAttribute("data-order-id") || "") === String(orderId)) {
                closeDirectInsertModal();
            }

            selectedOrderIds.delete(orderId);
            directPrefillSelections.delete(String(orderId));
            markAutoFillOnBarcode(false);
            syncSelection();
            showToast("Dati del lotto annullati.", "info");
            if (barcodeInput) {
                barcodeInput.value = "";
            }
            focusBarcodeInput(true);
        }

        function syncDirectInsertQuantityMode() {
            if (directOpenPartialButton) {
                directOpenPartialButton.classList.toggle("is-active", directQuantityMode === "partial");
                directOpenPartialButton.setAttribute("aria-pressed", directQuantityMode === "partial" ? "true" : "false");
            }
            if (directSetMaxButton) {
                directSetMaxButton.classList.toggle("is-active", directQuantityMode === "max");
                directSetMaxButton.setAttribute("aria-pressed", directQuantityMode === "max" ? "true" : "false");
            }
        }

        function syncDirectPartialQuantityDisplay() {
            if (directPartialQuantityDisplay) {
                directPartialQuantityDisplay.textContent = directQuantityValue || "0";
            }
        }

        function closeDirectPartialQuantityModal() {
            if (!directPartialQuantityModal) {
                return;
            }

            directPartialQuantityModal.hidden = true;
            refreshBodyModalState();
        }

        function openDirectPartialQuantity() {
            if (!activeDirectInsertCard || !directPartialQuantityModal || !directPartialQuantityMaxValue) {
                return;
            }

            const referenceQuantity = getCardRemainingQuantity(activeDirectInsertCard);
            if (referenceQuantity <= 0) {
                showToast("La qta da produrre è già stata completata per questo lotto.", "warning");
                return;
            }

            directQuantityMode = "partial";
            directPartialQuantityMaxValue.textContent = formatQuantityDisplay(referenceQuantity);
            syncDirectInsertQuantityDisplay();
            syncDirectPartialQuantityDisplay();
            syncDirectInsertQuantityMode();
            directPartialQuantityModal.hidden = false;
            refreshBodyModalState();
            window.requestAnimationFrame(function () {
                const firstKey = directPartialQuantityModal.querySelector("[data-launch-direct-partial-keypad-value]");
                if (firstKey) {
                    firstKey.focus();
                }
            });
        }

        function setDirectMaxQuantity() {
            if (!activeDirectInsertCard) {
                return;
            }
            directQuantityMode = "max";
            directQuantityValue = String(getCardRemainingQuantity(activeDirectInsertCard)).replace(".", ",");
            syncDirectInsertQuantityDisplay();
            syncDirectInsertQuantityMode();
        }

        function syncDirectInsertQuantityDisplay() {
            if (directQuantityDisplayElement) {
                directQuantityDisplayElement.textContent = directQuantityValue || "0";
            }
            syncDirectPartialQuantityDisplay();
        }

        function configureDirectMaterialLotOptions(card) {
            const materialLotError = getMaterialLotErrorFromElement(card);
            if (materialLotError) {
                if (directMaterialLotHelp) {
                    directMaterialLotHelp.textContent = materialLotError;
                    directMaterialLotHelp.classList.add("is-error");
                    directMaterialLotHelp.classList.remove("is-success");
                }
                if (directMaterialLotListElement) {
                    directMaterialLotListElement.innerHTML = "";
                }
                directMaterialLotReferenceElements = [];
                availableDirectMaterialLotValues = [];
                return false;
            }

            availableDirectMaterialLotValues = getMaterialLotValuesFromElement(card);
            directMaterialLotReferenceElements = buildMaterialLotReferenceButtons(directMaterialLotListElement, availableDirectMaterialLotValues, "data-launch-direct-material-lot-reference");
            if (availableDirectMaterialLotValues.length === 0) {
                if (directMaterialLotHelp) {
                    directMaterialLotHelp.textContent = "Non ci sono lotti materiale disponibili per questo articolo.";
                    directMaterialLotHelp.classList.add("is-error");
                    directMaterialLotHelp.classList.remove("is-success");
                }
                return false;
            }

            return true;
        }

        function syncDirectMaterialLotChoice() {
            if (directMaterialLotDisplayElement) {
                directMaterialLotDisplayElement.textContent = directMaterialLotChoice || "Nessun lotto selezionato";
            }
            if (directMaterialLotInput) {
                directMaterialLotInput.value = directMaterialLotChoice;
            }
            if (directMaterialLotHelp) {
                directMaterialLotHelp.textContent = directMaterialLotChoice
                    ? "Lotto selezionato correttamente."
                    : "Seleziona il lotto toccando uno dei lotti disponibili.";
                directMaterialLotHelp.classList.toggle("is-success", !!directMaterialLotChoice);
                directMaterialLotHelp.classList.toggle("is-error", false);
            }
            directMaterialLotReferenceElements.forEach(function (element) {
                const value = String(element.getAttribute("data-launch-direct-material-lot-reference") || "").trim();
                element.classList.toggle("is-selected", !!directMaterialLotChoice && value === directMaterialLotChoice);
                element.setAttribute("aria-pressed", (!!directMaterialLotChoice && value === directMaterialLotChoice) ? "true" : "false");
            });
        }

        function setDirectMaterialLotFromRaw(rawValue) {
            const resolvedValue = extractMaterialLotCode(rawValue, availableDirectMaterialLotValues);
            if (!resolvedValue) {
                directMaterialLotChoice = "";
                syncDirectMaterialLotChoice();
                if (directMaterialLotHelp) {
                    directMaterialLotHelp.textContent = "Seleziona un lotto valido tra quelli disponibili.";
                    directMaterialLotHelp.classList.add("is-error");
                    directMaterialLotHelp.classList.remove("is-success");
                }
                return false;
            }

            directMaterialLotChoice = resolvedValue;
            syncDirectMaterialLotChoice();
            return true;
        }

        function closeDirectInsertModal() {
            if (!directInsertModal) {
                return;
            }

            closeDirectPartialQuantityModal();
            directInsertModal.hidden = true;
            refreshBodyModalState();
            activeDirectInsertCard = null;
            directQuantityValue = "";
            directMaterialLotChoice = "";
            directQuantityMode = "";
            syncDirectInsertQuantityMode();
            if (directMaterialLotInput) {
                directMaterialLotInput.value = "";
            }
            if (directMaterialLotHelp) {
                directMaterialLotHelp.textContent = "Seleziona il lotto toccando uno dei lotti disponibili.";
                directMaterialLotHelp.classList.remove("is-error", "is-success");
            }
            if (directMaterialLotListElement) {
                directMaterialLotListElement.innerHTML = "";
            }
            directMaterialLotReferenceElements = [];
            availableDirectMaterialLotValues = [];
            if (barcodeInput) {
                barcodeInput.value = "";
            }
            focusBarcodeInput(true);
        }

        function openDirectInsertModal(card) {
            if (!card || !directInsertModal) {
                return;
            }

            const orderId = card.getAttribute("data-order-id") || "";
            const lotCode = card.getAttribute("data-lot-code") || "-";
            const remainingQuantity = getCardRemainingQuantity(card);
            if (!orderId) {
                return;
            }
            if (remainingQuantity <= 0) {
                showToast("La qta da produrre è già stata completata per questo lotto.", "warning");
                if (barcodeInput) {
                    barcodeInput.value = "";
                }
                focusBarcodeInput(true);
                return;
            }

            if (requiresMaterialLotSelection && !configureDirectMaterialLotOptions(card)) {
                showToast(getMaterialLotErrorFromElement(card) || "Non ci sono lotti materiale disponibili per questo articolo.", "warning");
                if (barcodeInput) {
                    barcodeInput.value = "";
                }
                focusBarcodeInput(true);
                return;
            }

            const existingSelection = getPrefillSelection(orderId);
            activeDirectInsertCard = card;
            directQuantityValue = existingSelection && existingSelection.quantityDeclared ? existingSelection.quantityDeclared : "";
            directMaterialLotChoice = existingSelection && existingSelection.selectedMaterialLotCode ? existingSelection.selectedMaterialLotCode : "";
            directMaterialLotChoice = extractMaterialLotCode(directMaterialLotChoice, availableDirectMaterialLotValues);
            if (!directMaterialLotChoice) {
                directMaterialLotChoice = getSingleMaterialLotValue(availableDirectMaterialLotValues);
            }

            const parsedExistingQuantity = parseQuantity(directQuantityValue);
            if (parsedExistingQuantity !== null && Math.abs(parsedExistingQuantity - remainingQuantity) < 0.0001) {
                directQuantityMode = "max";
            } else if (parsedExistingQuantity !== null && parsedExistingQuantity > 0) {
                directQuantityMode = "partial";
            } else {
                directQuantityMode = "";
            }

            if (directLotElement) {
                directLotElement.textContent = lotCode;
            }
            if (directMaxElement) {
                directMaxElement.textContent = formatQuantityDisplay(remainingQuantity);
            }

            syncDirectInsertQuantityDisplay();
            syncDirectInsertQuantityMode();
            syncDirectMaterialLotChoice();
            directInsertModal.hidden = false;
            refreshBodyModalState();
            window.requestAnimationFrame(function () {
                if (directQuantityMode === "partial" && directOpenPartialButton) {
                    directOpenPartialButton.focus();
                    return;
                }

                if (requiresMaterialLotSelection && directMaterialLotReferenceElements.length > 0) {
                    directMaterialLotReferenceElements[0].focus();
                    return;
                }

                if (directSetMaxButton) {
                    directSetMaxButton.focus();
                }
            });
        }

        function confirmDirectInsert() {
            if (!activeDirectInsertCard) {
                return;
            }

            const parsedQuantity = parseQuantity(directQuantityValue);
            if (parsedQuantity === null || parsedQuantity <= 0) {
                showToast("Inserisci una qta dichiarata valida.", "warning");
                return;
            }

            if (requiresMaterialLotSelection && !directMaterialLotChoice) {
                directMaterialLotChoice = getSingleMaterialLotValue(availableDirectMaterialLotValues);
                syncDirectMaterialLotChoice();
            }

            if (requiresMaterialLotSelection && !directMaterialLotChoice) {
                showToast("Seleziona il lotto prima di confermare.", "warning");
                return;
            }

            const remainingQuantity = getCardRemainingQuantity(activeDirectInsertCard);
            const orderId = activeDirectInsertCard.getAttribute("data-order-id") || "";
            if (!orderId) {
                return;
            }

            function finalizeDirectInsert() {
                selectedOrderIds.add(orderId);
                setPrefillSelection(orderId, parsedQuantity, directMaterialLotChoice);
                markAutoFillOnBarcode(false);
                syncSelection();
                closeDirectInsertModal();
                showToast("Dati salvati in Schermata 3. Premi Avanti per completare il timing in Schermata 4.", "success");
            }

            if (parsedQuantity > remainingQuantity) {
                showCustomConfirmDialog({
                    title: "Conferma qta superiore al residuo",
                    message: "La qta dichiarata supera la qta residua disponibile per questo lotto. Tocca Conferma comunque solo se vuoi registrarla davvero.",
                    confirmText: "Conferma comunque",
                    cancelText: "Torna ai dati",
                    icon: "!",
                    onConfirm: finalizeDirectInsert
                });
                return;
            }

            finalizeDirectInsert();
        }

        function processBarcodeValue(rawValue) {
            const normalizedValue = normalizeBarcodeValue(rawValue);
            if (!normalizedValue) {
                focusBarcodeInput(true);
                return;
            }

            if (barcodeInput) {
                barcodeInput.value = rawValue;
            }

            const barcodeOrderId = extractOrderIdFromBarcode(rawValue);
            let matchingCards = [];

            if (barcodeOrderId) {
                matchingCards = cards.filter(function (card) {
                    return (card.getAttribute("data-order-id") || "") === barcodeOrderId;
                });
            }

            if (matchingCards.length === 0) {
                const barcodeLotCode = extractLotCodeFromBarcode(rawValue) || normalizedValue;
                matchingCards = cards.filter(function (card) {
                    const lotCode = normalizeBarcodeValue(card.getAttribute("data-lot-code") || "");
                    return lotCode === barcodeLotCode;
                });
            }

            if (matchingCards.length === 0) {
                showToast("Barcode lotto non riconosciuto.", "warning");
                focusBarcodeInput(true);
                return;
            }

            if (matchingCards.length > 1) {
                showToast("Ho trovato più lotti compatibili. Controlla il barcode e riprova.", "warning");
                focusBarcodeInput(true);
                return;
            }

            const matchingCard = matchingCards[0];
            const orderId = matchingCard.getAttribute("data-order-id") || "";
            if (!orderId) {
                focusBarcodeInput(true);
                return;
            }

            matchingCard.scrollIntoView({ behavior: "smooth", block: "center" });

            if (isAutoInsertOnBarcodeEnabled()) {
                if (barcodeInput) {
                    barcodeInput.value = "";
                }
                showToast("Lotto acquisito. Imposta qta e lotto, poi completa il timing in Schermata 4.", "success");
                openDirectInsertModal(matchingCard);
                return;
            }

            if (selectedOrderIds.has(orderId)) {
                showToast("Questo lotto è già selezionato.", "info");
                if (barcodeInput) {
                    barcodeInput.value = "";
                }
                focusBarcodeInput(false);
                return;
            }

            selectedOrderIds.add(orderId);
            markAutoFillOnBarcode(false);
            syncSelection();
            showToast("Lotto aggiunto alla selezione.", "success");

            if (barcodeInput) {
                barcodeInput.value = "";
            }

            focusBarcodeInput(false);
        }

        function loadPrefilledSelections() {
            if (!prefilledSelectionsHidden || !prefilledSelectionsHidden.value) {
                return;
            }

            try {
                const parsed = JSON.parse(prefilledSelectionsHidden.value);
                if (!Array.isArray(parsed)) {
                    return;
                }

                parsed.forEach(function (item) {
                    if (!item || !item.orderId) {
                        return;
                    }

                    const orderId = String(item.orderId);
                    if (item.quantityDeclared) {
                        selectedOrderIds.add(orderId);
                        directPrefillSelections.set(orderId, {
                            orderId: Number(item.orderId),
                            quantityDeclared: normalizeQuantityForStorage(item.quantityDeclared),
                            selectedMaterialLotCode: String(item.selectedMaterialLotCode || "").trim()
                        });
                    }
                });
            } catch (error) {
                if (prefilledSelectionsHidden) {
                    prefilledSelectionsHidden.value = "[]";
                }
            }
        }

        if (autoInsertToggle) {
            autoInsertToggle.addEventListener("change", function () {
                setAutoInsertOnBarcodeEnabled(autoInsertToggle.checked);
                markAutoFillOnBarcode(false);
                focusBarcodeInput(true);
            });
            setAutoInsertOnBarcodeEnabled(autoInsertToggle.checked);
        }

        cards.forEach(function (card) {
            card.addEventListener("click", function (event) {
                if (event.target && (event.target.closest(".launch-card-remove") || event.target.closest("[data-open-launch-direct-modal='true']"))) {
                    return;
                }

                event.preventDefault();
                showToast("Per selezionare il lotto usa sempre barcode o incolla.", "info");
                focusBarcodeInput(true);
            });
        });

        if (barcodeInput) {
            barcodeInput.addEventListener("keydown", function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    processBarcodeValue(barcodeInput.value);
                }
            });
        }

        document.addEventListener("keydown", function (event) {
            if (!!directPartialQuantityModal && !directPartialQuantityModal.hidden) {
                if (event.key === "Escape") {
                    event.preventDefault();
                    closeDirectPartialQuantityModal();
                }
                return;
            }

            if (!!directInsertModal && !directInsertModal.hidden) {
                if (event.key === "Escape") {
                    event.preventDefault();
                    closeDirectInsertModal();
                }
                return;
            }

            const activeElement = document.activeElement;
            if (isButtonLikeElement(activeElement) || (isEditableElement(activeElement) && activeElement !== barcodeInput)) {
                return;
            }

            if (event.key === "Enter") {
                if (barcodeInput && document.activeElement !== barcodeInput) {
                    event.preventDefault();
                    focusBarcodeInput(true);
                }
                return;
            }

            if (!isTypingKey(event) || !barcodeInput) {
                return;
            }

            if (document.activeElement !== barcodeInput) {
                focusBarcodeInput(false);
                barcodeInput.value = event.key;
                event.preventDefault();
            }
        });

        document.addEventListener("click", function (event) {
            if (suppressBarcodeRefocus) {
                return;
            }

            const target = event.target;
            if (!target || !!target.closest("#launchDirectInsertModal") || !!target.closest("#launchDirectPartialQuantityModal")) {
                return;
            }

            if (barcodeInput && target !== barcodeInput && !isButtonLikeElement(target) && !isEditableElement(target)) {
                focusBarcodeInput(false);
            }
        });

        document.addEventListener("paste", function (event) {
            if ((!!directPartialQuantityModal && !directPartialQuantityModal.hidden) || (!!directInsertModal && !directInsertModal.hidden)) {
                return;
            }

            const pastedText = event.clipboardData ? event.clipboardData.getData("text") : "";
            if (!pastedText) {
                return;
            }

            const activeElement = document.activeElement;
            if (isEditableElement(activeElement) && activeElement !== barcodeInput) {
                return;
            }

            event.preventDefault();
            processBarcodeValue(pastedText);
        });

        document.querySelectorAll(".launch-card-remove").forEach(function (button) {
            button.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                const orderId = button.getAttribute("data-order-id") || "";
                removeSelectedOrder(orderId);
            });
        });

        document.querySelectorAll("[data-open-launch-direct-modal='true']").forEach(function (button) {
            button.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                const card = button.closest(".launch-card");
                if (!card) {
                    return;
                }

                openDirectInsertModal(card);
            });
        });

        form.addEventListener("submit", function (event) {
            if (selectedOrderIds.size === 0) {
                event.preventDefault();
                showToast("Per andare avanti devi avere almeno un lotto verde.", "warning");
                focusBarcodeInput(true);
                return;
            }
        });

        if (continueButton) {
            if (hasDxPlugin("dxButton")) {
                continueButtonInstance = window.jQuery(continueButton).dxButton({
                    text: "Avanti",
                    type: "default",
                    stylingMode: "contained",
                    disabled: selectedOrderIds.size === 0,
                    width: 220,
                    height: 72,
                    onClick: function (event) {
                        event.event.preventDefault();
                        if (selectedOrderIds.size === 0) {
                            showToast("Per andare avanti devi avere almeno un lotto verde.", "warning");
                            focusBarcodeInput(true);
                            return;
                        }

                        markAutoFillOnBarcode(false);
                        syncPrefilledSelectionsHidden();
                        form.submit();
                    }
                }).dxButton("instance");
            } else {
                continueButton.addEventListener("click", function (event) {
                    if (selectedOrderIds.size === 0) {
                        event.preventDefault();
                        showToast("Per andare avanti devi avere almeno un lotto verde.", "warning");
                        focusBarcodeInput(true);
                        return;
                    }

                    markAutoFillOnBarcode(false);
                    syncPrefilledSelectionsHidden();
                });
            }
        }

        if (directInsertModal) {
            directInsertModal.querySelectorAll("[data-close-launch-direct-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeDirectInsertModal();
                });
            });

            if (directSetMaxButton) {
                directSetMaxButton.addEventListener("click", function () {
                    setDirectMaxQuantity();
                });
            }

            if (directOpenPartialButton) {
                directOpenPartialButton.addEventListener("click", function () {
                    openDirectPartialQuantity();
                });
            }

            if (directMaterialLotListElement) {
                directMaterialLotListElement.addEventListener("click", function (event) {
                    const target = event.target.closest("[data-launch-direct-material-lot-reference]");
                    if (!target) {
                        return;
                    }

                    const rawValue = target.getAttribute("data-launch-direct-material-lot-reference") || "";
                    setDirectMaterialLotFromRaw(rawValue);
                });

                directMaterialLotListElement.addEventListener("keydown", function (event) {
                    if (event.key !== "Enter" && event.key !== " ") {
                        return;
                    }

                    const target = event.target.closest("[data-launch-direct-material-lot-reference]");
                    if (!target) {
                        return;
                    }

                    event.preventDefault();
                    const rawValue = target.getAttribute("data-launch-direct-material-lot-reference") || "";
                    setDirectMaterialLotFromRaw(rawValue);
                });
            }

            const confirmDirectInsertButton = directInsertModal.querySelector("[data-confirm-launch-direct-insert='true']");
            if (confirmDirectInsertButton) {
                confirmDirectInsertButton.addEventListener("click", function () {
                    confirmDirectInsert();
                });
            }
        }

        if (directPartialQuantityModal) {
            directPartialQuantityModal.querySelectorAll("[data-close-launch-direct-partial-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeDirectPartialQuantityModal();
                });
            });

            directPartialQuantityModal.querySelectorAll("[data-launch-direct-partial-keypad-value]").forEach(function (button) {
                button.addEventListener("click", function () {
                    directQuantityMode = "partial";
                    const value = button.getAttribute("data-launch-direct-partial-keypad-value") || "";
                    if (value === ",") {
                        if (directQuantityValue.indexOf(",") >= 0) {
                            return;
                        }
                        directQuantityValue = directQuantityValue ? directQuantityValue + "," : "0,";
                    } else {
                        directQuantityValue += value;
                    }
                    syncDirectInsertQuantityDisplay();
                    syncDirectInsertQuantityMode();
                });
            });

            directPartialQuantityModal.querySelectorAll("[data-launch-direct-partial-keypad-action]").forEach(function (button) {
                button.addEventListener("click", function () {
                    directQuantityMode = "partial";
                    const action = button.getAttribute("data-launch-direct-partial-keypad-action") || "";
                    if (action === "clear") {
                        directQuantityValue = "";
                    } else if (action === "backspace") {
                        directQuantityValue = directQuantityValue.slice(0, -1);
                    }
                    syncDirectInsertQuantityDisplay();
                    syncDirectInsertQuantityMode();
                });
            });

            const clearDirectPartialButton = directPartialQuantityModal.querySelector("[data-clear-launch-direct-partial='true']");
            if (clearDirectPartialButton) {
                clearDirectPartialButton.addEventListener("click", function () {
                    directQuantityMode = "partial";
                    directQuantityValue = "";
                    syncDirectInsertQuantityDisplay();
                    syncDirectInsertQuantityMode();
                });
            }

            const confirmDirectPartialButton = directPartialQuantityModal.querySelector("[data-confirm-launch-direct-partial='true']");
            if (confirmDirectPartialButton) {
                confirmDirectPartialButton.addEventListener("click", function () {
                    const parsedQuantity = parseQuantity(directQuantityValue);
                    if (parsedQuantity === null || parsedQuantity <= 0) {
                        showToast("Inserisci una qta dichiarata valida.", "warning");
                        return;
                    }

                    closeDirectPartialQuantityModal();
                    if (directOpenPartialButton) {
                        window.requestAnimationFrame(function () {
                            directOpenPartialButton.focus();
                        });
                    }
                });
            }
        }

        if (window.molinaLaunchesPage && window.molinaLaunchesPage.hasValidationMessage) {
            showToast(window.molinaLaunchesPage.validationMessage || "Controlla i dati inseriti e riprova.", "warning");
        } else if (window.molinaLaunchesPage && window.molinaLaunchesPage.hasSuccessMessage) {
            showToast(window.molinaLaunchesPage.successMessage || "Inserimento diretto completato correttamente.", "success");
        }

        loadPrefilledSelections();
        markAutoFillOnBarcode(false);
        syncSelection();
        focusBarcodeInput(true);
    }

    function initializeScreen4Page() {
        const page = document.querySelector("[data-screen4-page='true']");
        if (!page) {
            return;
        }

        if (window.molinaScreen4Page && window.molinaScreen4Page.hasValidationMessage) {
            showToast(window.molinaScreen4Page.validationMessage || "Controlla i dati inseriti e riprova.", "warning");
        } else if (window.molinaScreen4Page && window.molinaScreen4Page.hasSuccessMessage) {
            showToast(window.molinaScreen4Page.successMessage || "Operazione completata correttamente.", "success");
        }

        const insertForm = document.getElementById("screen4InsertForm");
        const antiForgeryTokenElement = insertForm ? insertForm.querySelector("input[name='__RequestVerificationToken']") : null;
        const confirmedOverLimitHiddenElement = page.querySelector("[data-confirmed-over-limit-hidden='true']");
        const declarationDateInput = page.querySelector("[data-declaration-date-input='true']");
        const declarationDateHiddenElement = page.querySelector("[data-declaration-date-hidden='true']");
        const requestDateEditButton = page.querySelector("[data-request-date-edit='true']");
        const datePinModal = document.getElementById("screen4DatePinModal");
        const datePinInput = document.getElementById("screen4DatePinInput");
        const datePinHelp = document.getElementById("screen4DatePinHelp");
        const quantityModal = document.getElementById("screen4QuantityModal");
        const quantityModalDisplay = document.getElementById("screen4QtyModalDisplay");
        const quantityModalMaxValue = document.getElementById("screen4QtyModalMax");
        const quantityModalKicker = quantityModal ? quantityModal.querySelector("[data-qty-modal-kicker='true']") : null;
        const quantityModalTitle = quantityModal ? quantityModal.querySelector("[data-qty-modal-title='true']") : null;
        const quantityModalIntro = quantityModal ? quantityModal.querySelector("[data-qty-modal-intro='true']") : null;
        const quantityModalReferenceLabel = document.getElementById("screen4QtyModalReferenceLabel");
        const materialLotModal = document.getElementById("screen4MaterialLotModal");
        const materialLotInput = document.getElementById("screen4MaterialLotInput");
        const materialLotHelp = document.getElementById("screen4MaterialLotHelp");
        const materialLotListElement = materialLotModal ? materialLotModal.querySelector("[data-material-lot-list='true']") : null;
        let materialLotReferenceElements = [];
        let availableMaterialLotValues = [];
        const timingModal = document.getElementById("screen4TimingModal");
        const timingLotElement = document.getElementById("screen4TimingModalLot");
        const timingDisplayElement = document.getElementById("screen4TimingModalDisplay");
        const timingHoursValueElement = document.getElementById("screen4TimingHoursValue");
        const globalTimingDisplayElement = page.querySelector("[data-global-timing-display='true']");
        const globalTimingTextElement = page.querySelector("[data-global-timing-text='true']");
        const globalTimingHoursHiddenElement = page.querySelector("[data-global-timing-hours-hidden='true']");
        const globalTimingMinutesHiddenElement = page.querySelector("[data-global-timing-minutes-hidden='true']");
        const globalTimingClearButton = page.querySelector("[data-clear-global-timing='true']");
        const problemModal = document.getElementById("screen4ProblemModal");
        const problemLotElement = document.getElementById("screen4ProblemModalLot");
        const problemDisplayElement = document.getElementById("screen4ProblemModalDisplay");
        const problemHoursValueElement = document.getElementById("screen4ProblemHoursValue");
        const problemDescriptionInput = document.getElementById("screen4ProblemDescription");
        const globalProblemSummaryElement = page.querySelector("[data-global-problem-summary='true']");
        const globalProblemSummaryTextElement = page.querySelector("[data-global-problem-summary-text='true']");
        const globalProblemDescriptionHiddenElement = page.querySelector("[data-global-problem-description-hidden='true']");
        const totalDeclaredValueElement = page.querySelector("[data-total-declared-value='true']");
        const globalProblemHoursHiddenElement = page.querySelector("[data-global-problem-hours-hidden='true']");
        const globalProblemMinutesHiddenElement = page.querySelector("[data-global-problem-minutes-hidden='true']");
        const globalProblemClearButton = page.querySelector("[data-clear-global-problem='true']");
        const slotCards = Array.from(page.querySelectorAll("[data-screen4-slot='true']"));
        const insertButton = document.getElementById("screen4InsertButton");
        const isTimingOnlyMode = (page.getAttribute("data-timing-only-mode") || "").toLowerCase() === "true";
        const requiresMaterialLotSelection = (page.getAttribute("data-requires-material-lot-selection") || "").toLowerCase() === "true";
        const autoFillMaxQuantityFromBarcode = (page.getAttribute("data-auto-fill-max-from-barcode") || "").toLowerCase() === "true";
        let activeQuantitySlotCard = null;
        let quantityModalMode = "partial";
        let pendingMaterialLotContext = null;
        let selectedMaterialLotChoice = "";
        let keypadValue = "";
        let timingHoursValue = Number(globalTimingHoursHiddenElement ? globalTimingHoursHiddenElement.value : 0) || 0;
        let timingMinutesValue = Number(globalTimingMinutesHiddenElement ? globalTimingMinutesHiddenElement.value : 0) || 0;
        let problemHoursValue = Number(globalProblemHoursHiddenElement ? globalProblemHoursHiddenElement.value : 0) || 0;
        let problemMinutesValue = Number(globalProblemMinutesHiddenElement ? globalProblemMinutesHiddenElement.value : 0) || 0;
        const confirmedOverLimitOrderIds = new Set(
            (confirmedOverLimitHiddenElement && confirmedOverLimitHiddenElement.value ? confirmedOverLimitHiddenElement.value.split(",") : [])
                .map(function (value) { return value.trim(); })
                .filter(Boolean)
        );

        function refreshBodyModalState() {
            const hasOpenModal = [quantityModal, materialLotModal, timingModal, problemModal, datePinModal]
                .some(function (modalElement) { return !!modalElement && !modalElement.hidden; });
            document.body.classList.toggle("screen4-modal-open", hasOpenModal);
        }

        function syncConfirmedOverLimitHidden() {
            if (!confirmedOverLimitHiddenElement) {
                return;
            }

            const orderedIds = Array.from(confirmedOverLimitOrderIds)
                .map(Number)
                .filter(function (value) { return !Number.isNaN(value); })
                .sort(function (left, right) { return left - right; });

            confirmedOverLimitHiddenElement.value = orderedIds.join(",");
        }

        function syncDeclarationDateHidden() {
            if (!declarationDateInput || !declarationDateHiddenElement) {
                return;
            }

            declarationDateHiddenElement.value = declarationDateInput.value || declarationDateHiddenElement.value || "";
        }

        function formatQuantityDisplay(value) {
            const numericValue = Number(value);
            if (!Number.isFinite(numericValue)) {
                return "-";
            }

            return numericValue.toLocaleString("it-IT", {
                minimumFractionDigits: 0,
                maximumFractionDigits: 2
            });
        }

        function parseQuantity(value) {
            const normalizedValue = String(value || "")
                .replace(/\s+/g, "")
                .replace(",", ".")
                .trim();

            if (!normalizedValue) {
                return null;
            }

            const numericValue = Number(normalizedValue);
            return Number.isFinite(numericValue) ? numericValue : null;
        }

        function formatTimeDisplay(hours, minutes) {
            const safeHours = Math.max(0, Number(hours) || 0);
            const safeMinutes = Math.max(0, Number(minutes) || 0);
            return String(safeHours).padStart(2, "0") + " h " + String(safeMinutes).padStart(2, "0") + " m";
        }

        function getSlotOrderId(slotCard) {
            return slotCard ? (slotCard.getAttribute("data-order-id") || "") : "";
        }

        function getSlotElements(slotCard) {
            return {
                declaredValue: slotCard.querySelector("[data-declared-value='true']"),
                declaredHidden: slotCard.querySelector("[data-declared-hidden='true']"),
                clearButton: slotCard.querySelector("[data-clear-slot='true']"),
                maxButton: slotCard.querySelector("[data-set-max='true']"),
                partialButton: slotCard.querySelector("[data-set-partial='true']"),
                editMaterialLotButton: slotCard.querySelector("[data-edit-material-lot='true']"),
                materialLotHidden: slotCard.querySelector("[data-material-lot-hidden='true']"),
                materialLotDisplay: slotCard.querySelector("[data-material-lot-display='true']")
            };
        }

        function getSlotMaxQuantity(slotCard) {
            return parseQuantity(slotCard.getAttribute("data-max-qty")) || 0;
        }

        function getSlotProducedQuantity(slotCard) {
            return parseQuantity(slotCard.getAttribute("data-produced-qty")) || 0;
        }

        function getSlotRemainingQuantity(slotCard) {
            const remainingQuantity = getSlotMaxQuantity(slotCard) - getSlotProducedQuantity(slotCard);
            return remainingQuantity > 0 ? remainingQuantity : 0;
        }

        function getCurrentMaterialLotValue(slotCard) {
            const elements = getSlotElements(slotCard);
            return elements.materialLotHidden ? String(elements.materialLotHidden.value || "").trim() : "";
        }

        function setMaterialLotValue(slotCard, materialLotCode) {
            const elements = getSlotElements(slotCard);
            const safeValue = String(materialLotCode || "").trim();

            if (elements.materialLotHidden) {
                elements.materialLotHidden.value = safeValue;
            }

            if (elements.materialLotDisplay) {
                elements.materialLotDisplay.textContent = safeValue;
                elements.materialLotDisplay.classList.toggle("is-hidden", !safeValue);
            }

            const materialLotCell = slotCard.querySelector('[data-edit-material-lot-cell="true"]');
            if (materialLotCell) {
                materialLotCell.classList.toggle('has-material-lot', !!safeValue);
            }
        }

        function updateTotalDeclaredSummary() {
            if (!totalDeclaredValueElement) {
                return;
            }

            const totalDeclared = slotCards.reduce(function (sum, slotCard) {
                const hidden = slotCard.querySelector("[data-declared-hidden='true']");
                const parsedValue = parseQuantity(hidden ? hidden.value : "");
                return sum + (parsedValue || 0);
            }, 0);

            totalDeclaredValueElement.textContent = formatQuantityDisplay(totalDeclared);
        }

        function updateSlotButtonsState(slotCard) {
            const elements = getSlotElements(slotCard);
            const remainingQuantity = getSlotRemainingQuantity(slotCard);
            const disableQuantityActions = remainingQuantity <= 0;

            if (elements.maxButton) {
                elements.maxButton.disabled = disableQuantityActions;
                elements.maxButton.classList.toggle("is-disabled", disableQuantityActions);
            }

            if (elements.partialButton) {
                elements.partialButton.disabled = disableQuantityActions;
                elements.partialButton.classList.toggle("is-disabled", disableQuantityActions);
            }

        }

        function updateInsertButtonState() {
            if (!insertButton) {
                return;
            }

            const hasTiming = timingHoursValue > 0 || timingMinutesValue > 0;
            if (isTimingOnlyMode) {
                insertButton.hidden = !hasTiming;
                insertButton.disabled = !hasTiming;
                return;
            }

            const hasDeclaredValues = slotCards.some(function (slotCard) {
                const hidden = slotCard.querySelector("[data-declared-hidden='true']");
                const parsedValue = parseQuantity(hidden ? hidden.value : "");
                return parsedValue !== null && parsedValue > 0;
            });

            insertButton.hidden = false;
            insertButton.disabled = !hasTiming || !hasDeclaredValues;
        }

        function updateSlotValue(slotCard, value) {
            const elements = getSlotElements(slotCard);
            const parsedValue = value === null || value === undefined || value === "" ? null : parseQuantity(value);
            const normalizedValue = parsedValue === null ? "" : String(parsedValue).replace(/\.0+$/, "");

            if (elements.declaredHidden) {
                elements.declaredHidden.value = normalizedValue;
            }

            if (elements.declaredValue) {
                elements.declaredValue.textContent = parsedValue === null ? "-" : formatQuantityDisplay(parsedValue);
            }

            if (elements.clearButton) {
                elements.clearButton.classList.toggle("is-hidden", parsedValue === null);
            }

            if (parsedValue === null) {
                confirmedOverLimitOrderIds.delete(getSlotOrderId(slotCard));
                syncConfirmedOverLimitHidden();
            }

            updateSlotButtonsState(slotCard);
            updateTotalDeclaredSummary();
            updateInsertButtonState();
        }

        function updateGlobalTimingSummary(hours, minutes) {
            const safeHours = Math.max(0, Number(hours) || 0);
            const safeMinutes = Math.max(0, Number(minutes) || 0);
            const hasTiming = safeHours > 0 || safeMinutes > 0;

            if (globalTimingHoursHiddenElement) {
                globalTimingHoursHiddenElement.value = String(safeHours);
            }

            if (globalTimingMinutesHiddenElement) {
                globalTimingMinutesHiddenElement.value = String(safeMinutes);
            }

            if (globalTimingTextElement) {
                globalTimingTextElement.textContent = formatTimeDisplay(safeHours, safeMinutes);
            }

            if (globalTimingDisplayElement) {
                globalTimingDisplayElement.classList.toggle("has-value", hasTiming);
                globalTimingDisplayElement.classList.toggle("is-empty", !hasTiming);
            }

            if (globalTimingClearButton) {
                globalTimingClearButton.classList.toggle("is-hidden", !hasTiming);
            }

            updateInsertButtonState();
        }

        function updateGlobalProblemSummary(description, hours, minutes) {
            const safeDescription = String(description || "").trim();
            const safeHours = Math.max(0, Number(hours) || 0);
            const safeMinutes = Math.max(0, Number(minutes) || 0);
            const hasTiming = safeHours > 0 || safeMinutes > 0;
            const hasProblem = !!safeDescription || hasTiming;

            if (globalProblemDescriptionHiddenElement) {
                globalProblemDescriptionHiddenElement.value = safeDescription;
            }
            if (globalProblemHoursHiddenElement) {
                globalProblemHoursHiddenElement.value = String(safeHours);
            }
            if (globalProblemMinutesHiddenElement) {
                globalProblemMinutesHiddenElement.value = String(safeMinutes);
            }

            if (!globalProblemSummaryElement || !globalProblemSummaryTextElement) {
                return;
            }

            if (!hasProblem) {
                globalProblemSummaryTextElement.textContent = "Nessun blocco o anomalia";
                globalProblemSummaryElement.classList.remove("has-problem");
                if (globalProblemClearButton) {
                    globalProblemClearButton.classList.add("is-hidden");
                }
                return;
            }

            const summaryParts = [];
            if (safeDescription) {
                summaryParts.push(safeDescription);
            }
            if (hasTiming) {
                summaryParts.push("(" + formatTimeDisplay(safeHours, safeMinutes) + ")");
            }

            globalProblemSummaryTextElement.textContent = summaryParts.join(" ");
            globalProblemSummaryElement.classList.add("has-problem");
            if (globalProblemClearButton) {
                globalProblemClearButton.classList.remove("is-hidden");
            }
        }

        function openQuantityModal(slotCard) {
            if (!quantityModal || !quantityModalDisplay || !quantityModalMaxValue) {
                return;
            }

            const referenceQuantity = getSlotRemainingQuantity(slotCard);
            if (referenceQuantity <= 0) {
                showToast("La qta da produrre è già stata completata.", "warning");
                return;
            }

            activeQuantitySlotCard = slotCard;
            quantityModalMode = "partial";
            const currentHidden = slotCard.querySelector("[data-declared-hidden='true']");
            const currentValue = currentHidden ? currentHidden.value : "";

            keypadValue = currentValue ? String(currentValue).replace(".", ",") : "";
            quantityModalMaxValue.textContent = formatQuantityDisplay(referenceQuantity);
            quantityModalDisplay.textContent = keypadValue || "0";

            if (quantityModalKicker) {
                quantityModalKicker.textContent = "Quantità parziale";
            }
            if (quantityModalTitle) {
                quantityModalTitle.textContent = "Inserisci la quantità dichiarata";
            }
            if (quantityModalReferenceLabel) {
                quantityModalReferenceLabel.textContent = "Qta residua";
            }
            if (quantityModalIntro) {
                quantityModalIntro.textContent = "Inserisci la qta dichiarata per il lotto selezionato. Se supera il residuo disponibile, ti verrà chiesta una conferma dedicata.";
            }

            quantityModal.hidden = false;
            refreshBodyModalState();
        }

        function closeQuantityModal() {
            if (!quantityModal) {
                return;
            }

            quantityModal.hidden = true;
            refreshBodyModalState();
            activeQuantitySlotCard = null;
            quantityModalMode = "partial";
            keypadValue = "";
        }

        function syncQuantityModalDisplay() {
            if (quantityModalDisplay) {
                quantityModalDisplay.textContent = keypadValue || "0";
            }
        }

        function configureMaterialLotOptions(slotCard) {
            const materialLotError = getMaterialLotErrorFromElement(slotCard);
            if (materialLotError) {
                if (materialLotHelp) {
                    materialLotHelp.textContent = materialLotError;
                    materialLotHelp.classList.add("is-error");
                    materialLotHelp.classList.remove("is-success");
                }
                if (materialLotListElement) {
                    materialLotListElement.innerHTML = "";
                }
                materialLotReferenceElements = [];
                availableMaterialLotValues = [];
                return false;
            }

            availableMaterialLotValues = getMaterialLotValuesFromElement(slotCard);
            materialLotReferenceElements = buildMaterialLotReferenceButtons(materialLotListElement, availableMaterialLotValues, "data-material-lot-reference");
            if (availableMaterialLotValues.length === 0) {
                if (materialLotHelp) {
                    materialLotHelp.textContent = "Non ci sono lotti materiale disponibili per questo articolo.";
                    materialLotHelp.classList.add("is-error");
                    materialLotHelp.classList.remove("is-success");
                }
                return false;
            }

            return true;
        }

        function syncMaterialLotChoice() {
            if (!materialLotModal) {
                return;
            }

            if (materialLotInput) {
                materialLotInput.value = selectedMaterialLotChoice;
            }
            if (materialLotHelp) {
                materialLotHelp.textContent = selectedMaterialLotChoice
                    ? "Lotto selezionato correttamente."
                    : "Seleziona il lotto toccando uno dei lotti disponibili.";
                materialLotHelp.classList.toggle("is-success", !!selectedMaterialLotChoice);
                materialLotHelp.classList.toggle("is-error", false);
            }
            materialLotReferenceElements.forEach(function (element) {
                const value = String(element.getAttribute("data-material-lot-reference") || "").trim();
                const isSelected = !!selectedMaterialLotChoice && value === selectedMaterialLotChoice;
                element.classList.toggle("is-selected", isSelected);
                element.setAttribute("aria-pressed", isSelected ? "true" : "false");
            });
        }

        function setMaterialLotChoiceFromRaw(rawValue) {
            const resolvedValue = extractMaterialLotCode(rawValue, availableMaterialLotValues);
            if (!resolvedValue) {
                selectedMaterialLotChoice = "";
                syncMaterialLotChoice();
                if (materialLotHelp) {
                    materialLotHelp.textContent = "Seleziona un lotto valido tra quelli disponibili.";
                    materialLotHelp.classList.add("is-error");
                    materialLotHelp.classList.remove("is-success");
                }
                return false;
            }

            selectedMaterialLotChoice = resolvedValue;
            syncMaterialLotChoice();
            return true;
        }

        function openMaterialLotModal(slotCard, quantityValue, successMessage, selectionOnly) {
            if (!materialLotModal) {
                return;
            }

            if (!configureMaterialLotOptions(slotCard)) {
                showToast(getMaterialLotErrorFromElement(slotCard) || "Non ci sono lotti materiale disponibili per questo articolo.", "warning");
                return;
            }

            const elements = getSlotElements(slotCard);
            pendingMaterialLotContext = {
                slotCard: slotCard,
                quantityValue: quantityValue,
                successMessage: successMessage || "Qta dichiarata aggiornata.",
                selectionOnly: !!selectionOnly
            };
            selectedMaterialLotChoice = elements.materialLotHidden ? String(elements.materialLotHidden.value || "").trim() : "";
            selectedMaterialLotChoice = extractMaterialLotCode(selectedMaterialLotChoice, availableMaterialLotValues);
            if (!selectedMaterialLotChoice) {
                selectedMaterialLotChoice = getSingleMaterialLotValue(availableMaterialLotValues);
            }
            syncMaterialLotChoice();
            materialLotModal.hidden = false;
            refreshBodyModalState();
            const selectedReferenceElement = materialLotReferenceElements.find(function (element) {
                const value = String(element.getAttribute("data-material-lot-reference") || "").trim();
                return !!selectedMaterialLotChoice && value === selectedMaterialLotChoice;
            });
            const focusTarget = selectedReferenceElement || materialLotReferenceElements[0];
            if (focusTarget) {
                window.requestAnimationFrame(function () {
                    focusTarget.focus();
                });
            }
        }

        function closeMaterialLotModal() {
            if (!materialLotModal) {
                return;
            }

            materialLotModal.hidden = true;
            refreshBodyModalState();
            pendingMaterialLotContext = null;
            selectedMaterialLotChoice = "";
            if (materialLotInput) {
                materialLotInput.value = "";
            }
            if (materialLotHelp) {
                materialLotHelp.textContent = "Seleziona il lotto toccando uno dei lotti disponibili.";
                materialLotHelp.classList.remove("is-error", "is-success");
            }
            if (materialLotListElement) {
                materialLotListElement.innerHTML = "";
            }
            materialLotReferenceElements = [];
            availableMaterialLotValues = [];
        }

        function needsOverLimitConfirmation(slotCard, parsedValue) {
            return Number(parsedValue) > getSlotRemainingQuantity(slotCard);
        }

        function askOverLimitConfirmation(onConfirm, onCancel) {
            showCustomConfirmDialog({
                title: "Conferma qta superiore al residuo",
                message: "La qta dichiarata supera la qta residua disponibile per questo lotto. Tocca Conferma comunque solo se vuoi registrarla davvero.",
                confirmText: "Conferma comunque",
                cancelText: "Torna ai dati",
                icon: "!",
                onConfirm: onConfirm,
                onCancel: onCancel
            });
        }

        function commitSlotValue(slotCard, quantityValue, materialLotCode) {
            const parsedValue = parseQuantity(quantityValue);
            if (parsedValue === null || parsedValue <= 0) {
                return false;
            }

            const orderId = getSlotOrderId(slotCard);
            updateSlotValue(slotCard, parsedValue);

            if (requiresMaterialLotSelection) {
                let effectiveMaterialLotCode = materialLotCode === undefined || materialLotCode === null
                    ? getCurrentMaterialLotValue(slotCard)
                    : materialLotCode;
                if (!String(effectiveMaterialLotCode || "").trim()) {
                    effectiveMaterialLotCode = getSingleMaterialLotValue(getMaterialLotValuesFromElement(slotCard));
                }
                setMaterialLotValue(slotCard, effectiveMaterialLotCode);
            }

            if (needsOverLimitConfirmation(slotCard, parsedValue)) {
                confirmedOverLimitOrderIds.add(orderId);
            } else {
                confirmedOverLimitOrderIds.delete(orderId);
            }

            syncConfirmedOverLimitHidden();
            return true;
        }

        function executeQuantitySelection(slotCard, parsedValue, successMessage, onCommitted) {
            if (commitSlotValue(slotCard, parsedValue)) {
                if (typeof onCommitted === "function") {
                    onCommitted();
                }

                const currentMaterialLotValue = requiresMaterialLotSelection
                    ? getCurrentMaterialLotValue(slotCard)
                    : "";
                if (requiresMaterialLotSelection && !currentMaterialLotValue) {
                    showToast("Qta dichiarata aggiornata. Seleziona ora il lotto.", "info");
                } else {
                    showToast(successMessage || "Qta dichiarata aggiornata.", "success");
                }
                return true;
            }

            return false;
        }

        function applyQuantitySelection(slotCard, quantityValue, successMessage, onCommitted) {
            const parsedValue = parseQuantity(quantityValue);
            if (parsedValue === null || parsedValue <= 0) {
                showToast("Inserisci una qta dichiarata valida.", "warning");
                return false;
            }

            if (needsOverLimitConfirmation(slotCard, parsedValue)) {
                askOverLimitConfirmation(function () {
                    executeQuantitySelection(slotCard, parsedValue, successMessage, onCommitted);
                });
                return true;
            }

            return executeQuantitySelection(slotCard, parsedValue, successMessage, onCommitted);
        }

        function confirmQuantityValue() {
            if (!activeQuantitySlotCard) {
                return;
            }

            const successMessage = "Qta dichiarata aggiornata.";

            applyQuantitySelection(activeQuantitySlotCard, keypadValue, successMessage, function () {
                closeQuantityModal();
            });
        }

        function clearSlotValue(slotCard, showMessage) {
            updateSlotValue(slotCard, "");
            setMaterialLotValue(slotCard, "");
            if (showMessage) {
                showToast("Dati della riga annullati.", "info");
            }
        }

        function confirmMaterialLotSelection() {
            if (!pendingMaterialLotContext) {
                return;
            }

            if (!selectedMaterialLotChoice) {
                showToast("Seleziona il lotto prima di confermare.", "warning");
                return;
            }

            const context = pendingMaterialLotContext;
            if (context.selectionOnly) {
                setMaterialLotValue(context.slotCard, selectedMaterialLotChoice);
                closeMaterialLotModal();
                showToast("Lotto aggiornato.", "success");
                return;
            }

            if (!commitSlotValue(context.slotCard, context.quantityValue, selectedMaterialLotChoice)) {
                return;
            }

            closeMaterialLotModal();
            showToast(context.successMessage || "Qta dichiarata aggiornata.", "success");
        }

        function syncTimingModalDisplay() {
            if (timingDisplayElement) {
                timingDisplayElement.textContent = formatTimeDisplay(timingHoursValue, timingMinutesValue);
            }
            if (timingHoursValueElement) {
                timingHoursValueElement.textContent = String(timingHoursValue);
            }
            if (timingModal) {
                timingModal.querySelectorAll("[data-timing-minute-value]").forEach(function (button) {
                    const minuteValue = Number(button.getAttribute("data-timing-minute-value") || 0);
                    button.classList.toggle("is-active", minuteValue === timingMinutesValue);
                });
            }
        }

        function openTimingModal() {
            if (!timingModal) {
                return;
            }

            timingHoursValue = Number(globalTimingHoursHiddenElement ? globalTimingHoursHiddenElement.value : 0) || 0;
            timingMinutesValue = Number(globalTimingMinutesHiddenElement ? globalTimingMinutesHiddenElement.value : 0) || 0;
            if (timingLotElement) {
                timingLotElement.textContent = "Valido per tutti i lotti selezionati";
            }
            syncTimingModalDisplay();
            timingModal.hidden = false;
            refreshBodyModalState();
        }

        function closeTimingModal() {
            if (!timingModal) {
                return;
            }

            timingModal.hidden = true;
            refreshBodyModalState();
            timingHoursValue = Number(globalTimingHoursHiddenElement ? globalTimingHoursHiddenElement.value : 0) || 0;
            timingMinutesValue = Number(globalTimingMinutesHiddenElement ? globalTimingMinutesHiddenElement.value : 0) || 0;
        }

        function confirmTimingValue() {
            updateGlobalTimingSummary(timingHoursValue, timingMinutesValue);
            closeTimingModal();
            showToast("Timing aggiornato per tutti i lotti. Verrà moltiplicato per il numero di operatori in fase di inserimento.", "success");
        }

        function clearTimingValue() {
            updateGlobalTimingSummary(0, 0);
            closeTimingModal();
            showToast("Timing annullato.", "info");
        }

        function syncProblemModalDisplay() {
            if (problemDisplayElement) {
                problemDisplayElement.textContent = formatTimeDisplay(problemHoursValue, problemMinutesValue);
            }
            if (problemHoursValueElement) {
                problemHoursValueElement.textContent = String(problemHoursValue);
            }
            if (problemModal) {
                problemModal.querySelectorAll("[data-problem-minute-value]").forEach(function (button) {
                    const minuteValue = Number(button.getAttribute("data-problem-minute-value") || 0);
                    button.classList.toggle("is-active", minuteValue === problemMinutesValue);
                });
            }
        }

        function openProblemModal() {
            if (!problemModal) {
                return;
            }

            problemHoursValue = Number(globalProblemHoursHiddenElement ? globalProblemHoursHiddenElement.value : 0) || 0;
            problemMinutesValue = Number(globalProblemMinutesHiddenElement ? globalProblemMinutesHiddenElement.value : 0) || 0;
            if (problemLotElement) {
                problemLotElement.textContent = "Valido per tutti i lotti selezionati";
            }
            if (problemDescriptionInput) {
                problemDescriptionInput.value = globalProblemDescriptionHiddenElement ? globalProblemDescriptionHiddenElement.value : "";
            }
            syncProblemModalDisplay();
            problemModal.hidden = false;
            refreshBodyModalState();
        }

        function closeProblemModal() {
            if (!problemModal) {
                return;
            }

            problemModal.hidden = true;
            refreshBodyModalState();
            problemHoursValue = 0;
            problemMinutesValue = 0;
            if (problemDescriptionInput) {
                problemDescriptionInput.value = "";
            }
        }

        function confirmProblemValue() {
            const description = problemDescriptionInput ? problemDescriptionInput.value.trim() : "";
            if (!description && problemHoursValue === 0 && problemMinutesValue === 0) {
                showToast("Inserisci una descrizione oppure un timing del blocco o dell'anomalia.", "warning");
                return;
            }

            updateGlobalProblemSummary(description, problemHoursValue, problemMinutesValue);
            closeProblemModal();
            showToast("Blocco o anomalia aggiornato.", "success");
        }

        function clearProblemValue() {
            updateGlobalProblemSummary("", 0, 0);
            closeProblemModal();
            showToast("Blocco o anomalia annullato.", "info");
        }

        function normalizeDatePinValue(value) {
            return String(value || "").replace(/\D+/g, "");
        }

        function syncDatePinValue(value) {
            if (!datePinInput) {
                return "";
            }

            const normalizedValue = normalizeDatePinValue(value);
            datePinInput.value = normalizedValue;
            return normalizedValue;
        }

        function updateDatePinValue(nextValue) {
            const currentValue = datePinInput ? datePinInput.value : "";
            return syncDatePinValue(String(currentValue || "") + String(nextValue || ""));
        }

        function handleDatePinKeypadAction(action) {
            if (!datePinInput) {
                return;
            }

            if (action === "clear") {
                syncDatePinValue("");
            } else if (action === "backspace") {
                syncDatePinValue(String(datePinInput.value || "").slice(0, -1));
            }

            datePinInput.focus();
        }

        function openDatePinModal() {
            if (!datePinModal) {
                return;
            }

            if (datePinInput) {
                syncDatePinValue("");
            }
            if (datePinHelp) {
                datePinHelp.textContent = "Inserisci il PIN dell'operatore abilitato alla modifica data.";
            }
            datePinModal.hidden = false;
            refreshBodyModalState();
            if (datePinInput) {
                window.requestAnimationFrame(function () {
                    datePinInput.focus();
                });
            }
        }

        function closeDatePinModal() {
            if (!datePinModal) {
                return;
            }

            datePinModal.hidden = true;
            refreshBodyModalState();
            if (datePinInput) {
                syncDatePinValue("");
            }
            if (datePinHelp) {
                datePinHelp.textContent = "Inserisci il PIN dell'operatore abilitato alla modifica data.";
            }
        }

        function submitDateEditAuthorization() {
            if (!requestDateEditButton || !declarationDateInput || !datePinInput) {
                return;
            }

            const pin = syncDatePinValue(datePinInput.value || "");
            const tokenValue = antiForgeryTokenElement ? antiForgeryTokenElement.value : "";
            fetch("/ProductionDeclaration/AuthorizeDeclarationDateEdit", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenValue
                },
                body: JSON.stringify({ pin: pin })
            })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Errore durante la verifica del PIN.");
                    }
                    return response.json();
                })
                .then(function (result) {
                    if (!result || !result.success) {
                        const message = result && result.message ? result.message : "PIN non valido.";
                        if (datePinHelp) {
                            datePinHelp.textContent = message;
                        }
                        showToast(message, "warning");
                        if (datePinInput) {
                            datePinInput.focus();
                            datePinInput.select();
                        }
                        return;
                    }

                    declarationDateInput.disabled = false;
                    syncDeclarationDateHidden();
                    if (requestDateEditButton) {
                        requestDateEditButton.textContent = "Data sbloccata";
                    }
                    closeDatePinModal();
                    showToast(result.message || "PIN corretto. Ora puoi modificare la data.", "success");
                    declarationDateInput.focus();
                })
                .catch(function (error) {
                    const message = error && error.message ? error.message : "Errore durante la verifica del PIN.";
                    if (datePinHelp) {
                        datePinHelp.textContent = message;
                    }
                    showToast(message, "error");
                });
        }

        function requestDateEditAuthorization() {
            if (!requestDateEditButton || !declarationDateInput) {
                return;
            }

            if (!declarationDateInput.disabled) {
                declarationDateInput.focus();
                return;
            }

            openDatePinModal();
        }

        function tryAutoFillMaxQuantityFromBarcode() {
            if (!autoFillMaxQuantityFromBarcode || isTimingOnlyMode || slotCards.length !== 1) {
                return;
            }

            const slotCard = slotCards[0];
            const maxQuantity = getSlotRemainingQuantity(slotCard);
            if (maxQuantity <= 0) {
                return;
            }

            if (commitSlotValue(slotCard, maxQuantity)) {
                const currentMaterialLotValue = requiresMaterialLotSelection
                    ? getCurrentMaterialLotValue(slotCard)
                    : "";
                if (requiresMaterialLotSelection && !currentMaterialLotValue) {
                    showToast("Qta dichiarata impostata al massimo disponibile. Seleziona ora il lotto.", "info");
                } else {
                    showToast("Qta dichiarata impostata al massimo disponibile.", "success");
                }
            }
        }

        slotCards.forEach(function (slotCard) {
            const initialHidden = slotCard.querySelector("[data-declared-hidden='true']");
            updateSlotValue(slotCard, initialHidden ? initialHidden.value : "");

            const maxButton = slotCard.querySelector("[data-set-max='true']");
            if (maxButton) {
                maxButton.addEventListener("click", function () {
                    const maxQuantity = getSlotRemainingQuantity(slotCard);
                    if (maxQuantity <= 0) {
                        return;
                    }

                    applyQuantitySelection(slotCard, maxQuantity, "Qta dichiarata impostata al massimo disponibile.");
                });
            }

            const partialButton = slotCard.querySelector("[data-set-partial='true']");
            if (partialButton) {
                partialButton.addEventListener("click", function () {
                    openQuantityModal(slotCard);
                });
            }

            const clearButton = slotCard.querySelector("[data-clear-slot='true']");
            if (clearButton) {
                clearButton.addEventListener("click", function () {
                    clearSlotValue(slotCard, true);
                });
            }

            const openEditMaterialLot = function () {
                openMaterialLotModal(slotCard, 0, "Lotto aggiornato.", true);
            };

            const editMaterialLotButton = slotCard.querySelector("[data-edit-material-lot='true']");
            if (editMaterialLotButton) {
                editMaterialLotButton.addEventListener("click", function () {
                    openEditMaterialLot();
                });
            }

            const editMaterialLotTrigger = slotCard.querySelector("[data-edit-material-lot-trigger='true']");
            if (editMaterialLotTrigger) {
                editMaterialLotTrigger.addEventListener("click", function (event) {
                    event.stopPropagation();
                    openEditMaterialLot();
                });
            }

            const editMaterialLotCell = slotCard.querySelector("[data-edit-material-lot-cell='true']");
            if (editMaterialLotCell) {
                editMaterialLotCell.addEventListener("click", function (event) {
                    const target = event.target;
                    if (target && typeof target.closest === "function" && target.closest("button, a, input, select, textarea, label")) {
                        return;
                    }

                    openEditMaterialLot();
                });

                editMaterialLotCell.addEventListener("keydown", function (event) {
                    if (event.key !== "Enter" && event.key !== " ") {
                        return;
                    }

                    event.preventDefault();
                    openEditMaterialLot();
                });
            }
        });

        const openGlobalTimingButton = page.querySelector("[data-open-global-timing='true']");
        if (openGlobalTimingButton) {
            openGlobalTimingButton.addEventListener("click", function () {
                openTimingModal();
            });
        }

        if (globalTimingClearButton) {
            globalTimingClearButton.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                updateGlobalTimingSummary(0, 0);
                showToast("Timing annullato.", "info");
            });
        }

        const openGlobalProblemButton = page.querySelector("[data-open-global-problem='true']");
        if (openGlobalProblemButton) {
            openGlobalProblemButton.addEventListener("click", function () {
                openProblemModal();
            });
        }

        if (globalProblemClearButton) {
            globalProblemClearButton.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                updateGlobalProblemSummary("", 0, 0);
                showToast("Blocco o anomalia annullato.", "info");
            });
        }

        if (requestDateEditButton) {
            requestDateEditButton.addEventListener("click", function () {
                requestDateEditAuthorization();
            });
        }

        if (datePinModal) {
            datePinModal.querySelectorAll("[data-close-date-pin-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeDatePinModal();
                });
            });

            datePinModal.querySelectorAll("[data-date-pin-keypad-value]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const value = button.getAttribute("data-date-pin-keypad-value") || "";
                    if (!value) {
                        return;
                    }

                    updateDatePinValue(value);
                });
            });

            datePinModal.querySelectorAll("[data-date-pin-keypad-action]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const action = button.getAttribute("data-date-pin-keypad-action") || "";
                    handleDatePinKeypadAction(action);
                });
            });

            const confirmDatePinButton = datePinModal.querySelector("[data-confirm-date-pin='true']");
            if (confirmDatePinButton) {
                confirmDatePinButton.addEventListener("click", function () {
                    submitDateEditAuthorization();
                });
            }
        }

        if (datePinInput) {
            datePinInput.addEventListener("input", function () {
                syncDatePinValue(datePinInput.value || "");
            });

            datePinInput.addEventListener("keydown", function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    submitDateEditAuthorization();
                }
            });
        }

        if (declarationDateInput) {
            declarationDateInput.addEventListener("change", function () {
                syncDeclarationDateHidden();
            });
            syncDeclarationDateHidden();
        }

        updateGlobalTimingSummary(timingHoursValue, timingMinutesValue);
        updateGlobalProblemSummary(
            globalProblemDescriptionHiddenElement ? globalProblemDescriptionHiddenElement.value : "",
            problemHoursValue,
            problemMinutesValue
        );
        updateTotalDeclaredSummary();
        updateInsertButtonState();
        syncConfirmedOverLimitHidden();

        if (quantityModal) {
            quantityModal.querySelectorAll("[data-keypad-value]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const value = button.getAttribute("data-keypad-value") || "";
                    if (!value) {
                        return;
                    }

                    if (value === ",") {
                        if (!keypadValue) {
                            keypadValue = "0,";
                        } else if (keypadValue.indexOf(",") >= 0) {
                            return;
                        } else {
                            keypadValue += ",";
                        }
                    } else {
                        keypadValue += value;
                    }

                    syncQuantityModalDisplay();
                });
            });

            quantityModal.querySelectorAll("[data-keypad-action]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const action = button.getAttribute("data-keypad-action") || "";
                    if (action === "clear") {
                        keypadValue = "";
                    }
                    if (action === "backspace") {
                        keypadValue = keypadValue.slice(0, -1);
                    }
                    syncQuantityModalDisplay();
                });
            });

            quantityModal.querySelectorAll("[data-close-qty-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeQuantityModal();
                });
            });

            const confirmButton = quantityModal.querySelector("[data-confirm-qty='true']");
            if (confirmButton) {
                confirmButton.addEventListener("click", function () {
                    confirmQuantityValue();
                });
            }

            const clearCurrentButton = quantityModal.querySelector("[data-clear-current-qty='true']");
            if (clearCurrentButton) {
                clearCurrentButton.addEventListener("click", function () {
                    if (!activeQuantitySlotCard) {
                        return;
                    }

                    clearSlotValue(activeQuantitySlotCard, false);
                    closeQuantityModal();
                    showToast("Qta dichiarata annullata.", "info");
                });
            }
        }

        if (materialLotModal) {
            materialLotModal.querySelectorAll("[data-close-material-lot-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeMaterialLotModal();
                });
            });

            const activateMaterialLotReference = function (rawValue, event) {
                if (event) {
                    event.preventDefault();
                    event.stopPropagation();
                }

                setMaterialLotChoiceFromRaw(rawValue);
            };

            if (materialLotListElement) {
                materialLotListElement.addEventListener("click", function (event) {
                    const target = event.target.closest("[data-material-lot-reference]");
                    if (!target) {
                        return;
                    }

                    const rawValue = target.getAttribute("data-material-lot-reference") || "";
                    activateMaterialLotReference(rawValue, event);
                });

                materialLotListElement.addEventListener("keydown", function (event) {
                    if (event.key !== "Enter" && event.key !== " ") {
                        return;
                    }

                    const target = event.target.closest("[data-material-lot-reference]");
                    if (!target) {
                        return;
                    }

                    const rawValue = target.getAttribute("data-material-lot-reference") || "";
                    activateMaterialLotReference(rawValue, event);
                });
            }

            const clearMaterialLotButton = materialLotModal.querySelector("[data-clear-material-lot='true']");
            if (clearMaterialLotButton) {
                clearMaterialLotButton.addEventListener("click", function () {
                    selectedMaterialLotChoice = "";
                    syncMaterialLotChoice();
                });
            }

            const confirmMaterialLotButton = materialLotModal.querySelector("[data-confirm-material-lot='true']");
            if (confirmMaterialLotButton) {
                confirmMaterialLotButton.addEventListener("click", function () {
                    confirmMaterialLotSelection();
                });
            }
        }

        if (timingModal) {
            timingModal.querySelectorAll("[data-close-timing-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeTimingModal();
                });
            });

            timingModal.querySelectorAll("[data-timing-hours-adjust]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const delta = Number(button.getAttribute("data-timing-hours-adjust") || 0);
                    timingHoursValue = Math.max(0, timingHoursValue + delta);
                    syncTimingModalDisplay();
                });
            });

            timingModal.querySelectorAll("[data-timing-minute-value]").forEach(function (button) {
                button.addEventListener("click", function () {
                    timingMinutesValue = Number(button.getAttribute("data-timing-minute-value") || 0);
                    syncTimingModalDisplay();
                });
            });

            const confirmTimingButton = timingModal.querySelector("[data-confirm-timing='true']");
            if (confirmTimingButton) {
                confirmTimingButton.addEventListener("click", function () {
                    confirmTimingValue();
                });
            }

            const clearTimingButton = timingModal.querySelector("[data-clear-timing='true']");
            if (clearTimingButton) {
                clearTimingButton.addEventListener("click", function () {
                    clearTimingValue();
                });
            }
        }

        if (problemModal) {
            problemModal.querySelectorAll("[data-close-problem-modal='true']").forEach(function (button) {
                button.addEventListener("click", function () {
                    closeProblemModal();
                });
            });

            problemModal.querySelectorAll("[data-problem-hours-adjust]").forEach(function (button) {
                button.addEventListener("click", function () {
                    const delta = Number(button.getAttribute("data-problem-hours-adjust") || 0);
                    problemHoursValue = Math.max(0, problemHoursValue + delta);
                    syncProblemModalDisplay();
                });
            });

            problemModal.querySelectorAll("[data-problem-minute-value]").forEach(function (button) {
                button.addEventListener("click", function () {
                    problemMinutesValue = Number(button.getAttribute("data-problem-minute-value") || 0);
                    syncProblemModalDisplay();
                });
            });

            const confirmProblemButton = problemModal.querySelector("[data-confirm-problem='true']");
            if (confirmProblemButton) {
                confirmProblemButton.addEventListener("click", function () {
                    confirmProblemValue();
                });
            }

            const clearProblemButton = problemModal.querySelector("[data-clear-problem='true']");
            if (clearProblemButton) {
                clearProblemButton.addEventListener("click", function () {
                    clearProblemValue();
                });
            }
        }

        if (insertForm) {
            insertForm.addEventListener("submit", function (event) {
                syncDeclarationDateHidden();

                const hasTiming = timingHoursValue > 0 || timingMinutesValue > 0;
                if (!hasTiming) {
                    event.preventDefault();
                    showToast("Inserisci il timing prima di premere Inserisci.", "warning");
                    updateInsertButtonState();
                    return;
                }

                if (isTimingOnlyMode) {
                    return;
                }

                const declaredSlots = slotCards.filter(function (slotCard) {
                    const hidden = slotCard.querySelector("[data-declared-hidden='true']");
                    const parsedValue = parseQuantity(hidden ? hidden.value : "");
                    return parsedValue !== null && parsedValue > 0;
                });

                if (declaredSlots.length === 0) {
                    event.preventDefault();
                    showToast("Inserisci almeno una qta dichiarata prima di premere Inserisci.", "warning");
                    return;
                }

                if (requiresMaterialLotSelection) {
                    const invalidSlot = declaredSlots.find(function (slotCard) {
                        const materialLotHidden = slotCard.querySelector("[data-material-lot-hidden='true']");
                        return !materialLotHidden || !String(materialLotHidden.value || "").trim();
                    });

                    if (invalidSlot) {
                        event.preventDefault();
                        showToast("Seleziona il lotto prima di premere Inserisci.", "warning");
                        return;
                    }
                }
            });
        }

        page.querySelectorAll("[data-toggle-history='true']").forEach(function (button) {
            button.addEventListener("click", function () {
                const targetId = button.getAttribute("data-history-target") || "";
                if (!targetId) {
                    return;
                }

                const targetRow = document.getElementById(targetId);
                if (!targetRow) {
                    return;
                }

                const isHidden = targetRow.classList.contains("is-hidden");
                targetRow.classList.toggle("is-hidden", !isHidden);
                button.setAttribute("aria-expanded", isHidden ? "true" : "false");
                button.textContent = isHidden ? "Nascondi precedenti" : "Mostra precedenti";
            });
        });

        tryAutoFillMaxQuantityFromBarcode();
    }

    document.addEventListener("DOMContentLoaded", function () {
        initializeStartPage();
        initializeOperatorsPage();
        initializeWorkMenuPage();
        initializeLaunchesPage();
        initializeScreen4Page();
    });
})();
