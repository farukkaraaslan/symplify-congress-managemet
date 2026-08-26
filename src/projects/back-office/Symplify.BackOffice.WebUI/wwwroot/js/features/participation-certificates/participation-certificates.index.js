(function ($) {
    'use strict';

    const page = document.getElementById('participationCertificatePage');
    if (!page || !page.dataset.congressId || page.dataset.congressId === '00000000-0000-0000-0000-000000000000') {
        return;
    }

    const congressId = page.dataset.congressId;
    const antiForgeryToken = document.querySelector('#participationCertificateAntiForgeryForm input[name="__RequestVerificationToken"]')?.value || '';
    const candidateState = createSelectionState();
    const mailState = createSelectionState();
    let candidateTable = null;
    let documentsTable = null;
    let mailTable = null;

    function createSelectionState() {
        return {
            allFiltered: false,
            allFilteredSearch: '',
            selected: new Set(),
            excluded: new Set(),
            clear: function () {
                this.allFiltered = false;
                this.allFilteredSearch = '';
                this.selected.clear();
                this.excluded.clear();
            },
            selectAllFiltered: function (searchText) {
                this.allFiltered = true;
                this.allFilteredSearch = (searchText || '').trim();
                this.selected.clear();
                this.excluded.clear();
            },
            isSelected: function (key) {
                return this.allFiltered ? !this.excluded.has(key) : this.selected.has(key);
            },
            setSelected: function (key, selected) {
                if (!key) return;
                if (this.allFiltered) {
                    if (selected) this.excluded.delete(key);
                    else this.excluded.add(key);
                    return;
                }
                if (selected) this.selected.add(key);
                else this.selected.delete(key);
            }
        };
    }

    function ajaxHeaders() {
        return antiForgeryToken ? { 'RequestVerificationToken': antiForgeryToken } : {};
    }

    function dataTableLanguage() {
        return {
            processing: 'İşleniyor...',
            search: 'Ara:',
            lengthMenu: '_MENU_ kayıt göster',
            info: '_TOTAL_ kayıttan _START_ - _END_ arası',
            infoEmpty: 'Kayıt bulunamadı',
            infoFiltered: '(_MAX_ kayıt içerisinden filtrelendi)',
            zeroRecords: 'Eşleşen kayıt bulunamadı',
            emptyTable: 'Gösterilecek kayıt yok',
            paginate: { first: 'İlk', previous: 'Önceki', next: 'Sonraki', last: 'Son' }
        };
    }

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : String(value)).html();
    }

    function formatDate(value) {
        if (!value) return '-';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '-' : date.toLocaleString('tr-TR');
    }

    function showMessage(icon, title, text) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            return window.Swal.fire({ icon: icon, title: title, text: text, confirmButtonText: 'Tamam' });
        }
        window.alert(text || title);
        return Promise.resolve();
    }

    function confirmAction(title, text, confirmButtonText) {
        if (window.Swal && typeof window.Swal.fire === 'function') {
            return window.Swal.fire({
                icon: 'question',
                title: title,
                text: text,
                showCancelButton: true,
                confirmButtonText: confirmButtonText,
                cancelButtonText: 'Vazgeç'
            }).then(result => result.isConfirmed);
        }
        return Promise.resolve(window.confirm(text));
    }

    async function postJson(url, payload) {
        const response = await fetch(url, {
            method: 'POST',
            cache: 'no-store',
            headers: Object.assign({ 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }, ajaxHeaders()),
            body: JSON.stringify(payload)
        });

        let body = null;
        try { body = await response.json(); } catch (_) { body = null; }
        if (!response.ok) {
            throw new Error(body?.message || 'İşlem tamamlanamadı.');
        }
        return body;
    }

    function initializeDocumentsTable() {
        const $table = $('#participationCertificateDocumentsTable');
        if (!$table.length || !$.fn.DataTable) return;

        documentsTable = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: true,
            paging: true,
            pageLength: 25,
            autoWidth: false,
            scrollX: true,
            order: [[3, 'desc']],
            ajax: {
                url: page.dataset.documentUrl,
                type: 'POST',
                headers: ajaxHeaders(),
                data: function (data) {
                    data.congressId = congressId;
                    data.certificateCulture = $('#documentCultureFilter').val() || '';
                    data.emailStatus = $('#documentEmailStatusFilter').val() || '';
                    data.includeRevoked = $('#includeRevokedDocuments').is(':checked');
                    return data;
                },
                error: function () { showMessage('error', 'Liste yüklenemedi', 'Oluşturulan belgeler alınamadı.'); }
            },
            columns: [
                { data: 'submissionNumber', name: 'submissionNumber', render: value => `<strong>${escapeHtml(value)}</strong>` },
                { data: null, name: 'author', render: renderAuthor },
                { data: 'culture', name: 'culture', className: 'text-nowrap', render: renderCulture },
                { data: 'generatedAt', name: 'generatedAt', className: 'text-nowrap', render: formatDate },
                { data: null, name: 'emailStatus', className: 'text-nowrap', render: renderEmailStatus },
                { data: null, name: 'publishedAt', className: 'text-nowrap', render: renderPublicationStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderDocumentActions }
            ],
            language: dataTableLanguage()
        });

        $('#documentCultureFilter,#documentEmailStatusFilter,#includeRevokedDocuments').on('change', function () {
            documentsTable.ajax.reload();
        });
    }

    function renderAuthor(_, __, row) {
        const email = row.authorEmail ? `<small class="d-block text-neutral-500">${escapeHtml(row.authorEmail)}</small>` : '';
        const title = row.submissionTitle ? `<small class="d-block text-neutral-500 text-truncate" style="max-width:340px" title="${escapeHtml(row.submissionTitle)}">${escapeHtml(row.submissionTitle)}</small>` : '';
        return `<strong>${escapeHtml(row.authorFullName)}</strong>${email}${title}`;
    }

    function renderCulture(value) {
        const isEnglish = String(value).toLowerCase() === 'en-us';
        return `<span class="badge ${isEnglish ? 'bg-info-focus text-info-main' : 'bg-primary-focus text-primary-main'}">${isEnglish ? 'EN' : 'TR'}</span>`;
    }

    function renderEmailStatus(_, __, row) {
        if (row.isRevoked) return '<span class="badge bg-danger-focus text-danger-main">Kaldırıldı</span>';
        if (row.emailSentAt) return `<span class="badge bg-success-focus text-success-main">Gönderildi</span><small class="d-block mt-1">${formatDate(row.emailSentAt)}</small>`;
        const status = row.emailStatus || 'Gönderilmedi';
        const css = status === 'Failed' ? 'bg-danger-focus text-danger-main' :
            ['QueueRequested', 'QueuePreparing', 'Queued'].includes(status) ? 'bg-warning-focus text-warning-main' : 'bg-neutral-200 text-neutral-700';
        return `<span class="badge ${css}">${escapeHtml(status)}</span>`;
    }

    function renderPublicationStatus(_, __, row) {
        if (row.isRevoked) return '<span class="text-danger">Link iptal</span>';
        if (row.isPublished) return '<span class="text-success"><i class="ri-shield-check-line"></i> Public</span>';
        return '<span class="text-neutral-500"><i class="ri-lock-line"></i> Private</span>';
    }

    function renderDocumentActions(_, __, row) {
        if (row.isRevoked) return '<span class="text-neutral-400">İşlem yok</span>';
        const downloadUrl = `${page.dataset.downloadBaseUrl}${encodeURIComponent(row.id)}`;
        return `<div class="d-inline-flex gap-1">
            <a class="btn btn-sm btn-outline-primary-600" href="${downloadUrl}" title="İndir"><i class="ri-download-2-line"></i></a>
            <button type="button" class="btn btn-sm btn-outline-danger-600 js-revoke-certificate" data-id="${escapeHtml(row.id)}" title="Belgeyi kaldır"><i class="ri-delete-bin-6-line"></i></button>
        </div>`;
    }

    function initializeCandidateTable() {
        const $table = $('#participationCertificateCandidatesTable');
        if (!$table.length || candidateTable || !$.fn.DataTable) return;

        candidateTable = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            searchDelay: 400,
            ordering: true,
            paging: true,
            pageLength: 25,
            autoWidth: false,
            scrollX: true,
            order: [[1, 'asc']],
            ajax: {
                url: page.dataset.candidateUrl,
                type: 'POST',
                headers: ajaxHeaders(),
                data: function (data) {
                    data.congressId = congressId;
                    data.submissionStatusCode = $('#candidateSubmissionStatusFilter').val() || '';
                    data.paymentStatusCode = $('#candidatePaymentStatusFilter').val() || '';
                    return data;
                },
                error: function () { showMessage('error', 'Liste yüklenemedi', 'Belge adayları alınamadı.'); }
            },
            columns: [
                { data: null, name: 'selection', orderable: false, searchable: false, className: 'text-center', render: renderCandidateCheckbox },
                { data: 'submissionNumber', name: 'submissionNumber', className: 'text-nowrap', render: value => `<strong>${escapeHtml(value)}</strong>` },
                { data: null, name: 'title', render: renderCandidateSubmission },
                { data: null, name: 'authors', render: renderCandidateAuthors },
                { data: null, name: 'status', className: 'text-nowrap', render: renderCandidateStatus },
                { data: null, name: 'certificate', className: 'text-nowrap', render: renderCandidateCertificateStatus }
            ],
            drawCallback: refreshCandidateCheckboxes,
            language: dataTableLanguage()
        });
    }

    function renderCandidateCheckbox(_, __, row) {
        const disabled = row.isEligible ? '' : 'disabled';
        return `<input type="checkbox" class="form-check-input js-candidate-checkbox" data-key="${escapeHtml(row.generationKey)}" ${disabled} />`;
    }

    function renderCandidateSubmission(_, __, row) {
        return `<div style="min-width:260px"><strong>${escapeHtml(row.submissionTitle)}</strong><small class="d-block text-neutral-500">${escapeHtml(row.submissionTypeName || '-')}</small></div>`;
    }

    function renderCandidateAuthors(_, __, row) {
        const names = escapeHtml(row.authorNames || '-');
        const count = Number(row.authorCount || 0);
        const emails = row.authorEmails
            ? `<small class="d-block text-neutral-500 text-truncate" style="max-width:320px" title="${escapeHtml(row.authorEmails)}">${escapeHtml(row.authorEmails)}</small>`
            : '<small class="d-block text-danger">E-posta bilgisi olmayan yazar bulunuyor.</small>';
        return `<div style="min-width:280px"><strong>${count} yazar</strong><small class="d-block text-neutral-700 text-truncate" style="max-width:360px" title="${names}">${names}</small>${emails}</div>`;
    }

    function renderCandidateStatus(_, __, row) {
        const eligibility = row.isEligible
            ? '<span class="badge bg-success-focus text-success-main">Uygun</span>'
            : '<span class="badge bg-danger-focus text-danger-main">Uygun değil</span>';
        return `${eligibility}<small class="d-block mt-1">${escapeHtml(row.submissionStatusName || '-')} / ${escapeHtml(row.paymentStatusName || '-')}</small>`;
    }

    function renderCandidateCertificateStatus(_, __, row) {
        const authorCount = Number(row.authorCount || 0);
        const trCount = Number(row.turkishCertificateCount || 0);
        const enCount = Number(row.englishCertificateCount || 0);
        const trCss = authorCount > 0 && trCount >= authorCount
            ? 'bg-primary-focus text-primary-main'
            : trCount > 0 ? 'bg-warning-focus text-warning-main' : 'bg-neutral-200 text-neutral-700';
        const enCss = authorCount > 0 && enCount >= authorCount
            ? 'bg-info-focus text-info-main'
            : enCount > 0 ? 'bg-warning-focus text-warning-main' : 'bg-neutral-200 text-neutral-700';
        return `<span class="badge ${trCss} me-1">TR ${trCount}/${authorCount}</span><span class="badge ${enCss}">EN ${enCount}/${authorCount}</span>`;
    }

    function refreshCandidateCheckboxes() {
        $('#participationCertificateCandidatesTable .js-candidate-checkbox').each(function () {
            const key = this.dataset.key;
            this.checked = candidateState.isSelected(key);
        });
        updateCandidateSummary();
        updateCandidatePageSelect();
    }

    function updateCandidateSummary() {
        let text;
        if (candidateState.allFiltered) {
            const scope = candidateState.allFilteredSearch
                ? ` Seçim kapsamı: “${candidateState.allFilteredSearch}”.`
                : ' Seçim kapsamı: mevcut durum ve ödeme filtrelerindeki tüm bildiriler.';
            text = `Filtrelenen tüm uygun bildiriler seçildi. Seçimden çıkarılan bildiri: ${candidateState.excluded.size}.${scope}`;
        } else {
            text = `Seçili bildiri: ${candidateState.selected.size}`;
        }
        $('#candidateSelectionSummary').text(text);
    }

    function updateCandidatePageSelect() {
        if (!candidateTable) return;
        const eligibleRows = candidateTable.rows({ page: 'current' }).data().toArray().filter(row => row.isEligible);
        const selectedCount = eligibleRows.filter(row => candidateState.isSelected(row.generationKey)).length;
        const checkbox = document.getElementById('candidatePageSelect');
        if (!checkbox) return;
        checkbox.checked = eligibleRows.length > 0 && selectedCount === eligibleRows.length;
        checkbox.indeterminate = selectedCount > 0 && selectedCount < eligibleRows.length;
    }

    function initializeMailTable() {
        const $table = $('#participationCertificateMailTable');
        if (!$table.length || mailTable || !$.fn.DataTable) return;

        mailTable = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            searchDelay: 400,
            ordering: true,
            paging: true,
            pageLength: 25,
            autoWidth: false,
            scrollX: true,
            order: [[4, 'desc']],
            ajax: {
                url: page.dataset.documentUrl,
                type: 'POST',
                headers: ajaxHeaders(),
                data: function (data) {
                    data.congressId = congressId;
                    data.certificateCulture = $('#mailCultureFilter').val() || '';
                    data.emailStatus = $('#mailStatusFilter').val() || 'NotSent';
                    data.includeRevoked = false;
                    return data;
                },
                error: function () { showMessage('error', 'Liste yüklenemedi', 'Mail gönderilebilecek belgeler alınamadı.'); }
            },
            columns: [
                { data: null, name: 'selection', orderable: false, searchable: false, className: 'text-center', render: renderMailCheckbox },
                { data: 'submissionNumber', name: 'submissionNumber', render: value => `<strong>${escapeHtml(value)}</strong>` },
                { data: null, name: 'author', render: renderAuthor },
                { data: 'culture', name: 'culture', render: renderCulture },
                { data: 'generatedAt', name: 'generatedAt', className: 'text-nowrap', render: formatDate },
                { data: null, name: 'emailStatus', render: renderEmailStatus }
            ],
            drawCallback: refreshMailCheckboxes,
            language: dataTableLanguage()
        });

        mailTable.on('search.dt', function () {
            if (mailState.allFiltered) {
                mailState.clear();
                updateMailSummary();
                updateMailPageSelect();
            }
        });
    }

    function renderMailCheckbox(_, __, row) {
        const disabled = row.canQueueEmail ? '' : 'disabled';
        return `<input type="checkbox" class="form-check-input js-mail-checkbox" data-id="${escapeHtml(row.id)}" ${disabled} />`;
    }

    function refreshMailCheckboxes() {
        $('#participationCertificateMailTable .js-mail-checkbox').each(function () {
            this.checked = mailState.isSelected(this.dataset.id);
        });
        updateMailSummary();
        updateMailPageSelect();
    }

    function updateMailSummary() {
        const text = mailState.allFiltered
            ? `Filtrelenen tüm gönderilebilir belgeler seçildi. Seçimden çıkarılan: ${mailState.excluded.size}`
            : `Seçili belge: ${mailState.selected.size}`;
        $('#mailSelectionSummary').text(text);
    }

    function updateMailPageSelect() {
        if (!mailTable) return;
        const eligibleRows = mailTable.rows({ page: 'current' }).data().toArray().filter(row => row.canQueueEmail);
        const selectedCount = eligibleRows.filter(row => mailState.isSelected(row.id)).length;
        const checkbox = document.getElementById('mailPageSelect');
        if (!checkbox) return;
        checkbox.checked = eligibleRows.length > 0 && selectedCount === eligibleRows.length;
        checkbox.indeterminate = selectedCount > 0 && selectedCount < eligibleRows.length;
    }

    $(document).on('change', '.js-candidate-checkbox', function () {
        candidateState.setSelected(this.dataset.key, this.checked);
        updateCandidateSummary();
        updateCandidatePageSelect();
    });

    $(document).on('change', '.js-mail-checkbox', function () {
        mailState.setSelected(this.dataset.id, this.checked);
        updateMailSummary();
        updateMailPageSelect();
    });

    $('#candidatePageSelect').on('change', function () {
        if (!candidateTable) return;
        candidateTable.rows({ page: 'current' }).data().toArray()
            .filter(row => row.isEligible)
            .forEach(row => candidateState.setSelected(row.generationKey, this.checked));
        refreshCandidateCheckboxes();
    });

    $('#mailPageSelect').on('change', function () {
        if (!mailTable) return;
        mailTable.rows({ page: 'current' }).data().toArray()
            .filter(row => row.canQueueEmail)
            .forEach(row => mailState.setSelected(row.id, this.checked));
        refreshMailCheckboxes();
    });

    $('#candidateSelectAllFiltered').on('click', function () {
        candidateState.selectAllFiltered(candidateTable ? candidateTable.search() : '');
        refreshCandidateCheckboxes();
    });

    $('#candidateClearSelection').on('click', function () {
        candidateState.clear();
        refreshCandidateCheckboxes();
    });

    $('#mailSelectAllFiltered').on('click', function () {
        mailState.selectAllFiltered();
        refreshMailCheckboxes();
    });

    $('#mailClearSelection').on('click', function () {
        mailState.clear();
        refreshMailCheckboxes();
    });

    $('#candidateSubmissionStatusFilter,#candidatePaymentStatusFilter').on('change', function () {
        candidateState.clear();
        if (candidateTable) candidateTable.ajax.reload();
    });

    $('#mailCultureFilter,#mailStatusFilter').on('change', function () {
        mailState.clear();
        if (mailTable) mailTable.ajax.reload();
    });

    $('#certificateCandidateModal').on('shown.bs.modal', function () {
        initializeCandidateTable();
        if (candidateTable) {
            candidateTable.columns.adjust();
            candidateTable.ajax.reload(null, false);
        }
    });

    $('#certificateMailModal').on('shown.bs.modal', function () {
        initializeMailTable();
        if (mailTable) {
            mailTable.columns.adjust();
            mailTable.ajax.reload(null, false);
        }
    });

    $('.js-generate-certificate').on('click', async function () {
        const targetCulture = this.dataset.culture;
        if (!candidateState.allFiltered && candidateState.selected.size === 0) {
            await showMessage('warning', 'Seçim gerekli', 'Belge oluşturmak için en az bir bildiri seçin.');
            return;
        }

        const cultureName = targetCulture === 'en-US' ? 'İngilizce' : 'Türkçe';
        const selectionText = candidateState.allFiltered
            ? `Seçim kapsamındaki tüm uygun bildirilerin yazarları için ${cultureName} belge oluşturulacak. ${candidateState.excluded.size} bildiri seçim dışında.`
            : `${candidateState.selected.size} bildirinin tüm yazarları için ${cultureName} belge oluşturulacak.`;

        if (!await confirmAction('Belge üretimi başlatılsın mı?', selectionText, 'Evet, Oluştur')) return;

        try {
            const result = await postJson(page.dataset.generateUrl, {
                congressId: congressId,
                certificateCulture: targetCulture,
                submissionStatusCode: $('#candidateSubmissionStatusFilter').val() || null,
                paymentStatusCode: $('#candidatePaymentStatusFilter').val() || null,
                candidateSearch: candidateState.allFiltered ? (candidateState.allFilteredSearch || null) : null,
                selectAllFiltered: candidateState.allFiltered,
                selectedCandidateKeys: Array.from(candidateState.selected),
                excludedCandidateKeys: Array.from(candidateState.excluded)
            });
            bootstrap.Modal.getInstance(document.getElementById('certificateCandidateModal'))?.hide();
            await showMessage('success', 'Üretim kuyruğa alındı', result.message);
            window.location.reload();
        } catch (error) {
            await showMessage('error', 'İşlem başarısız', error.message);
        }
    });

    $('#queueCertificateEmailsButton').on('click', async function () {
        if (!mailState.allFiltered && mailState.selected.size === 0) {
            await showMessage('warning', 'Seçim gerekli', 'Mail göndermek için en az bir belge seçin.');
            return;
        }

        const selectionText = mailState.allFiltered
            ? `Filtrelenen tüm uygun belgelere public link içeren mail gönderilecek. ${mailState.excluded.size} belge seçim dışında.`
            : `${mailState.selected.size} belge için public link içeren mail gönderilecek.`;
        if (!await confirmAction('Mailler kuyruğa alınsın mı?', selectionText, 'Evet, Gönder')) return;

        try {
            const result = await postJson(page.dataset.emailUrl, {
                congressId: congressId,
                certificateCulture: $('#mailCultureFilter').val() || null,
                emailStatus: $('#mailStatusFilter').val() || 'NotSent',
                searchText: mailTable ? mailTable.search() : null,
                selectAllFiltered: mailState.allFiltered,
                certificateIds: Array.from(mailState.selected),
                excludedCertificateIds: Array.from(mailState.excluded)
            });
            bootstrap.Modal.getInstance(document.getElementById('certificateMailModal'))?.hide();
            await showMessage('success', 'Mail kuyruğa alındı', result.message);
            window.location.reload();
        } catch (error) {
            await showMessage('error', 'İşlem başarısız', error.message);
        }
    });

    $(document).on('click', '.js-revoke-certificate', async function () {
        const certificateId = this.dataset.id;
        let reason = 'Yönetim panelinden kaldırıldı.';

        if (window.Swal && typeof window.Swal.fire === 'function') {
            const result = await window.Swal.fire({
                icon: 'warning',
                title: 'Belge kaldırılsın mı?',
                text: 'Public link anında iptal olur, belge Dokümanlar bölümünden kaldırılır ve MinIO dosyası silinir.',
                input: 'textarea',
                inputLabel: 'Kaldırma nedeni',
                inputValue: reason,
                inputAttributes: { maxlength: 1000 },
                showCancelButton: true,
                confirmButtonText: 'Evet, Kaldır',
                cancelButtonText: 'Vazgeç',
                confirmButtonColor: '#dc2626'
            });
            if (!result.isConfirmed) return;
            reason = result.value || reason;
        } else {
            if (!window.confirm('Belge kaldırılacak ve public link iptal edilecek. Devam edilsin mi?')) return;
            reason = window.prompt('Kaldırma nedeni:', reason) || reason;
        }

        try {
            const response = await postJson(page.dataset.revokeUrl, { certificateId: certificateId, reason: reason });
            await showMessage('success', 'Belge kaldırıldı', response.message);
            if (documentsTable) documentsTable.ajax.reload(null, false);
            if (mailTable) mailTable.ajax.reload(null, false);
            window.setTimeout(() => window.location.reload(), 700);
        } catch (error) {
            await showMessage('error', 'Belge kaldırılamadı', error.message);
        }
    });

    function initializeGenerationPolling() {
        const card = document.getElementById('participationCertificateGenerationJobCard');
        if (!card || card.dataset.isActive !== 'true' || !card.dataset.statusUrl) return;
        let stopped = false;

        async function poll() {
            if (stopped) return;
            try {
                const response = await fetch(card.dataset.statusUrl, { cache: 'no-store', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (!response.ok) throw new Error('Status okunamadı.');
                const job = await response.json();
                $('#generationJobStatus').text(job.status);
                $('#generationJobProgressBar').val(job.progressPercent);
                $('#generationJobProgressText').text(`${job.processedCount} / ${job.totalCount}`);
                $('#generationJobSucceeded').text(job.succeededCount);
                $('#generationJobFailed').text(job.failedCount);
                if (job.isActive) {
                    window.setTimeout(poll, 3000);
                } else {
                    stopped = true;
                    if (documentsTable) documentsTable.ajax.reload();
                    window.setTimeout(() => window.location.reload(), 900);
                }
            } catch (_) {
                if (!stopped) window.setTimeout(poll, 5000);
            }
        }
        window.setTimeout(poll, 1500);
    }

    initializeDocumentsTable();
    initializeGenerationPolling();
})(jQuery);
