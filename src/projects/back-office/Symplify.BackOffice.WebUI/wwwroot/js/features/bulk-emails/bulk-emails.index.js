(() => {
    "use strict";

    const form = document.getElementById("bulkEmailForm");
    if (!form) {
        return;
    }

    const congressSelect = document.getElementById("bulkEmailCongressId");
    const audienceSelect = document.getElementById("bulkEmailAudienceType");
    const previewRecipientsButton = document.getElementById("bulkEmailPreviewRecipientsButton");
    const previewContentButton = document.getElementById("bulkEmailPreviewContentButton");
    const queueButton = document.getElementById("bulkEmailQueueButton");
    const recipientCountElement = document.getElementById("bulkEmailRecipientCount");
    const invalidEmailSummary = document.getElementById("bulkEmailInvalidEmailSummary");
    const recipientLoadingText = document.getElementById("bulkEmailRecipientLoadingText");
    const linkWarnings = document.getElementById("bulkEmailLinkWarnings");
    const subjectInput = form.querySelector('[name="Subject"]');
    const titleInput = form.querySelector('[name="Title"]');
    const bodyInput = form.querySelector('[name="BodyText"]');

    const exclusionsInput = document.getElementById("bulkEmailExcludedRecipientEmailsJson");
    const additionsInput = document.getElementById("bulkEmailAdditionalRecipientsJson");

    const recipientsModalElement = document.getElementById("bulkEmailRecipientsModal");
    const recipientsModalSummary = document.getElementById("bulkEmailRecipientsModalSummary");
    const recipientsTableBody = document.getElementById("bulkEmailRecipientsTableBody");
    const recipientSearchInput = document.getElementById("bulkEmailRecipientSearch");
    const recipientPageSizeSelect = document.getElementById("bulkEmailRecipientPageSize");
    const recipientPageSummary = document.getElementById("bulkEmailRecipientPageSummary");
    const recipientPreviousButton = document.getElementById("bulkEmailRecipientPreviousButton");
    const recipientNextButton = document.getElementById("bulkEmailRecipientNextButton");
    const manualRecipientNameInput = document.getElementById("bulkEmailManualRecipientName");
    const manualRecipientEmailInput = document.getElementById("bulkEmailManualRecipientEmail");
    const addRecipientButton = document.getElementById("bulkEmailAddRecipientButton");
    const resetRecipientsButton = document.getElementById("bulkEmailResetRecipientsButton");

    const excludedEmails = new Set(readJsonArray(exclusionsInput?.value));
    const additionalRecipients = new Map();
    readJsonArray(additionsInput?.value).forEach((recipient) => {
        const email = normalizeEmail(recipient?.email || recipient?.Email || "");
        if (!email) {
            return;
        }

        additionalRecipients.set(email, {
            email,
            name: String(recipient?.name || recipient?.Name || "").trim()
        });
    });

    let currentRecipientCount = null;
    let currentPageIndex = 1;
    let currentTotalPages = 1;
    let currentPageSize = Number(recipientPageSizeSelect?.value || 25);
    let currentSearch = "";
    let submitting = false;
    let requestSequence = 0;
    let searchTimer = null;
    let filterTimer = null;

    syncAdjustmentInputs();

    function readJsonArray(value) {
        if (!value) {
            return [];
        }

        try {
            const parsed = JSON.parse(value);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function normalizeEmail(value) {
        return String(value || "").trim().toLowerCase();
    }

    function syncAdjustmentInputs() {
        if (exclusionsInput) {
            exclusionsInput.value = JSON.stringify(Array.from(excludedEmails));
        }

        if (additionsInput) {
            additionsInput.value = JSON.stringify(Array.from(additionalRecipients.values()));
        }
    }

    function buildFormData(values) {
        const parameters = new URLSearchParams();
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            parameters.set("__RequestVerificationToken", token);
        }

        Object.entries(values).forEach(([key, value]) => {
            parameters.set(key, value == null ? "" : String(value));
        });

        return parameters.toString();
    }

    async function postForm(url, values) {
        const response = await fetch(url, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: buildFormData(values)
        });

        const payload = await response.json().catch(() => ({}));
        if (!response.ok) {
            throw new Error(payload.message || form.dataset.genericError || "İşlem sırasında bir hata oluştu.");
        }

        return payload;
    }

    function setBusy(button, busy) {
        if (!button) {
            return;
        }

        if (busy) {
            if (!button.dataset.originalText) {
                button.dataset.originalText = button.innerHTML;
            }
            button.disabled = true;
            button.innerHTML = `<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>${form.dataset.loadingText || "İşlem yapılıyor..."}`;
            return;
        }

        button.disabled = false;
        if (button.dataset.originalText) {
            button.innerHTML = button.dataset.originalText;
        }
    }

    function setRecipientLoading(loading) {
        if (!recipientLoadingText) {
            return;
        }

        recipientLoadingText.textContent = loading
            ? (form.dataset.loadingRecipients || "Alıcılar yükleniyor...")
            : "";
    }

    function showError(message) {
        if (window.Swal) {
            window.Swal.fire({
                icon: "error",
                title: form.dataset.errorTitle || "Hata",
                text: message
            });
            return;
        }

        window.alert(message);
    }

    function getValidationSpan(input) {
        if (!input?.name) {
            return null;
        }

        return form.querySelector(`[data-valmsg-for="${CSS.escape(input.name)}"]`);
    }

    function setFieldError(input, message) {
        if (!input) {
            return;
        }

        input.setCustomValidity(message || "");
        input.classList.toggle("is-invalid", Boolean(message));
        input.setAttribute("aria-invalid", message ? "true" : "false");

        const span = getValidationSpan(input);
        if (span) {
            span.textContent = message || "";
            span.classList.toggle("field-validation-error", Boolean(message));
            span.classList.toggle("field-validation-valid", !message);
        }
    }

    function clearFieldError(input) {
        setFieldError(input, "");
    }

    function validateContentFields(options = {}) {
        const focusFirstInvalid = options.focusFirstInvalid !== false;
        let firstInvalid = null;

        const subject = String(subjectInput?.value || "");
        const title = String(titleInput?.value || "");
        const body = String(bodyInput?.value || "");

        clearFieldError(subjectInput);
        clearFieldError(titleInput);
        clearFieldError(bodyInput);

        if (!subject.trim()) {
            setFieldError(
                subjectInput,
                form.dataset.subjectRequired || "E-posta konusu zorunludur."
            );
            firstInvalid ||= subjectInput;
        } else if (subject.length > 200) {
            setFieldError(
                subjectInput,
                form.dataset.subjectTooLong || "E-posta konusu en fazla 200 karakter olabilir."
            );
            firstInvalid ||= subjectInput;
        } else if (subject.includes("\r") || subject.includes("\n")) {
            setFieldError(
                subjectInput,
                form.dataset.subjectInvalid || "E-posta konusu satır sonu içeremez."
            );
            firstInvalid ||= subjectInput;
        }

        if (!title.trim()) {
            setFieldError(
                titleInput,
                form.dataset.titleRequired || "Mail başlığı zorunludur."
            );
            firstInvalid ||= titleInput;
        } else if (title.length > 200) {
            setFieldError(
                titleInput,
                form.dataset.titleTooLong || "Mail başlığı en fazla 200 karakter olabilir."
            );
            firstInvalid ||= titleInput;
        }

        if (!body.trim()) {
            setFieldError(
                bodyInput,
                form.dataset.bodyRequired || "Mail içeriği zorunludur."
            );
            firstInvalid ||= bodyInput;
        } else if (body.length > 20000) {
            setFieldError(
                bodyInput,
                form.dataset.bodyTooLong || "Mail içeriği en fazla 20.000 karakter olabilir."
            );
            firstInvalid ||= bodyInput;
        }

        if (firstInvalid && focusFirstInvalid) {
            firstInvalid.focus();
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
        }

        return firstInvalid === null;
    }

    function updateRecipientSummary(payload) {
        currentRecipientCount = Number(payload.recipientCount || 0);
        recipientCountElement.textContent = String(currentRecipientCount);

        const invalidCount = Number(payload.invalidEmailCount || 0);
        if (invalidCount > 0) {
            invalidEmailSummary.textContent = (form.dataset.invalidSummary || "({0} geçersiz adres atlandı)")
                .replace("{0}", String(invalidCount));
            invalidEmailSummary.classList.remove("d-none");
        } else {
            invalidEmailSummary.textContent = "";
            invalidEmailSummary.classList.add("d-none");
        }
    }

    function resetRecipientSummary() {
        currentRecipientCount = null;
        recipientCountElement.textContent = "-";
        invalidEmailSummary.textContent = "";
        invalidEmailSummary.classList.add("d-none");
    }

    function resetRecipientAdjustments() {
        excludedEmails.clear();
        additionalRecipients.clear();
        syncAdjustmentInputs();
    }

    async function loadRecipients(options = {}) {
        const congressId = congressSelect?.value;
        const audienceType = audienceSelect?.value;
        const showModal = Boolean(options.showModal);
        const explicit = Boolean(options.explicit);
        const pageIndex = Number(options.pageIndex || currentPageIndex || 1);

        if (!congressId) {
            resetRecipientSummary();
            if (explicit) {
                showError(form.dataset.selectCongress || "Lütfen bir kongre seçiniz.");
            }
            return null;
        }

        const sequence = ++requestSequence;
        setRecipientLoading(true);
        if (explicit) {
            setBusy(previewRecipientsButton, true);
        }

        try {
            const payload = await postForm(form.dataset.previewRecipientsUrl, {
                congressId,
                audienceType,
                pageIndex,
                pageSize: currentPageSize,
                search: currentSearch,
                excludedRecipientEmailsJson: JSON.stringify(Array.from(excludedEmails)),
                additionalRecipientsJson: JSON.stringify(Array.from(additionalRecipients.values()))
            });

            if (sequence !== requestSequence) {
                return null;
            }

            currentPageIndex = Number(payload.pageIndex || 1);
            currentTotalPages = Number(payload.totalPages || 1);
            updateRecipientSummary(payload);
            renderRecipientsTable(payload);

            if (showModal && recipientsModalElement) {
                bootstrap.Modal.getOrCreateInstance(recipientsModalElement).show();
            }

            return payload;
        } catch (error) {
            if (sequence === requestSequence) {
                resetRecipientSummary();
                if (explicit || showModal) {
                    showError(error.message);
                }
            }
            return null;
        } finally {
            if (sequence === requestSequence) {
                setRecipientLoading(false);
            }
            if (explicit) {
                setBusy(previewRecipientsButton, false);
            }
        }
    }

    function renderRecipientsTable(payload) {
        if (!recipientsTableBody) {
            return;
        }

        const recipientCount = Number(payload.recipientCount || 0);
        const filteredCount = Number(payload.filteredCount || 0);
        const invalidCount = Number(payload.invalidEmailCount || 0);

        recipientsModalSummary.textContent = (form.dataset.recipientModalSummary || "{0} seçili alıcı · {1} filtre sonucu · {2} geçersiz adres")
            .replace("{0}", String(recipientCount))
            .replace("{1}", String(filteredCount))
            .replace("{2}", String(invalidCount));

        recipientsTableBody.replaceChildren();
        const recipients = Array.isArray(payload.recipients) ? payload.recipients : [];

        if (recipients.length === 0) {
            const row = document.createElement("tr");
            const cell = document.createElement("td");
            cell.colSpan = 4;
            cell.className = "text-center text-secondary-light py-24";
            cell.textContent = form.dataset.recipientNotFound || "Alıcı bulunamadı.";
            row.appendChild(cell);
            recipientsTableBody.appendChild(row);
        } else {
            recipients.forEach((recipient) => {
                const row = document.createElement("tr");
                const nameCell = document.createElement("td");
                const emailCell = document.createElement("td");
                const sourceCell = document.createElement("td");
                const actionCell = document.createElement("td");

                nameCell.textContent = recipient.name || "-";
                emailCell.textContent = recipient.email || "-";

                const sourceBadge = document.createElement("span");
                sourceBadge.className = recipient.isManual
                    ? "badge bg-warning-100 text-warning-600"
                    : "badge bg-primary-50 text-primary-600";
                sourceBadge.textContent = recipient.isManual
                    ? (form.dataset.sourceManual || "Manuel")
                    : (form.dataset.sourceFilter || "Filtre");
                sourceCell.appendChild(sourceBadge);

                actionCell.className = "text-end";
                const removeButton = document.createElement("button");
                removeButton.type = "button";
                removeButton.className = "btn btn-sm btn-outline-danger radius-8";
                removeButton.dataset.email = recipient.email || "";
                removeButton.dataset.manual = recipient.isManual ? "true" : "false";
                removeButton.innerHTML = '<i class="ri-user-unfollow-line me-1"></i>';
                const label = document.createElement("span");
                label.textContent = form.dataset.removeRecipient || "Listeden Çıkar";
                removeButton.appendChild(label);
                removeButton.addEventListener("click", removeRecipient);
                actionCell.appendChild(removeButton);

                row.append(nameCell, emailCell, sourceCell, actionCell);
                recipientsTableBody.appendChild(row);
            });
        }

        currentPageIndex = Number(payload.pageIndex || 1);
        currentTotalPages = Number(payload.totalPages || 1);

        recipientPageSummary.textContent = (form.dataset.recipientPageSummary || "Sayfa {0} / {1} · {2} kayıt")
            .replace("{0}", String(currentPageIndex))
            .replace("{1}", String(currentTotalPages))
            .replace("{2}", String(filteredCount));

        recipientPreviousButton.disabled = currentPageIndex <= 1;
        recipientNextButton.disabled = currentPageIndex >= currentTotalPages;
    }

    function removeRecipient(event) {
        const button = event.currentTarget;
        const email = normalizeEmail(button.dataset.email);
        const isManual = button.dataset.manual === "true";

        if (!email) {
            return;
        }

        if (isManual) {
            additionalRecipients.delete(email);
        } else {
            additionalRecipients.delete(email);
            excludedEmails.add(email);
        }

        syncAdjustmentInputs();
        loadRecipients({ pageIndex: currentPageIndex });
    }

    function addManualRecipient() {
        const email = normalizeEmail(manualRecipientEmailInput?.value);
        const name = String(manualRecipientNameInput?.value || "").trim();

        if (!email || !manualRecipientEmailInput?.checkValidity()) {
            manualRecipientEmailInput?.reportValidity();
            showError(form.dataset.manualEmailInvalid || "Geçerli bir e-posta adresi giriniz.");
            return;
        }

        excludedEmails.delete(email);
        additionalRecipients.set(email, { email, name });
        syncAdjustmentInputs();

        if (manualRecipientEmailInput) {
            manualRecipientEmailInput.value = "";
        }
        if (manualRecipientNameInput) {
            manualRecipientNameInput.value = "";
        }

        currentSearch = "";
        if (recipientSearchInput) {
            recipientSearchInput.value = "";
        }

        loadRecipients({ pageIndex: 1 });
    }

    async function resetManagedRecipients() {
        if (excludedEmails.size === 0 && additionalRecipients.size === 0) {
            return;
        }

        let confirmed = true;
        if (window.Swal) {
            const result = await window.Swal.fire({
                icon: "warning",
                title: form.dataset.resetConfirmTitle || "Alıcı listesini sıfırla",
                text: form.dataset.resetConfirmText || "Manuel eklenen ve çıkarılan alıcılar silinecek. Devam etmek istiyor musunuz?",
                showCancelButton: true,
                confirmButtonText: form.dataset.confirmButton || "Devam Et",
                cancelButtonText: form.dataset.cancelButton || "Vazgeç",
                reverseButtons: true
            });
            confirmed = result.isConfirmed;
        } else {
            confirmed = window.confirm(form.dataset.resetConfirmText || "Alıcı listesi sıfırlansın mı?");
        }

        if (!confirmed) {
            return;
        }

        resetRecipientAdjustments();
        loadRecipients({ pageIndex: 1 });
    }

    async function previewContent() {
        if (!validateContentFields()) {
            return;
        }

        const congressId = congressSelect?.value;
        if (!congressId) {
            showError(form.dataset.selectCongress || "Lütfen bir kongre seçiniz.");
            return;
        }

        setBusy(previewContentButton, true);
        clearLinkWarnings();

        try {
            const payload = await postForm(form.dataset.previewContentUrl, {
                congressId,
                culture: form.querySelector('[name="Culture"]')?.value || "tr-TR",
                subject: form.querySelector('[name="Subject"]')?.value || "",
                title: form.querySelector('[name="Title"]')?.value || "",
                bodyText: form.querySelector('[name="BodyText"]')?.value || ""
            });

            if (!payload.success) {
                renderUnsafeLinks(payload.unsafeLinks || [], payload.message);
                return;
            }

            renderWarningLinks(payload.warningLinks || []);

            document.getElementById("bulkEmailPreviewSubject").textContent = payload.subject || "";
            document.getElementById("bulkEmailPreviewFrame").srcdoc = payload.htmlBody || "";

            const modalElement = document.getElementById("bulkEmailContentModal");
            bootstrap.Modal.getOrCreateInstance(modalElement).show();
        } catch (error) {
            showError(error.message);
        } finally {
            setBusy(previewContentButton, false);
        }
    }

    function clearLinkWarnings() {
        linkWarnings.replaceChildren();
        linkWarnings.classList.add("d-none");
    }

    function renderUnsafeLinks(links, message) {
        clearLinkWarnings();
        linkWarnings.classList.remove("d-none");

        const title = document.createElement("strong");
        title.textContent = message || form.dataset.unsafeLinks || "Güvenli olmayan bağlantılar bulundu.";
        linkWarnings.appendChild(title);

        if (links.length > 0) {
            const list = document.createElement("ul");
            list.className = "mb-0 mt-2";
            links.forEach((link) => {
                const item = document.createElement("li");
                item.textContent = link;
                list.appendChild(item);
            });
            linkWarnings.appendChild(list);
        }
    }

    function renderWarningLinks(links) {
        clearLinkWarnings();
        if (!links.length) {
            return;
        }

        linkWarnings.classList.remove("d-none");
        const title = document.createElement("strong");
        title.textContent = form.dataset.httpWarning || "HTTP kullanan bağlantılar bulundu. Mümkünse HTTPS kullanınız.";
        linkWarnings.appendChild(title);

        const list = document.createElement("ul");
        list.className = "mb-0 mt-2";
        links.forEach((link) => {
            const item = document.createElement("li");
            item.textContent = link;
            list.appendChild(item);
        });
        linkWarnings.appendChild(list);
    }

    async function confirmQueue(event) {
        if (submitting) {
            return;
        }

        event.preventDefault();

        if (!validateContentFields()) {
            return;
        }

        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        // Always resolve the final list again before queueing. This prevents a stale count after filters or manual edits.
        const payload = await loadRecipients({ pageIndex: 1, explicit: false });
        if (!payload) {
            return;
        }

        const recipientCount = Number(payload.recipientCount || 0);
        if (recipientCount <= 0) {
            showError(form.dataset.noDeliverableRecipient || "Seçilen grupta gönderilebilir alıcı bulunamadı.");
            return;
        }

        const confirmText = (form.dataset.confirmText || "{0} alıcı için gönderim başlatılacak.")
            .replace("{0}", String(recipientCount));

        let confirmed = false;
        if (window.Swal) {
            const result = await window.Swal.fire({
                icon: "warning",
                title: form.dataset.confirmTitle || "Gönderimi Onayla",
                text: confirmText,
                showCancelButton: true,
                confirmButtonText: form.dataset.confirmButton || "Kuyruğa Al",
                cancelButtonText: form.dataset.cancelButton || "Vazgeç",
                reverseButtons: true
            });
            confirmed = result.isConfirmed;
        } else {
            confirmed = window.confirm(confirmText);
        }

        if (!confirmed) {
            return;
        }

        submitting = true;
        syncAdjustmentInputs();
        setBusy(queueButton, true);
        form.submit();
    }

    function handleAudienceFilterChanged() {
        const hadManualChanges = excludedEmails.size > 0 || additionalRecipients.size > 0;
        resetRecipientAdjustments();
        resetRecipientSummary();
        currentPageIndex = 1;
        currentSearch = "";

        if (recipientSearchInput) {
            recipientSearchInput.value = "";
        }

        window.clearTimeout(filterTimer);
        filterTimer = window.setTimeout(() => loadRecipients({ pageIndex: 1 }), 200);

        if (hadManualChanges) {
            recipientLoadingText.textContent = form.dataset.filterChangeNote || "Alıcı grubu değiştiği için manuel alıcı seçimleri sıfırlandı.";
        }
    }

    [subjectInput, titleInput, bodyInput].forEach((input) => {
        input?.addEventListener("input", () => clearFieldError(input));
    });

    previewRecipientsButton?.addEventListener("click", () => loadRecipients({
        showModal: true,
        explicit: true,
        pageIndex: 1
    }));

    previewContentButton?.addEventListener("click", previewContent);
    form.addEventListener("submit", confirmQueue);
    congressSelect?.addEventListener("change", handleAudienceFilterChanged);
    audienceSelect?.addEventListener("change", handleAudienceFilterChanged);
    addRecipientButton?.addEventListener("click", addManualRecipient);
    resetRecipientsButton?.addEventListener("click", resetManagedRecipients);

    manualRecipientEmailInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            addManualRecipient();
        }
    });

    recipientSearchInput?.addEventListener("input", () => {
        window.clearTimeout(searchTimer);
        searchTimer = window.setTimeout(() => {
            currentSearch = recipientSearchInput.value.trim();
            loadRecipients({ pageIndex: 1 });
        }, 350);
    });

    recipientPageSizeSelect?.addEventListener("change", () => {
        currentPageSize = Number(recipientPageSizeSelect.value || 25);
        loadRecipients({ pageIndex: 1 });
    });

    recipientPreviousButton?.addEventListener("click", () => {
        if (currentPageIndex > 1) {
            loadRecipients({ pageIndex: currentPageIndex - 1 });
        }
    });

    recipientNextButton?.addEventListener("click", () => {
        if (currentPageIndex < currentTotalPages) {
            loadRecipients({ pageIndex: currentPageIndex + 1 });
        }
    });

    if (congressSelect?.value) {
        loadRecipients({ pageIndex: 1 });
    }
})();
