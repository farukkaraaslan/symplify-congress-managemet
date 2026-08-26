(function () {
    "use strict";

    document.addEventListener("submit", function (event) {
        var form = event.target;

        if (!form.classList || !form.classList.contains("js-user-secure-action")) {
            form = form.closest ? form.closest("form") : null;
        }

        if (!form || !form.querySelector || !form.querySelector(".js-user-secure-action")) {
            return;
        }

        if (form.dataset.confirmed === "true") {
            form.dataset.confirmed = "false";
            return;
        }

        event.preventDefault();

        var title = form.getAttribute("data-confirm-title") || "İşlem onaylansın mı?";
        var text = form.getAttribute("data-confirm-text") || "Bu işlem güvenlik açısından önemlidir.";

        if (window.Swal) {
            window.Swal.fire({
                title: title,
                text: text,
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Onayla",
                cancelButtonText: "Vazgeç"
            }).then(function (result) {
                if (!result.isConfirmed) {
                    return;
                }

                form.dataset.confirmed = "true";
                form.submit();
            });

            return;
        }

        if (window.confirm(title + "\n" + text)) {
            form.dataset.confirmed = "true";
            form.submit();
        }
    });
}());
