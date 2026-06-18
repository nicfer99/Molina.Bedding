(function () {
    window.molinaBlazorStorage = {
        get: function (key) {
            return window.localStorage.getItem(key);
        },
        set: function (key, value) {
            window.localStorage.setItem(key, value);
        },
        remove: function (key) {
            window.localStorage.removeItem(key);
        }
    };

    window.molinaBlazorDom = {
        focusById: function (id) {
            window.setTimeout(function () {
                var element = document.getElementById(id);
                if (element && typeof element.focus === "function") {
                    element.focus();
                }
            }, 30);
        },
        focusAndSelectById: function (id) {
            window.setTimeout(function () {
                var element = document.getElementById(id);
                if (element && typeof element.focus === "function") {
                    element.focus();
                }
                if (element && typeof element.select === "function") {
                    element.select();
                }
            }, 30);
        },
        scrollLaunchCardIntoView: function (orderId) {
            var element = document.querySelector("[data-order-id='" + String(orderId || "") + "']");
            if (element && typeof element.scrollIntoView === "function") {
                element.scrollIntoView({ behavior: "smooth", block: "center" });
            }
        }
    };

    window.molinaBlazorModal = {
        setOpen: function (isOpen) {
            document.body.classList.toggle("screen4-modal-open", isOpen === true);
        }
    };

    function isEditableElement(element) {
        if (!element) {
            return false;
        }

        var tagName = String(element.tagName || "").toLowerCase();
        return tagName === "input" || tagName === "textarea" || tagName === "select" || element.isContentEditable === true;
    }

    function isButtonLikeElement(element) {
        if (!element) {
            return false;
        }

        var tagName = String(element.tagName || "").toLowerCase();
        return tagName === "button" || tagName === "a" || !!element.closest("button,a,[role='button']");
    }

    function isTypingKey(event) {
        return !event.defaultPrevented
            && !event.ctrlKey
            && !event.altKey
            && !event.metaKey
            && typeof event.key === "string"
            && event.key.length === 1;
    }

    function isInsideAnySelector(element, selectors) {
        if (!element || !selectors) {
            return false;
        }

        return selectors.some(function (selector) {
            return !!selector && !!element.closest(selector);
        });
    }

    window.molinaBlazorBarcode = {
        activate: function (inputId, blockedSelectors) {
            var state = window.__molinaBlazorBarcodeState || {};
            state.inputId = inputId;
            state.blockedSelectors = Array.isArray(blockedSelectors) ? blockedSelectors : [];

            if (!state.bound) {
                state.bound = true;
                state.suppressRefocus = false;

                state.focusBarcode = function (clearValue) {
                    var input = document.getElementById(state.inputId);
                    if (!input || typeof input.focus !== "function") {
                        return;
                    }

                    if (clearValue) {
                        input.value = "";
                        input.dispatchEvent(new Event("input", { bubbles: true }));
                    }

                    window.setTimeout(function () {
                        input.focus({ preventScroll: true });
                    }, 0);
                };

                document.addEventListener("keydown", function (event) {
                    var input = document.getElementById(state.inputId);
                    if (!input || isInsideAnySelector(document.activeElement, state.blockedSelectors)) {
                        return;
                    }

                    var activeElement = document.activeElement;
                    if (isButtonLikeElement(activeElement) || (isEditableElement(activeElement) && activeElement !== input)) {
                        return;
                    }

                    if (event.key === "Enter") {
                        if (document.activeElement !== input) {
                            event.preventDefault();
                            state.focusBarcode(true);
                        }
                        return;
                    }

                    if (!isTypingKey(event)) {
                        return;
                    }

                    if (document.activeElement !== input) {
                        input.value = event.key;
                        input.dispatchEvent(new Event("input", { bubbles: true }));
                        event.preventDefault();
                        state.focusBarcode(false);
                    }
                });

                document.addEventListener("click", function (event) {
                    var input = document.getElementById(state.inputId);
                    var target = event.target;
                    if (!input || !target || isInsideAnySelector(target, state.blockedSelectors)) {
                        return;
                    }

                    if (target !== input && !isButtonLikeElement(target) && !isEditableElement(target)) {
                        state.focusBarcode(false);
                    }
                });
            }

            window.__molinaBlazorBarcodeState = state;
        }
    };

    function ensureToast() {
        var toast = document.getElementById("globalToast");
        if (!toast) {
            toast = document.createElement("div");
            toast.id = "globalToast";
            toast.className = "fallback-toast";
            toast.setAttribute("aria-live", "polite");
            toast.setAttribute("aria-atomic", "true");
            document.body.appendChild(toast);
        }

        return toast;
    }

    function closeOnKeys(event, close, confirmOnEnter) {
        if (event.key === "Escape") {
            event.preventDefault();
            close(false);
        }

        if (confirmOnEnter && event.key === "Enter") {
            event.preventDefault();
            close(true);
        }
    }

    function showSuccessDialog(message, title) {
        return new Promise(function (resolve) {
            var existingOverlay = document.getElementById("appSuccessDialogOverlay");
            if (existingOverlay) {
                existingOverlay.remove();
            }

            var overlay = document.createElement("div");
            overlay.id = "appSuccessDialogOverlay";
            overlay.className = "app-success-dialog-overlay";

            var dialog = document.createElement("div");
            dialog.className = "app-success-dialog";
            dialog.setAttribute("role", "dialog");
            dialog.setAttribute("aria-modal", "true");
            dialog.setAttribute("aria-labelledby", "appSuccessDialogTitle");
            dialog.setAttribute("aria-describedby", "appSuccessDialogMessage");

            var icon = document.createElement("div");
            icon.className = "app-success-dialog-icon";
            icon.textContent = "✓";

            var titleElement = document.createElement("div");
            titleElement.id = "appSuccessDialogTitle";
            titleElement.className = "app-success-dialog-title";
            titleElement.textContent = title || "Inserimento completato";

            var messageElement = document.createElement("div");
            messageElement.id = "appSuccessDialogMessage";
            messageElement.className = "app-success-dialog-message";
            messageElement.textContent = message || "Operazione completata correttamente.";

            var actions = document.createElement("div");
            actions.className = "app-success-dialog-actions";

            var okButton = document.createElement("button");
            okButton.type = "button";
            okButton.className = "app-success-dialog-button";
            okButton.textContent = "OK";

            var previousOverflow = document.body.style.overflow || "";
            var closed = false;

            function closeDialog() {
                if (closed) {
                    return;
                }

                closed = true;
                document.removeEventListener("keydown", keyHandler);
                document.body.style.overflow = previousOverflow;
                overlay.remove();
                resolve(true);
            }

            function keyHandler(event) {
                closeOnKeys(event, closeDialog, true);
            }

            okButton.addEventListener("click", closeDialog);
            actions.appendChild(okButton);
            dialog.appendChild(icon);
            dialog.appendChild(titleElement);
            dialog.appendChild(messageElement);
            dialog.appendChild(actions);
            overlay.appendChild(dialog);
            document.body.appendChild(overlay);

            document.body.style.overflow = "hidden";
            document.addEventListener("keydown", keyHandler);
            window.setTimeout(function () {
                okButton.focus();
            }, 30);
        });
    }

    function showConfirmDialog(message, title, confirmText, cancelText, iconText) {
        return new Promise(function (resolve) {
            var existingOverlay = document.getElementById("appConfirmDialogOverlay");
            if (existingOverlay) {
                existingOverlay.remove();
            }

            var overlay = document.createElement("div");
            overlay.id = "appConfirmDialogOverlay";
            overlay.className = "app-confirm-dialog-overlay";

            var dialog = document.createElement("div");
            dialog.className = "app-confirm-dialog";
            dialog.setAttribute("role", "dialog");
            dialog.setAttribute("aria-modal", "true");
            dialog.setAttribute("aria-labelledby", "appConfirmDialogTitle");
            dialog.setAttribute("aria-describedby", "appConfirmDialogMessage");

            var icon = document.createElement("div");
            icon.className = "app-confirm-dialog-icon";
            icon.textContent = iconText || "!";

            var titleElement = document.createElement("div");
            titleElement.id = "appConfirmDialogTitle";
            titleElement.className = "app-confirm-dialog-title";
            titleElement.textContent = title || "Conferma operazione";

            var messageElement = document.createElement("div");
            messageElement.id = "appConfirmDialogMessage";
            messageElement.className = "app-confirm-dialog-message";
            messageElement.textContent = message || "Confermi l'operazione?";

            var actions = document.createElement("div");
            actions.className = "app-confirm-dialog-actions";

            var cancelButton = document.createElement("button");
            cancelButton.type = "button";
            cancelButton.className = "app-confirm-dialog-button app-confirm-dialog-button--secondary";
            cancelButton.textContent = cancelText || "Annulla";

            var confirmButton = document.createElement("button");
            confirmButton.type = "button";
            confirmButton.className = "app-confirm-dialog-button app-confirm-dialog-button--primary";
            confirmButton.textContent = confirmText || "Conferma";

            var previousOverflow = document.body.style.overflow || "";
            var closed = false;

            function closeDialog(result) {
                if (closed) {
                    return;
                }

                closed = true;
                document.removeEventListener("keydown", keyHandler);
                document.body.style.overflow = previousOverflow;
                overlay.remove();
                resolve(result === true);
            }

            function keyHandler(event) {
                closeOnKeys(event, closeDialog, false);
            }

            overlay.addEventListener("click", function (event) {
                if (event.target === overlay) {
                    closeDialog(false);
                }
            });
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

            document.body.style.overflow = "hidden";
            document.addEventListener("keydown", keyHandler);
            window.setTimeout(function () {
                cancelButton.focus();
            }, 30);
        });
    }

    window.molinaBlazorDialog = {
        toast: function (message, type) {
            var toast = ensureToast();
            toast.textContent = message || "";
            toast.className = "fallback-toast is-visible" + (type ? " is-" + type : "");
            window.clearTimeout(toast.__molinaTimer);
            toast.__molinaTimer = window.setTimeout(function () {
                toast.className = "fallback-toast";
            }, 3200);
        },
        confirm: function (message, title, confirmText, cancelText, iconText) {
            return showConfirmDialog(message, title, confirmText, cancelText, iconText);
        },
        success: function (message, title) {
            return showSuccessDialog(message, title);
        }
    };
})();
