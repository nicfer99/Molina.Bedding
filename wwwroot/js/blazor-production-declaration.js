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
        }
    };

    window.molinaBlazorDialog = {
        toast: function (message) {
            var toast = document.getElementById("globalToast");
            if (!toast) {
                toast = document.createElement("div");
                toast.id = "globalToast";
                toast.className = "fallback-toast";
                toast.setAttribute("aria-live", "polite");
                document.body.appendChild(toast);
            }

            toast.textContent = message || "";
            toast.className = "fallback-toast is-visible";
            window.clearTimeout(toast.__molinaTimer);
            toast.__molinaTimer = window.setTimeout(function () {
                toast.className = "fallback-toast";
            }, 3200);
        },
        confirm: function (message) {
            return window.confirm(message || "Confermi l'operazione?");
        },
        success: function (message, title) {
            window.alert((title ? title + "\n\n" : "") + (message || "Operazione completata correttamente."));
        }
    };
})();
