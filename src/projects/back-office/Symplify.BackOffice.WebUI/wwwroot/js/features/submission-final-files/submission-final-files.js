window.Symplify = window.Symplify || {};
window.Symplify.SubmissionFinalFiles = (function ($) {
    'use strict';

    let table = null;
    let $table = null;
    let selectedFullTextBookCongressId = '';
    let fullTextBookCoverObjectUrl = null;
    const selectedFileIds = new Set();

    function init() {
        $table = $('.js-final-files-table').first();
        if (!$table.length || !$.fn.DataTable) {
            return;
        }

        table = $table.DataTable({
            processing: true,
            serverSide: true,
            searching: true,
            ordering: true,
            paging: true,
            pageLength: 10,
            autoWidth: false,
            responsive: false,
            language: getDataTableLanguage(),
            ajax: {
                url: $table.data('source-url'),
                type: 'POST',
                headers: getAjaxHeaders(),
                data: function (request) {
                    request.CongressId = $('#finalFilesCongressFilter').val() || '';
                    request.ArchiveMode = ($table.data('archive-mode') || false).toString();
                },
                error: showError
            },
            order: [[resolveUploadedAtColumnIndex(), 'desc']],
            columns: buildColumns($table.data('is-video-page') === true || $table.data('is-video-page') === 'true'),
            drawCallback: function () {
                restoreVisibleCheckboxSelection();
            }
        });

        bindEvents();
    }

    function buildColumns(isVideoPage) {
        const columns = [
            {
                data: null,
                name: 'select',
                orderable: false,
                searchable: false,
                className: 'text-center',
                render: function (data, type, row) {
                    const fileId = normalizeFileId(row.id);
                    const checked = fileId && selectedFileIds.has(fileId)
                        ? ' checked'
                        : '';

                    return '<input class="form-check-input js-final-file-check"' +
                        ' type="checkbox"' +
                        ' value="' + escapeHtml(fileId) + '"' +
                        ' data-file-id="' + escapeHtml(fileId) + '"' +
                        ' aria-label="' + escapeHtml(row.submissionNumber || '') + '"' +
                        checked +
                        ' />';
                }
            },
            {
                data: 'submissionNumber',
                name: 'submissionNumber',
                className: 'text-nowrap',
                render: function (value) {
                    return '<span class="fw-semibold">' + escapeHtml(value || '-') + '</span>';
                }
            },
            {
                data: null,
                name: 'title',
                render: renderSubmissionTitle
            },
            {
                data: null,
                name: 'author',
                render: renderAuthor
            },
            {
                data: null,
                name: 'fileName',
                render: renderFile
            },
            {
                data: null,
                name: 'reviewStatus',
                className: 'text-nowrap',
                render: renderReviewStatus
            }
        ];


        columns.push(
            {
                data: null,
                name: 'uploadedAt',
                className: 'text-nowrap',
                render: renderUploadedAt
            },
            {
                data: null,
                name: 'actions',
                orderable: false,
                searchable: false,
                className: 'text-nowrap',
                render: renderActions
            }
        );

        return columns;
    }

    function bindEvents() {
        $(document).on('change', '.js-final-files-select-all', function () {
            const checked = $(this).is(':checked');

            getVisibleRowCheckboxes().each(function () {
                const $checkbox = $(this);
                const fileId = resolveCheckboxFileId($checkbox);

                $checkbox.prop('checked', checked);

                if (!fileId) {
                    return;
                }

                if (checked) {
                    selectedFileIds.add(fileId);
                } else {
                    selectedFileIds.delete(fileId);
                }
            });

            updateSelectAllState();
        });

        $(document).on('change click', '.js-final-file-check', function () {
            const checkbox = this;

            window.setTimeout(function () {
                const $checkbox = $(checkbox);
                const fileId = resolveCheckboxFileId($checkbox);

                if (!fileId) {
                    return;
                }

                if ($checkbox.is(':checked')) {
                    selectedFileIds.add(fileId);
                } else {
                    selectedFileIds.delete(fileId);
                }

                updateSelectAllState();
            }, 0);
        });

        $(document).on('click', '.js-final-files-apply-filters', function () {
            reloadTable();
        });

        $(document).on('click', '.js-final-files-clear-filters', function () {
            $('#finalFilesCongressFilter').val('');
            reloadTable();
        });

        $(document).on('click', '.js-generate-full-text-book', function () {
            const congressId = ($('#finalFilesCongressFilter').val() || '').toString().trim();
            if (!congressId) {
                showMessage('warning', $table.data('congress-required-message'));
                return;
            }

            selectedFullTextBookCongressId = congressId;
            openFullTextBookCoverModal();
        });

        $(document).on('change', '.js-full-text-book-cover-input', function () {
            updateFullTextBookCoverPreview(this);
        });

        $(document).on('click', '.js-clear-full-text-book-cover', function () {
            clearFullTextBookCoverSelection();
        });

        $(document).on('click', '.js-confirm-generate-full-text-book', function () {
            const congressId = selectedFullTextBookCongressId ||
                ($('#finalFilesCongressFilter').val() || '').toString().trim();

            if (!congressId) {
                showMessage('warning', $table.data('congress-required-message'));
                return;
            }

            const input = document.querySelector('.js-full-text-book-cover-input');
            const coverFile = input && input.files && input.files.length
                ? input.files[0]
                : null;

            if (coverFile && !validateFullTextBookCover(coverFile)) {
                return;
            }

            generateFullTextBook(congressId, coverFile, $(this));
        });

        $(document).on('hidden.bs.modal', '#fullTextBookCoverModal', function () {
            selectedFullTextBookCongressId = '';
            clearFullTextBookCoverSelection();
        });

        $(document).on('click', '.js-final-files-bulk-review', function () {
            const status = $(this).data('review-status');
            const ids = getSelectedFileIds();
            if (!ids.length) {
                showMessage('warning', $table.data('select-required-message'));
                return;
            }

            confirmAction(resolveConfirmText(status), function () {
                postForm($table.data('bulk-review-url'), {
                    FileIds: ids,
                    ReviewStatus: status
                });
            });
        });

        $(document).on('click', '.js-final-file-review', function () {
            const $button = $(this);
            const status = $button.data('review-status');
            const id = $button.data('file-id');

            confirmAction(resolveConfirmText(status), function () {
                postForm($table.data('review-url'), {
                    FileId: id,
                    ReviewStatus: status
                });
            });
        });

        $(document).on('click', '.js-final-file-delete', function () {
            const id = $(this).data('file-id');
            const deleteUrl = $table.data('delete-url');

            if (!id || !deleteUrl) {
                showMessage('error', $table.data('generic-error-message'));
                return;
            }

            confirmDeleteAction(function () {
                postForm(deleteUrl, {
                    FileId: id
                });
            });
        });

        $(document).on('click', '.js-final-files-bulk-delete', function () {
            const ids = getSelectedFileIds();
            const bulkDeleteUrl = $table.data('bulk-delete-url');

            if (!ids.length) {
                showMessage('warning', $table.data('select-required-message'));
                return;
            }

            if (!bulkDeleteUrl || isVideoPage()) {
                showMessage('error', $table.data('generic-error-message'));
                return;
            }

            confirmBulkDeleteAction(ids.length, function () {
                postBulkDelete(bulkDeleteUrl, ids);
            });
        });

        $(document).on('click', '.js-final-files-bulk-download', function () {
            const ids = getSelectedFileIds();
            if (!ids.length) {
                showMessage('warning', $table.data('select-required-message'));
                return;
            }

            const $form = $('<form/>', {
                method: 'post',
                action: $table.data('bulk-download-url')
            });

            $form.append($('<input/>', {
                type: 'hidden',
                name: '__RequestVerificationToken',
                value: getAntiForgeryToken()
            }));

            ids.forEach(function (id) {
                $form.append($('<input/>', {
                    type: 'hidden',
                    name: 'FileIds',
                    value: id
                }));
            });

            $('body').append($form);
            $form.trigger('submit');
            window.setTimeout(function () { $form.remove(); }, 1000);
        });

        $(document).on('click', '.js-final-files-public-links', function () {
            if (!isVideoPage()) {
                const links = getSelectedRows().map(function (row) { return row.publicUrl; }).filter(Boolean);
                if (!links.length) {
                    showMessage('warning', $table.data('select-required-message'));
                }

                $('#publicLinksText').val(links.join('\n'));
                return;
            }

            const ids = getSelectedFileIds();
            if (!ids.length) {
                showMessage('warning', $table.data('select-required-message'));
                $('#publicLinksText').val('');
                return;
            }

            loadPublicLinks(ids);
        });

        $(document).on('click', '.js-final-file-public-link', function () {
            const id = $(this).data('file-id');
            if (!isVideoPage() || !id) {
                $('#publicLinksText').val($(this).data('public-url') || '');
                return;
            }

            loadPublicLinks([id]);
        });

        $(document).on('click', '#copyPublicLinks', function () {
            const text = $('#publicLinksText').val() || '';
            if (!text) {
                return;
            }

            navigator.clipboard?.writeText(text);
        });
    }

    function renderSubmissionTitle(data, type, row) {
        return '' +
            '<span class="fw-semibold d-block text-primary-light lh-sm">' + escapeHtml(row.title || '-') + '</span>' +
            '<span class="text-secondary-light text-sm">' + escapeHtml(row.submissionTypeName || '-') + '</span>';
    }

    function renderAuthor(data, type, row) {
        return '' +
            '<span class="fw-semibold d-block text-primary-light text-nowrap">' + escapeHtml(row.correspondingAuthorName || '-') + '</span>' +
            (row.otherAuthorsText ? '<span class="text-secondary-light text-sm">' + escapeHtml(row.otherAuthorsText) + '</span>' : '');
    }

    function renderFile(data, type, row) {
        return '' +
            '<span class="badge bg-info-focus text-info-main rounded-pill mb-1">' + escapeHtml(row.fileExtension || '-') + '</span>' +
            '<span class="text-secondary-light text-sm d-block">' + escapeHtml(row.originalFileName || '-') + '</span>' +
            '<span class="text-neutral-500 text-xs d-block">' + escapeHtml(row.fileSizeText || '-') + '</span>';
    }

    function renderReviewStatus(data, type, row) {
        return '<span class="badge ' + escapeHtml(row.reviewStatusBadgeClass || 'bg-warning-focus text-warning-main') + ' rounded-pill">' + escapeHtml(row.reviewStatusText || '-') + '</span>';
    }


    function renderUploadedAt(data, type, row) {
        return '' +
            '<span class="fw-medium d-block"><i class="ri-calendar-line text-primary-600 me-1"></i>' + escapeHtml(row.uploadedDate || '-') + '</span>' +
            '<small class="text-neutral-500"><i class="ri-time-line me-1"></i>' + escapeHtml(row.uploadedTime || '-') + '</small>';
    }

    function renderActions(data, type, row) {
        const previewText = $table.data('preview-text') || 'Önizle';
        const downloadText = $table.data('download-text') || 'İndir';
        const approveText = $table.data('approve-text') || 'Onayla';
        const revertText = $table.data('revert-text') || 'Onayı Geri Al';
        const publicLinkText = (row.fileKind === 'Presentation' ? ($table.data('short-link-text') || 'Kısa Link') : ($table.data('public-link-text') || 'Public Link'));
        const actionsText = $table.data('actions-text') || 'İşlemler';

        const previewUrl = row.watchUrl || row.previewUrl || '#';

        const deleteText = $table.data('delete-text') || 'Sil';
        const deleteAction = !isVideoPage() && row.fileKind === 'FullText'
            ? '<li><hr class="dropdown-divider"></li>' +
              '<li><button class="dropdown-item text-danger js-final-file-delete" type="button" data-file-id="' + escapeHtml(row.id || '') + '"><i class="ri-delete-bin-6-line me-2"></i>' + escapeHtml(deleteText) + '</button></li>'
            : '';

        return '' +
            '<div class="dropdown">' +
                '<button class="btn btn-sm btn-outline-primary-600 radius-8 dropdown-toggle" type="button" data-bs-toggle="dropdown" aria-expanded="false">' + escapeHtml(actionsText) + '</button>' +
                '<ul class="dropdown-menu">' +
                    '<li><a class="dropdown-item" href="' + escapeHtml(previewUrl) + '" target="_blank" rel="noopener"><i class="ri-eye-line me-2"></i>' + escapeHtml(previewText) + '</a></li>' +
                    '<li><a class="dropdown-item" href="' + escapeHtml(row.downloadUrl || '#') + '"><i class="ri-download-2-line me-2"></i>' + escapeHtml(downloadText) + '</a></li>' +
                    '<li><button class="dropdown-item js-final-file-public-link" type="button" data-file-id="' + escapeHtml(row.id || '') + '" data-public-url="' + escapeHtml(row.publicUrl || '') + '" data-bs-toggle="modal" data-bs-target="#publicLinksModal"><i class="ri-links-line me-2"></i>' + escapeHtml(publicLinkText) + '</button></li>' +
                    '<li><hr class="dropdown-divider"></li>' +
                    '<li><button class="dropdown-item js-final-file-review" type="button" data-file-id="' + escapeHtml(row.id || '') + '" data-review-status="Approved"><i class="ri-check-line me-2"></i>' + escapeHtml(approveText) + '</button></li>' +
                    '<li><button class="dropdown-item text-danger js-final-file-review" type="button" data-file-id="' + escapeHtml(row.id || '') + '" data-review-status="PendingReview"><i class="ri-arrow-go-back-line me-2"></i>' + escapeHtml(revertText) + '</button></li>' +
                    deleteAction +
                '</ul>' +
            '</div>';
    }

    function openFullTextBookCoverModal() {
        const modalElement = document.getElementById('fullTextBookCoverModal');
        if (!modalElement || !window.bootstrap || !window.bootstrap.Modal) {
            showMessage('error', $table.data('generic-error-message'));
            return;
        }

        clearFullTextBookCoverSelection();
        window.bootstrap.Modal.getOrCreateInstance(modalElement).show();
    }

    function validateFullTextBookCover(file) {
        if (!file) {
            return true;
        }

        const maxBytes = Number($table.data('cover-max-bytes')) || (8 * 1024 * 1024);
        if (file.size <= 0 || file.size > maxBytes) {
            showMessage('warning', $table.data('cover-too-large-message'));
            return false;
        }

        const name = (file.name || '').toLowerCase();
        const type = (file.type || '').toLowerCase();
        const validExtension = name.endsWith('.png') || name.endsWith('.jpg') || name.endsWith('.jpeg');
        const validType = !type || type === 'image/png' || type === 'image/jpeg';

        if (!validExtension || !validType) {
            showMessage('warning', $table.data('cover-invalid-message'));
            return false;
        }

        return true;
    }

    function updateFullTextBookCoverPreview(input) {
        const file = input && input.files && input.files.length ? input.files[0] : null;
        if (!file) {
            clearFullTextBookCoverPreview();
            return;
        }

        if (!validateFullTextBookCover(file)) {
            input.value = '';
            clearFullTextBookCoverPreview();
            return;
        }

        clearFullTextBookCoverPreview();
        fullTextBookCoverObjectUrl = window.URL.createObjectURL(file);

        const preview = document.querySelector('.js-full-text-book-cover-preview');
        const image = document.querySelector('.js-full-text-book-cover-preview-image');
        const fileName = document.querySelector('.js-full-text-book-cover-file-name');
        const fileSize = document.querySelector('.js-full-text-book-cover-file-size');

        if (preview) preview.classList.remove('d-none');
        if (image) image.src = fullTextBookCoverObjectUrl;
        if (fileName) fileName.textContent = file.name || '';
        if (fileSize) fileSize.textContent = formatBytes(file.size);
    }

    function clearFullTextBookCoverSelection() {
        const input = document.querySelector('.js-full-text-book-cover-input');
        if (input) input.value = '';
        clearFullTextBookCoverPreview();
    }

    function clearFullTextBookCoverPreview() {
        if (fullTextBookCoverObjectUrl) {
            window.URL.revokeObjectURL(fullTextBookCoverObjectUrl);
            fullTextBookCoverObjectUrl = null;
        }

        const preview = document.querySelector('.js-full-text-book-cover-preview');
        const image = document.querySelector('.js-full-text-book-cover-preview-image');
        const fileName = document.querySelector('.js-full-text-book-cover-file-name');
        const fileSize = document.querySelector('.js-full-text-book-cover-file-size');

        if (preview) preview.classList.add('d-none');
        if (image) image.removeAttribute('src');
        if (fileName) fileName.textContent = '';
        if (fileSize) fileSize.textContent = '';
    }

    function formatBytes(bytes) {
        const value = Number(bytes) || 0;
        if (value < 1024) return value + ' B';
        if (value < 1024 * 1024) return (value / 1024).toFixed(1) + ' KB';
        return (value / (1024 * 1024)).toFixed(1) + ' MB';
    }

    async function generateFullTextBook(congressId, coverFile, $button) {
        const url = $table.data('generate-book-url');
        if (!url) {
            showMessage('error', $table.data('generic-error-message'));
            return;
        }

        const originalHtml = $button.html();
        $button.prop('disabled', true);
        $button.html('<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>' +
            escapeHtml($table.data('generating-book-message') || 'Tam metin kitabı hazırlanıyor...'));

        try {
            const formData = new FormData();
            formData.append('congressId', congressId);
            if (coverFile) {
                formData.append('coverImage', coverFile, coverFile.name);
            }
            formData.append('__RequestVerificationToken', getAntiForgeryToken());

            const response = await fetch(url, {
                method: 'POST',
                headers: getAjaxHeaders(),
                body: formData,
                credentials: 'same-origin'
            });

            if (!response.ok) {
                const errorBlob = await response.blob();
                const errorText = await errorBlob.text();
                let message = $table.data('generic-error-message');

                if (errorText) {
                    try {
                        const payload = JSON.parse(errorText);
                        message = payload.message || payload.detail || message;
                    } catch (ignore) {
                        message = errorText;
                    }
                }

                throw new Error(message);
            }

            const blob = await response.blob();
            const fileName = resolveDownloadFileName(
                response.headers.get('Content-Disposition'),
                'tam-metin-kitabi.docx');
            const objectUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = objectUrl;
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.setTimeout(function () {
                window.URL.revokeObjectURL(objectUrl);
            }, 1000);

            const modalElement = document.getElementById('fullTextBookCoverModal');
            if (modalElement && window.bootstrap && window.bootstrap.Modal) {
                window.bootstrap.Modal.getOrCreateInstance(modalElement).hide();
            }
        } catch (error) {
            showMessage(
                'error',
                error && error.message
                    ? error.message
                    : $table.data('generic-error-message'));
        } finally {
            $button.prop('disabled', false);
            $button.html(originalHtml);
        }
    }

    function resolveDownloadFileName(contentDisposition, fallback) {
        if (!contentDisposition) {
            return fallback;
        }

        const encodedMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
        if (encodedMatch && encodedMatch[1]) {
            try {
                return decodeURIComponent(encodedMatch[1].replace(/["']/g, '').trim());
            } catch (ignore) {
                return encodedMatch[1].replace(/["']/g, '').trim();
            }
        }

        const plainMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
        return plainMatch && plainMatch[1]
            ? plainMatch[1].trim()
            : fallback;
    }

    function loadPublicLinks(ids) {
        const url = $table.data('public-links-url');
        if (!url) {
            const links = getSelectedRows().map(function (row) { return row.publicUrl; }).filter(Boolean);
            $('#publicLinksText').val(links.join('\n'));
            return;
        }

        const payload = {
            __RequestVerificationToken: getAntiForgeryToken(),
            FileIds: ids
        };

        $.ajax({
            url: url,
            type: 'POST',
            data: payload,
            traditional: true,
            success: function (response) {
                if (response && response.success === false) {
                    $('#publicLinksText').val('');
                    showMessage('warning', response.message || $table.data('short-link-unavailable-message'));
                    return;
                }

                const links = response && Array.isArray(response.links) ? response.links : [];
                $('#publicLinksText').val(links.join('\n'));
            },
            error: function (xhr) {
                $('#publicLinksText').val('');
                showError(xhr);
            }
        });
    }

    function postForm(url, data) {
        const payload = $.extend({}, data, {
            __RequestVerificationToken: getAntiForgeryToken()
        });

        $.ajax({
            url: url,
            type: 'POST',
            data: payload,
            traditional: true,
            success: function (response) {
                if (response && response.success === false) {
                    showMessage('error', response.message || $table.data('generic-error-message'));
                    return;
                }

                showMessage('success', response && response.message ? response.message : 'OK');
                reloadTable();
            },
            error: showError
        });
    }

    function postBulkDelete(url, ids) {
        const payload = ids.map(function (id) {
            return {
                name: 'FileIds',
                value: id
            };
        });

        payload.push({
            name: '__RequestVerificationToken',
            value: getAntiForgeryToken()
        });

        $.ajax({
            url: url,
            type: 'POST',
            data: $.param(payload),
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            processData: false,
            success: function (response) {
                const deletedCount = Number(response && response.deletedCount) || 0;
                const failedCount = Number(response && response.failedCount) || 0;
                const icon = failedCount > 0
                    ? (deletedCount > 0 ? 'warning' : 'error')
                    : 'success';

                showMessage(
                    icon,
                    response && response.message
                        ? response.message
                        : $table.data('generic-error-message'));

                if (deletedCount > 0) {
                    reloadTable();
                }
            },
            error: showError
        });
    }

    function getSelectedFileIds() {
        getAllRenderedRowCheckboxes()
            .filter(':checked')
            .each(function () {
                const fileId = resolveCheckboxFileId($(this));

                if (fileId) {
                    selectedFileIds.add(fileId);
                }
            });

        return Array.from(selectedFileIds)
            .map(normalizeFileId)
            .filter(Boolean);
    }

    function resolveCheckboxFileId($checkbox) {
        return normalizeFileId(
            $checkbox.attr('data-file-id') ||
            $checkbox.data('file-id') ||
            $checkbox.val());
    }

    function normalizeFileId(value) {
        return value === null || value === undefined
            ? ''
            : value.toString().trim().toLowerCase();
    }

    function getAllRenderedRowCheckboxes() {
        return $('.js-final-file-check, ' +
            '.dataTables_scrollBody input[type="checkbox"][data-file-id], ' +
            '.dt-scroll-body input[type="checkbox"][data-file-id]');
    }

    function getVisibleRowCheckboxes() {
        if (!$table || !$table.length) {
            return getAllRenderedRowCheckboxes();
        }

        const $wrapper = $table.closest('.dataTables_wrapper, .dt-container');
        const $checkboxes = $wrapper.find(
            'tbody .js-final-file-check, ' +
            '.dataTables_scrollBody tbody input[type="checkbox"][data-file-id], ' +
            '.dt-scroll-body tbody input[type="checkbox"][data-file-id]');

        return $checkboxes.length
            ? $checkboxes
            : getAllRenderedRowCheckboxes();
    }

    function restoreVisibleCheckboxSelection() {
        getVisibleRowCheckboxes().each(function () {
            const $checkbox = $(this);
            const fileId = resolveCheckboxFileId($checkbox);

            $checkbox.prop(
                'checked',
                Boolean(fileId && selectedFileIds.has(fileId)));
        });

        updateSelectAllState();
    }

    function updateSelectAllState() {
        const $checkboxes = getVisibleRowCheckboxes();
        const selectableCount = $checkboxes.length;
        const checkedCount = $checkboxes.filter(':checked').length;

        $('.js-final-files-select-all')
            .prop('checked', selectableCount > 0 && checkedCount === selectableCount)
            .prop('indeterminate', checkedCount > 0 && checkedCount < selectableCount);
    }

    function clearSelection() {
        selectedFileIds.clear();
        getAllRenderedRowCheckboxes().prop('checked', false);
        $('.js-final-files-select-all')
            .prop('checked', false)
            .prop('indeterminate', false);
    }

    function getSelectedRows() {
        if (!table) {
            return [];
        }

        const ids = new Set(getSelectedFileIds());
        return table.rows({ page: 'current' }).data().toArray().filter(function (row) {
            return ids.has(normalizeFileId(row.id));
        });
    }

    function reloadTable() {
        if (table) {
            clearSelection();
            table.ajax.reload(null, false);
        }
    }

    function isVideoPage() {
        return $table && ($table.data('is-video-page') === true || $table.data('is-video-page') === 'true');
    }

    function resolveUploadedAtColumnIndex() {
        return 6;
    }

    function resolveConfirmText(status) {
        return status === 'Approved'
            ? ($table.data('confirm-approve') || 'Onaylansın mı?')
            : ($table.data('confirm-revert') || 'Onay geri alınsın mı?');
    }

    function confirmAction(text, callback) {
        if (!window.Swal) {
            callback();
            return;
        }

        window.Swal.fire({
            title: $table.data('confirm-title') || '',
            text: text,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: $table.data('confirm-button') || 'Onayla',
            cancelButtonText: $table.data('cancel-button') || 'Vazgeç'
        }).then(function (result) {
            if (result.isConfirmed) {
                callback();
            }
        });
    }

    function confirmDeleteAction(callback) {
        const message = $table.data('confirm-delete') || 'Tam metin dosyası kalıcı olarak silinecek. Devam edilsin mi?';

        if (!window.Swal) {
            if (window.confirm(message)) {
                callback();
            }
            return;
        }

        window.Swal.fire({
            title: $table.data('confirm-title') || 'İşlem onayı',
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: $table.data('confirm-delete-button') || 'Evet, Sil',
            cancelButtonText: $table.data('cancel-button') || 'Vazgeç',
            confirmButtonColor: '#dc3545'
        }).then(function (result) {
            if (result.isConfirmed) {
                callback();
            }
        });
    }

    function confirmBulkDeleteAction(fileCount, callback) {
        const template = $table.data('confirm-bulk-delete') ||
            'Seçilen {0} tam metin dosyası kalıcı olarak silinecek. Devam edilsin mi?';
        const message = template.toString().replace('{0}', fileCount.toString());

        if (!window.Swal) {
            if (window.confirm(message)) {
                callback();
            }
            return;
        }

        window.Swal.fire({
            title: $table.data('confirm-title') || 'İşlem onayı',
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: $table.data('confirm-delete-button') || 'Evet, Sil',
            cancelButtonText: $table.data('cancel-button') || 'Vazgeç',
            confirmButtonColor: '#dc3545'
        }).then(function (result) {
            if (result.isConfirmed) {
                callback();
            }
        });
    }

    function showMessage(icon, message) {
        if (window.Swal) {
            window.Swal.fire({ icon: icon, text: message });
            return;
        }

        if (icon === 'error') {
            console.error(message);
        } else {
            console.log(message);
        }
    }

    function showError(xhr) {
        const message = (xhr && xhr.responseJSON && xhr.responseJSON.message)
            ? xhr.responseJSON.message
            : ($table ? $table.data('generic-error-message') : 'İşlem sırasında bir hata oluştu.');
        showMessage('error', message);
    }

    function getAjaxHeaders() {
        const token = getAntiForgeryToken();
        return token ? { RequestVerificationToken: token } : {};
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    }

    function getDataTableLanguage() {
        let language = {};
        if (window.Symplify.DataTables && typeof window.Symplify.DataTables.getLanguage === 'function') {
            language = window.Symplify.DataTables.getLanguage() || {};
        } else {
            language = window.Symplify.DataTables?.language || window.Symplify.dataTables?.language || {};
        }

        return $.extend(true, {}, language, {
            search: $table.data('dt-search') || language.search || 'Ara:',
            lengthMenu: $table.data('dt-length-menu') || language.lengthMenu || '_MENU_ kayıt göster',
            info: $table.data('dt-info') || language.info || '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor',
            infoEmpty: $table.data('dt-info-empty') || language.infoEmpty || 'Kayıt bulunamadı',
            zeroRecords: $table.data('dt-zero-records') || language.zeroRecords || 'Eşleşen kayıt bulunamadı',
            paginate: $.extend({}, language.paginate || {}, {
                first: $table.data('dt-first') || language.paginate?.first || 'İlk',
                previous: $table.data('dt-previous') || language.paginate?.previous || 'Önceki',
                next: $table.data('dt-next') || language.paginate?.next || 'Sonraki',
                last: $table.data('dt-last') || language.paginate?.last || 'Son'
            })
        });
    }

    function escapeHtml(value) {
        return $('<div/>').text(value == null ? '' : value.toString()).html();
    }

    return { init: init };
})(jQuery);

$(function () {
    window.Symplify.SubmissionFinalFiles.init();
});
