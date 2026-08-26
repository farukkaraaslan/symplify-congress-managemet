(function () {
    "use strict";

    const historyCard = document.getElementById("bulkEmailHistoryCard");
    if (!historyCard) {
        return;
    }

    const congressSelect = document.getElementById("bulkEmailHistoryCongressId");
    const historyRefreshButton = document.getElementById("bulkEmailHistoryRefreshButton");
    const historySearchInput = document.getElementById("bulkEmailHistorySearch");
    const historyStatusSelect = document.getElementById("bulkEmailHistoryStatus");
    const historyOpenedSelect = document.getElementById("bulkEmailHistoryOpened");
    const historyTableBody = document.getElementById("bulkEmailHistoryTableBody");
    const historyPreviousButton = document.getElementById("bulkEmailHistoryPreviousButton");
    const historyNextButton = document.getElementById("bulkEmailHistoryNextButton");
    const historyPageSummary = document.getElementById("bulkEmailHistoryPageSummary");

    let historyPageIndex = 1;
    let historyTotalPages = 0;
    let historyAbortController = null;
    let historySearchTimer = null;

    function setBusy(button, busy) {
        if (!button) {
            return;
        }

        if (busy) {
            if (!button.dataset.originalText) {
                button.dataset.originalText = button.innerHTML;
            }

            button.disabled = true;
            button.innerHTML = `<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>${historyCard.dataset.refreshLoading || "İşlem yapılıyor..."}`;
            return;
        }

        button.disabled = false;
        if (button.dataset.originalText) {
            button.innerHTML = button.dataset.originalText;
        }
    }
    function getHistoryText(name, fallback) {
        return historyCard?.dataset[name] || fallback;
    }

    function setHistorySummary(payload) {
        const pendingCount = Number(payload.pendingCount || 0);
        const sentCount = Number(payload.sentCount || 0);
        const failedCount = Number(payload.failedCount || 0);
        const cancelledCount = Number(payload.cancelledCount || 0);
        const overallCount = pendingCount + sentCount + failedCount + cancelledCount;

        document.getElementById("bulkEmailHistoryTotalCount").textContent = String(overallCount);
        document.getElementById("bulkEmailHistoryPendingCount").textContent = String(pendingCount);
        document.getElementById("bulkEmailHistorySentCount").textContent = String(sentCount);
        document.getElementById("bulkEmailHistoryFailedCount").textContent = String(failedCount);
        document.getElementById("bulkEmailHistoryOpenedCount").textContent = String(payload.openedCount || 0);
    }

    function renderHistoryLoading() {
        if (!historyTableBody) {
            return;
        }

        historyTableBody.replaceChildren();
        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 8;
        cell.className = "text-center text-secondary-light py-24";
        cell.textContent = getHistoryText("loading", "Gönderim geçmişi yükleniyor...");
        row.appendChild(cell);
        historyTableBody.appendChild(row);
    }

    function renderHistoryMessage(message, className) {
        historyTableBody.replaceChildren();
        const row = document.createElement("tr");
        const cell = document.createElement("td");
        cell.colSpan = 8;
        cell.className = className || "text-center text-secondary-light py-24";
        cell.textContent = message;
        row.appendChild(cell);
        historyTableBody.appendChild(row);
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "-";
        }

        try {
            return new Intl.DateTimeFormat(historyCard?.dataset.culture || "tr-TR", {
                dateStyle: "short",
                timeStyle: "short"
            }).format(date);
        } catch {
            return date.toLocaleString();
        }
    }

    function getAudienceText(value) {
        switch (Number(value)) {
            case 1:
                return getHistoryText("audienceAll", "Tüm Kayıt Olanlara");
            case 2:
                return getHistoryText("audienceAcceptedCorresponding", "Sadece Kabul Alanlara");
            case 3:
                return getHistoryText("audienceAcceptedAll", "Kabul Alan Tüm Yazarlara");
            case 4:
                return getHistoryText("audiencePaymentPending", "Ücret Ödemeyenlere");
            case 5:
                return getHistoryText("audiencePaymentCompleted", "Ücret Ödeyenlere");
            default:
                return "-";
        }
    }

    function createBadge(text, className) {
        const badge = document.createElement("span");
        badge.className = `badge ${className}`;
        badge.textContent = text;
        return badge;
    }

    function createSendStatusBadge(status) {
        switch (Number(status)) {
            case 1:
                return createBadge(getHistoryText("statusPending", "Kuyrukta"), "bg-warning-focus text-warning-main");
            case 2:
                return createBadge(getHistoryText("statusSent", "Gönderildi"), "bg-success-focus text-success-main");
            case 3:
                return createBadge(getHistoryText("statusFailed", "Başarısız"), "bg-danger-focus text-danger-main");
            case 4:
                return createBadge(getHistoryText("statusCancelled", "İptal Edildi"), "bg-neutral-200 text-neutral-700");
            default:
                return createBadge("-", "bg-neutral-200 text-neutral-700");
        }
    }

    function createOpenStatus(item) {
        const wrapper = document.createElement("div");
        wrapper.className = "d-flex flex-column align-items-start gap-1";

        if (item.firstOpenedAt) {
            wrapper.appendChild(createBadge(getHistoryText("opened", "Açıldı"), "bg-info-focus text-info-main"));

            const detail = document.createElement("small");
            const openCountText = (getHistoryText("openCount", "{0} kez"))
                .replace("{0}", String(item.openCount || 1));
            detail.className = "text-secondary-light";
            detail.textContent = `${formatDate(item.firstOpenedAt)} · ${openCountText}`;
            wrapper.appendChild(detail);
            return wrapper;
        }

        if (Number(item.status) === 2) {
            wrapper.appendChild(createBadge(getHistoryText("notOpened", "Açılmadı"), "bg-neutral-200 text-neutral-700"));
            return wrapper;
        }

        wrapper.appendChild(createBadge(getHistoryText("openPending", "Henüz gönderilmedi"), "bg-neutral-100 text-neutral-600"));
        return wrapper;
    }

    function createTextCell(value, className) {
        const cell = document.createElement("td");
        if (className) {
            cell.className = className;
        }
        cell.textContent = value || "-";
        return cell;
    }

    function renderHistoryItems(items) {
        historyTableBody.replaceChildren();

        if (!Array.isArray(items) || items.length === 0) {
            renderHistoryMessage(getHistoryText("empty", "Bu kongre için henüz toplu e-posta kaydı bulunmuyor."));
            return;
        }

        items.forEach((item) => {
            const row = document.createElement("tr");

            row.appendChild(createTextCell(formatDate(item.createdAt), "text-nowrap"));

            const recipientCell = document.createElement("td");
            const name = document.createElement("div");
            name.className = "fw-semibold";
            name.textContent = item.recipientName || "-";
            const email = document.createElement("small");
            email.className = "text-secondary-light";
            email.textContent = item.recipientEmail || "-";
            recipientCell.append(name, email);
            row.appendChild(recipientCell);

            const subjectCell = createTextCell(item.subject || "-");
            subjectCell.style.minWidth = "220px";
            row.appendChild(subjectCell);

            row.appendChild(createTextCell(getAudienceText(item.audienceType)));

            const statusCell = document.createElement("td");
            statusCell.appendChild(createSendStatusBadge(item.status));
            row.appendChild(statusCell);

            const openCell = document.createElement("td");
            openCell.appendChild(createOpenStatus(item));
            row.appendChild(openCell);

            row.appendChild(createTextCell(formatDate(item.sentAt), "text-nowrap"));

            const errorCell = document.createElement("td");
            const errorValue = item.lastError || getHistoryText("noError", "-");
            errorCell.textContent = errorValue;
            if (item.lastError) {
                errorCell.className = "text-danger-600 text-sm";
                errorCell.title = item.lastError;
                errorCell.style.maxWidth = "240px";
                errorCell.style.whiteSpace = "nowrap";
                errorCell.style.overflow = "hidden";
                errorCell.style.textOverflow = "ellipsis";
            }
            row.appendChild(errorCell);

            historyTableBody.appendChild(row);
        });
    }

    function updateHistoryPagination(payload) {
        historyPageIndex = Number(payload.pageIndex || 1);
        historyTotalPages = Number(payload.totalPages || 0);

        historyPreviousButton.disabled = historyPageIndex <= 1 || historyTotalPages === 0;
        historyNextButton.disabled = historyTotalPages === 0 || historyPageIndex >= historyTotalPages;

        if (historyTotalPages === 0) {
            historyPageSummary.textContent = "";
            return;
        }

        historyPageSummary.textContent = (getHistoryText("pageSummary", "Sayfa {0} / {1} · Toplam {2} kayıt"))
            .replace("{0}", String(historyPageIndex))
            .replace("{1}", String(historyTotalPages))
            .replace("{2}", String(payload.totalCount || 0));
    }

    async function loadHistory(pageIndex) {
        if (!historyCard || !historyTableBody) {
            return;
        }

        const congressId = congressSelect?.value;
        if (!congressId) {
            setHistorySummary({});
            historyPageSummary.textContent = "";
            renderHistoryMessage(historyCard.dataset.selectCongress || "Lütfen bir kongre seçiniz.");
            return;
        }

        historyAbortController?.abort();
        historyAbortController = new AbortController();

        renderHistoryLoading();
        setBusy(historyRefreshButton, true);

        const url = new URL(historyCard.dataset.historyUrl, window.location.origin);
        url.searchParams.set("congressId", congressId);
        url.searchParams.set("pageIndex", String(Math.max(1, pageIndex || 1)));
        url.searchParams.set("pageSize", "25");

        const status = historyStatusSelect?.value;
        const opened = historyOpenedSelect?.value;
        const search = historySearchInput?.value?.trim();

        if (status) {
            url.searchParams.set("status", status);
        }
        if (opened) {
            url.searchParams.set("opened", opened);
        }
        if (search) {
            url.searchParams.set("search", search);
        }

        try {
            const response = await fetch(url.toString(), {
                method: "GET",
                headers: {
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                },
                signal: historyAbortController.signal
            });

            const payload = await response.json().catch(() => ({}));
            if (!response.ok || payload.success === false) {
                throw new Error(payload.message || getHistoryText("loadError", "Gönderim geçmişi yüklenemedi."));
            }

            setHistorySummary(payload);
            renderHistoryItems(payload.items || []);
            updateHistoryPagination(payload);
        } catch (error) {
            if (error.name === "AbortError") {
                return;
            }

            renderHistoryMessage(
                error.message || getHistoryText("loadError", "Gönderim geçmişi yüklenemedi."),
                "text-center text-danger-600 py-24");
        } finally {
            setBusy(historyRefreshButton, false);
        }
    }

    function syncCongressQueryString() {
        const url = new URL(window.location.href);
        const congressId = congressSelect?.value;

        if (congressId) {
            url.searchParams.set("congressId", congressId);
        } else {
            url.searchParams.delete("congressId");
        }

        url.searchParams.delete("batchId");
        window.history.replaceState({}, "", url.toString());
    }

    congressSelect?.addEventListener("change", () => {
        historyPageIndex = 1;
        syncCongressQueryString();
        loadHistory(1);
    });

    historyRefreshButton?.addEventListener("click", () => loadHistory(historyPageIndex));
    historyStatusSelect?.addEventListener("change", () => loadHistory(1));
    historyOpenedSelect?.addEventListener("change", () => loadHistory(1));

    historyPreviousButton?.addEventListener("click", () => {
        if (historyPageIndex > 1) {
            loadHistory(historyPageIndex - 1);
        }
    });

    historyNextButton?.addEventListener("click", () => {
        if (historyPageIndex < historyTotalPages) {
            loadHistory(historyPageIndex + 1);
        }
    });

    historySearchInput?.addEventListener("input", () => {
        window.clearTimeout(historySearchTimer);
        historySearchTimer = window.setTimeout(() => loadHistory(1), 400);
    });

    if (congressSelect?.value) {
        loadHistory(1);
    } else {
        setHistorySummary({});
        historyPageSummary.textContent = "";
        renderHistoryMessage(historyCard.dataset.selectCongress || "Lütfen bir kongre seçiniz.");
    }

    window.setInterval(() => {
        if (!document.hidden && congressSelect?.value) {
            loadHistory(historyPageIndex);
        }
    }, 30000);
})();
