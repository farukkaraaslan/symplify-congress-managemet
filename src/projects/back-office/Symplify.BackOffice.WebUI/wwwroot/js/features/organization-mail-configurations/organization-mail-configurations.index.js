(() => {
    "use strict";

    const selector = document.getElementById("organizationMailSelector");
    selector?.addEventListener("change", () => {
        const organizationId = selector.value;
        const baseUrl = selector.dataset.indexUrl;
        window.location.href = organizationId
            ? `${baseUrl}?organizationId=${encodeURIComponent(organizationId)}`
            : baseUrl;
    });

    const root = document.getElementById("organizationMailConfigurationRoot");
    if (!root) return;

    const organizationId = root.dataset.organizationId;
    const configurationForm = document.getElementById("organizationMailConfigurationForm");
    const testForm = document.getElementById("organizationMailTestForm");
    const deleteButton = document.getElementById("deleteOrganizationMailConfigurationButton");
    const passwordHint = document.getElementById("mailPasswordHint");
    const testStatus = document.getElementById("organizationMailTestStatus");
    const logoInput = document.getElementById("organizationMailLogoInput");
    const removeLogoCheckbox = document.getElementById("removeOrganizationMailLogo");
    const logoPreviewContainer = document.getElementById("organizationMailLogoPreviewContainer");

    const field = name => configurationForm?.querySelector(`[name="${name}"]`);

    // Form alanlarını disabled yapmak FormData dışında kalmalarına neden olur.
    // Bu nedenle yalnızca işlem butonlarını kilitliyoruz.
    const setBusy = (form, busy) => {
        if (!form) return;

        form.setAttribute("aria-busy", busy ? "true" : "false");
        form.querySelectorAll('button, input[type="submit"], input[type="button"]')
            .forEach(element => {
                element.disabled = busy;
            });
    };

    const showMessage = (message, type = "success") => {
        if (window.Swal) {
            Swal.fire({
                icon: type,
                text: message,
                confirmButtonText: "Tamam"
            });
            return;
        }

        window.alert(message);
    };

    const readError = async response => {
        try {
            const payload = await response.json();

            if (payload?.message) return payload.message;

            if (payload?.errors) {
                const validationMessages = Object.values(payload.errors)
                    .flatMap(value => Array.isArray(value) ? value : [value])
                    .filter(Boolean);

                if (validationMessages.length > 0) {
                    return validationMessages.join("\n");
                }
            }

            return "İşlem tamamlanamadı.";
        } catch {
            return "İşlem tamamlanamadı.";
        }
    };

    const renderLogoPreview = file => {
        if (!logoPreviewContainer) return;

        if (!file) {
            logoPreviewContainer.innerHTML = `
                <div class="text-center text-neutral-500 text-sm">
                    <i class="ri-image-line d-block text-xl mb-1"></i>
                    Mail logosu yüklenmedi
                </div>`;
            return;
        }

        const reader = new FileReader();
        reader.addEventListener("load", () => {
            logoPreviewContainer.innerHTML = `
                <img id="organizationMailLogoPreview"
                     src="${reader.result}"
                     alt="Mail logosu önizlemesi"
                     style="display:block;max-width:190px;max-height:86px;width:auto;height:auto;" />`;
        });
        reader.readAsDataURL(file);
    };

    logoInput?.addEventListener("change", () => {
        const file = logoInput.files?.[0];
        if (!file) return;

        const allowedTypes = new Set(["image/png", "image/jpeg"]);
        const allowedExtension = /\.(png|jpe?g)$/i.test(file.name || "");

        if (file.size > 300 * 1024 || (!allowedTypes.has(file.type) && !allowedExtension)) {
            logoInput.value = "";
            showMessage("Mail logosu PNG veya JPEG olmalı ve 300 KB boyutunu geçmemelidir.", "error");
            return;
        }

        if (removeLogoCheckbox) removeLogoCheckbox.checked = false;
        renderLogoPreview(file);
    });

    removeLogoCheckbox?.addEventListener("change", () => {
        if (!removeLogoCheckbox.checked) return;

        if (logoInput) logoInput.value = "";
        renderLogoPreview(null);
    });

    configurationForm?.addEventListener("submit", async event => {
        event.preventDefault();

        // FormData, herhangi bir alan kilitlenmeden önce oluşturulmalıdır.
        const formData = new FormData(configurationForm);
        formData.set("EnableSsl", field("EnableSsl")?.checked ? "true" : "false");
        formData.set("IsActive", field("IsActive")?.checked ? "true" : "false");

        setBusy(configurationForm, true);

        try {
            const response = await fetch(root.dataset.saveUrl, {
                method: "POST",
                body: formData,
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error(await readError(response));

            const payload = await response.json();
            const passwordField = field("Password");
            if (passwordField) passwordField.value = "";

            deleteButton?.classList.remove("d-none");

            if (passwordHint) {
                passwordHint.textContent =
                    "Kayıtlı parola vardır. Değiştirmek istemiyorsanız alanı boş bırakın.";
            }

            showMessage(payload.message || "Mail ayarları kaydedildi.");
        } catch (error) {
            showMessage(error.message || "Mail ayarları kaydedilemedi.", "error");
        } finally {
            setBusy(configurationForm, false);
        }
    });

    testForm?.addEventListener("submit", async event => {
        event.preventDefault();

        // Test formunda da FormData, buton kilitlenmeden önce alınır.
        const formData = new FormData(testForm);
        setBusy(testForm, true);

        try {
            const response = await fetch(root.dataset.testUrl, {
                method: "POST",
                body: formData,
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error(await readError(response));

            const payload = await response.json();
            showMessage(payload.message || "Test maili gönderildi.");

            if (testStatus) {
                testStatus.innerHTML =
                    '<div class="alert alert-success mb-0">Son test başarılı.</div>';
            }
        } catch (error) {
            showMessage(error.message || "Test maili gönderilemedi.", "error");

            if (testStatus) {
                testStatus.innerHTML =
                    '<div class="alert alert-danger mb-0">Son test başarısız.</div>';
            }
        } finally {
            setBusy(testForm, false);
        }
    });

    deleteButton?.addEventListener("click", async () => {
        if (!window.confirm("Bu organizasyonun mail ayarları kaldırılsın mı?")) return;

        const token = configurationForm
            ?.querySelector('input[name="__RequestVerificationToken"]')
            ?.value || "";

        const formData = new FormData();
        formData.append("organizationId", organizationId);
        formData.append("__RequestVerificationToken", token);

        setBusy(configurationForm, true);

        try {
            const response = await fetch(root.dataset.deleteUrl, {
                method: "POST",
                body: formData,
                credentials: "same-origin",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error(await readError(response));
            window.location.reload();
        } catch (error) {
            showMessage(error.message || "Mail ayarları kaldırılamadı.", "error");
            setBusy(configurationForm, false);
        }
    });
})();
