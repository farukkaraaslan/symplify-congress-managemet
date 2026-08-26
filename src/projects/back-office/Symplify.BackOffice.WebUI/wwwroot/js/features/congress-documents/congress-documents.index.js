window.Symplify = window.Symplify || {};
window.Symplify.CongressDocuments = window.Symplify.CongressDocuments || {};

window.Symplify.CongressDocuments.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressDocumentPanel',
        table: '#congressDocumentsTable',
        modalContainer: '#congressDocumentModalContainer',
        createButton: '#openCreateDocumentModalButton',
        createForm: '#createCongressDocumentForm',
        updateForm: '#updateCongressDocumentForm',
        dropzone: '[data-symplify-dropzone]',
        dragHandle: '.js-document-drag-handle'
    };

    let table;

    function init() {
        if (!$(selectors.panel).length || !$(selectors.table).length) {
            return;
        }

        ensureReorderStyles();
        initializeTable();
        bindEvents();
    }

    function initializeTable() {
        const $panel = $(selectors.panel);
        const $table = $(selectors.table);

        if (!$.fn.DataTable) {
            console.error('DataTables plugin bulunamadı. Congress documents tablosu başlatılamadı.');
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
            initializeReorder();
            updateDragHandleState();
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
            order: [[2, 'asc']],
            ajax: {
                url: $panel.data('source-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: function (data) {
                    data.congressId = $panel.data('congress-id');
                    data.culture = getCurrentCulture();
                    return data;
                },
                error: showError
            },
            columns: [
                { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false, className: 'text-nowrap' },
                { data: null, name: 'dragHandle', orderable: false, searchable: false, className: 'text-center text-nowrap', render: renderDragHandle },
                { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap', render: renderOrder },
                { data: null, name: 'originalFileName', orderable: true, searchable: true, render: renderFileName },
                { data: 'documentTypeName', name: 'documentTypeName', orderable: true, searchable: true, render: renderDocumentType },
                { data: 'fileSizeText', name: 'fileSize', orderable: true, searchable: false, className: 'text-nowrap', render: renderTextOrDash },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, className: 'text-nowrap', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            createdRow: function (row, data) {
                if (data && data.id) {
                    $(row).attr('data-id', data.id).attr('data-order', data.order || '').addClass('js-document-row');
                }
            },
            drawCallback: function () {
                initializeReorder();
                updateDragHandleState();
            },
            language: getDataTableLanguage()
        });

        $table
            .off('order.dt.congressDocuments search.dt.congressDocuments page.dt.congressDocuments draw.dt.congressDocuments')
            .on('order.dt.congressDocuments search.dt.congressDocuments page.dt.congressDocuments draw.dt.congressDocuments', function () {
                window.setTimeout(function () {
                    initializeReorder();
                    updateDragHandleState();
                }, 0);
            });
    }

    function bindEvents() {
        $(document).off('click.congressDocumentsCreate', selectors.createButton).on('click.congressDocumentsCreate', selectors.createButton, openCreateModal);
        $(document).off('click.congressDocumentsEdit', '.js-edit-congress-document').on('click.congressDocumentsEdit', '.js-edit-congress-document', openUpdateModal);
        $(document).off('click.congressDocumentsDelete', '.js-delete-congress-document').on('click.congressDocumentsDelete', '.js-delete-congress-document', deleteDocument);
        $(document).off('submit.congressDocumentsCreate', selectors.createForm).on('submit.congressDocumentsCreate', selectors.createForm, submitForm);
        $(document).off('submit.congressDocumentsUpdate', selectors.updateForm).on('submit.congressDocumentsUpdate', selectors.updateForm, submitForm);
    }

    function openCreateModal() {
        $.get($(selectors.panel).data('create-modal-url'))
            .done(function (html) { showModalHtml(html, '#createDocumentModal'); })
            .fail(showError);
    }

    function openUpdateModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('edit-modal-url'), { id: $button.data('id'), congressId: $panel.data('congress-id') })
            .done(function (html) { showModalHtml(html, '#updateDocumentModal'); })
            .fail(showError);
    }

    function submitForm(event) {
        event.preventDefault();

        const $form = $(this);
        prepareForm($form);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        setBusy($form, true);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) return;
                    showError(response);
                    return;
                }

                hideModal($form.closest('.modal'));
                reload(false);
                showSuccess(response.message || text('saved', 'Kayıt kaydedildi.'));
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) return;
                showError(xhr);
            })
            .always(function () { setBusy($form, false); });
    }

    function deleteDocument() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        confirmAction({
            title: text('deleteConfirmTitle', 'Emin misiniz?'),
            text: text('deleteConfirmText', 'Bu doküman silinecek.'),
            confirmButtonText: text('deleteConfirmButton', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) return;

            $.ajax({
                url: $panel.data('delete-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: { id: $button.data('id'), congressId: $panel.data('congress-id') }
            })
                .done(function (response) {
                    if (!response || response.success !== true) { showError(response); return; }
                    reload(false);
                    showSuccess(response.message || text('deleted', 'Kayıt silindi.'));
                })
                .fail(showError);
        });
    }

    function showModalHtml(html, modalSelector) {
        cleanupModalArtifacts();
        $(selectors.modalContainer).empty();

        const $html = $(html);
        const $modal = $html.filter(modalSelector).add($html.find(modalSelector)).first();

        if (!$modal.length) { showError(text('modalNotFound', 'Modal içeriği yüklenemedi.')); return; }

        $modal.appendTo(document.body);
        initializeModal($modal);

        const modalElement = $modal[0];

        $modal.one('hidden.bs.modal', function () {
            if (window.Symplify.Dropzone && typeof window.Symplify.Dropzone.destroy === 'function') {
                window.Symplify.Dropzone.destroy($modal);
            }

            const instance = bootstrap.Modal.getInstance(modalElement);
            if (instance) instance.dispose();

            $modal.remove();
            cleanupModalArtifacts();
        });

        bootstrap.Modal.getOrCreateInstance(modalElement, { backdrop: true, focus: true, keyboard: true }).show();
    }

    function initializeModal($modal) {
        $modal.find('form').each(function () {
            const $form = $(this);
            if (window.Symplify.Forms && typeof window.Symplify.Forms.initialize === 'function') {
                window.Symplify.Forms.initialize($form);
            } else if ($.validator && $.validator.unobtrusive) {
                $form.removeData('validator');
                $form.removeData('unobtrusiveValidation');
                $.validator.unobtrusive.parse($form);
            }
        });

        if (window.Symplify.Dropzone && typeof window.Symplify.Dropzone.initAll === 'function') {
            window.Symplify.Dropzone.initAll($modal, {
                selector: selectors.dropzone,
                maxSizeMb: 50,
                invalidFileText: text('invalidFile', 'Bu dosya türüne izin verilmiyor.'),
                fileTooLargeText: text('fileTooLarge', 'Dosya boyutu en fazla 50 MB olabilir.'),
                selectedText: text('selectedFile', 'Seçilen dosya')
            });
        }
    }

    function hideModal($modal) {
        if ($modal && $modal.length) bootstrap.Modal.getOrCreateInstance($modal[0]).hide();
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) return;
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
    }

    function renderDragHandle() {
        const label = text('dragHandle', 'Sırayı değiştirmek için sürükleyin');
        return '<span role="button" tabindex="0" class="d-inline-flex align-items-center justify-content-center text-neutral-500 js-document-drag-handle" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '"><i class="ri-draggable"></i></span>';
    }

    function renderOrder(data) {
        const value = data === null || data === undefined ? '-' : data;
        return '<span class="fw-medium text-secondary-light js-document-order-value">' + escapeHtml(value) + '</span>';
    }

    function renderFileName(row) {
        const fileName = row && row.originalFileName ? row.originalFileName : '-';
        const contentType = row && row.contentType ? row.contentType : '';
        const description = row && row.description ? row.description : (row && row.descriptionEn ? row.descriptionEn : '');
        const coverUrl = row && row.coverImageUrl ? row.coverImageUrl : '';
        const cover = coverUrl
            ? '<img src="' + escapeHtml(coverUrl) + '" alt="' + escapeHtml(text('coverImage', 'Kapak görseli')) + '" class="rounded border object-fit-cover flex-shrink-0" style="width:42px;height:56px;object-fit:cover;" loading="lazy" />'
            : '<span class="d-inline-flex align-items-center justify-content-center rounded bg-neutral-100 text-neutral-500 flex-shrink-0" style="width:42px;height:56px;"><i class="ri-file-text-line"></i></span>';

        return '' +
            '<div class="d-flex align-items-center gap-3">' +
                cover +
                '<div class="d-flex flex-column gap-1 min-w-0">' +
                    '<span class="fw-medium text-secondary-light text-break">' + escapeHtml(fileName) + '</span>' +
                    (contentType ? '<small class="text-neutral-500">' + escapeHtml(contentType) + '</small>' : '') +
                    (description ? '<small class="text-neutral-600">' + escapeHtml(description) + '</small>' : '') +
                    (row && row.hasCoverImage ? '<small class="text-primary-600">' + escapeHtml(text('hasCoverImage', 'Kapak görseli var')) + '</small>' : '') +
                '</div>' +
            '</div>';
    }

    function renderDocumentType(value) { return value ? escapeHtml(value) : '<span class="text-neutral-400">-</span>'; }
    function renderTextOrDash(value) { return value ? escapeHtml(value) : '<span class="text-neutral-400">-</span>'; }

    function renderStatus(isActive) {
        return isActive
            ? '<span class="badge bg-success-100 text-success-600 rounded-pill px-12 py-6">' + text('active', 'Aktif') + '</span>'
            : '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill px-12 py-6">' + text('passive', 'Pasif') + '</span>';
    }

    function renderActions(data, type, row) {
        const id = row && row.id ? row.id : '';
        const downloadUrl = row && row.downloadUrl ? row.downloadUrl : '#';
        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
                '<a href="' + escapeHtml(downloadUrl) + '" target="_blank" class="btn btn-info-100 text-info-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px" aria-label="' + escapeHtml(text('download', 'İndir')) + '"><i class="ri-download-line"></i></a>' +
                '<button type="button" class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-edit-congress-document" data-id="' + escapeHtml(id) + '" aria-label="' + escapeHtml(text('edit', 'Düzenle')) + '"><i class="ri-edit-line"></i></button>' +
                '<button type="button" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-delete-congress-document" data-id="' + escapeHtml(id) + '" aria-label="' + escapeHtml(text('delete', 'Sil')) + '"><i class="ri-delete-bin-line"></i></button>' +
            '</div>';
    }

    function initializeReorder() {
        if (!table) return;
        if ($.fn.sortable) { initializeSortableReorder(); return; }
    }

    function initializeSortableReorder() {
        const $tbody = $(table.table().body());
        if ($tbody.data('ui-sortable')) $tbody.sortable('destroy');

        $tbody.sortable({
            items: 'tr[data-id]',
            handle: selectors.dragHandle,
            axis: 'y',
            cursor: 'move',
            tolerance: 'pointer',
            forcePlaceholderSize: true,
            placeholder: 'lookup-sort-placeholder',
            helper: function (event, row) {
                const $originals = row.children();
                const $helper = row.clone();
                $helper.children().each(function (index) { $(this).width($originals.eq(index).width()); });
                return $helper;
            },
            start: function (event, ui) {
                if (!isReorderAllowed()) { $(this).sortable('cancel'); showReorderNotAllowedMessage(); return; }
                ui.item.addClass('lookup-row-dragging');
                ui.placeholder.html('<td colspan="8">&nbsp;</td>');
            },
            update: function () { updateVisibleRowNumbers(); persistReorder(); },
            stop: function (event, ui) { ui.item.removeClass('lookup-row-dragging'); updateVisibleRowNumbers(); }
        });

        $tbody.sortable(isReorderAllowed() ? 'enable' : 'disable');
    }

    function updateDragHandleState() {
        if (!table) return;
        const allowed = isReorderAllowed();
        const $tbody = $(table.table().body());
        const $handles = $tbody.find(selectors.dragHandle);
        if ($.fn.sortable && $tbody.data('ui-sortable')) $tbody.sortable(allowed ? 'enable' : 'disable');
        $handles.toggleClass('opacity-50', !allowed).css('cursor', allowed ? 'grab' : 'not-allowed');
    }

    function isReorderAllowed() {
        if (!table) return false;
        const order = table.order();
        const firstOrder = Array.isArray(order) && order.length > 0 ? order[0] : null;
        const isOrderAsc = firstOrder && Number(firstOrder[0]) === 2 && String(firstOrder[1] || '').toLowerCase() === 'asc';
        const hasSearch = String(table.search() || '').trim().length > 0;
        return isOrderAsc && !hasSearch;
    }

    function showReorderNotAllowedMessage() {
        showError({ responseJSON: { message: text('reorderNotAllowed', 'Sıralama yapmak için arama boş olmalı ve tablo Sıra No kolonuna göre artan sıralanmalıdır.') } });
    }

    function updateVisibleRowNumbers() {
        if (!table) return;
        const pageInfo = table.page.info();
        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const visibleNumber = pageInfo.start + index + 1;
            $(this).find('td').eq(0).text(visibleNumber);
            $(this).find('td').eq(2).find('.js-document-order-value').text(visibleNumber);
        });
    }

    function persistReorder() {
        if (!table || !isReorderAllowed()) { reload(false); return; }
        const $panel = $(selectors.panel);
        const reorderUrl = $panel.data('reorder-url');
        const pageInfo = table.page.info();
        const items = [];
        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const id = $(this).attr('data-id');
            if (id) items.push({ id: id, order: pageInfo.start + index + 1 });
        });
        if (!items.length) { reload(false); return; }

        $.ajax({
            url: reorderUrl,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ items: items }),
            headers: buildAjaxHeaders($panel)
        })
            .done(function (response) {
                if (!response || response.success !== true) { showError(response); reload(false); return; }
                showSuccess(response.message || text('reordered', 'Sıralama güncellendi.'));
                reload(false);
            })
            .fail(function (xhr) { showError(xhr); reload(false); });
    }

    function reload(resetPaging) { if (table) table.ajax.reload(null, resetPaging === true); }

    function prepareForm($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.prepareForSubmit === 'function') { window.Symplify.Forms.prepareForSubmit($form); return; }
        $form.find('.field-validation-error').removeClass('field-validation-error').addClass('field-validation-valid').empty();
        $form.find('.input-validation-error, .is-invalid').removeClass('input-validation-error is-invalid');
    }

    function postForm($form) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.postForm === 'function') return window.Symplify.Ajax.postForm($form, { multipart: true });
        return $.ajax({ url: $form.attr('action'), type: $form.attr('method') || 'POST', data: new FormData($form[0]), processData: false, contentType: false, headers: buildAjaxHeaders($form) });
    }

    function renderValidationErrors($form, response) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.renderValidationErrors === 'function') return window.Symplify.Forms.renderValidationErrors($form, response);
        const payload = response && response.responseJSON ? response.responseJSON : response;
        const errors = payload && payload.errors ? payload.errors : null;
        if (!errors) return false;
        Object.keys(errors).forEach(function (fieldName) {
            const messages = Array.isArray(errors[fieldName]) ? errors[fieldName] : [errors[fieldName]];
            const message = messages.filter(Boolean).join(' ');
            $form.find('[data-valmsg-for="' + escapeAttribute(fieldName) + '"]').removeClass('field-validation-valid').addClass('field-validation-error').text(message);
            $form.find('[name="' + escapeAttribute(fieldName) + '"]').addClass('input-validation-error is-invalid');
        });
        return true;
    }

    function focusFirstInvalidField($form) { const $field = $form.find('.input-validation-error, .is-invalid').first(); if ($field.length) $field.trigger('focus'); }
    function hasJQueryValidation() { return typeof $.validator !== 'undefined' && typeof $.validator.unobtrusive !== 'undefined'; }

    function setBusy($form, isBusy) {
        const $buttons = $form.find('button[type="submit"]');
        $buttons.prop('disabled', isBusy);
        if (isBusy) {
            $buttons.each(function () { const $button = $(this); if (!$button.attr('data-original-text')) $button.attr('data-original-text', $button.html()); $button.html('<span class="spinner-border spinner-border-sm me-2"></span>' + escapeHtml(text('saving', 'Kaydediliyor...'))); });
        } else {
            $buttons.each(function () { const $button = $(this); const originalText = $button.attr('data-original-text'); if (originalText) $button.html(originalText).removeAttr('data-original-text'); });
        }
    }

    function buildAjaxHeaders($source) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.buildAjaxHeaders === 'function') return window.Symplify.Ajax.buildAjaxHeaders($source);
        const headers = { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json', 'X-Culture': getCurrentCulture() };
        const token = $('input[name="__RequestVerificationToken"]').first().val();
        if (token) headers.RequestVerificationToken = token;
        return headers;
    }

    function confirmAction(options) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.confirm === 'function') return window.Symplify.Ajax.confirm(options);
        return Promise.resolve({ isConfirmed: window.confirm(options && options.text ? options.text : 'Emin misiniz?') });
    }

    function getDataTableLanguage() { return window.Symplify.DataTables?.language || {}; }
    function showSuccess(message) { if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') { window.Symplify.Ajax.showSuccess(message); return; } console.info(message); }
    function showError(response) { if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') { window.Symplify.Ajax.showError(response); return; } alert(normalizeMessage(response) || text('genericError', 'İşlem sırasında hata oluştu.')); }
    function normalizeMessage(value) { if (!value) return null; if (typeof value === 'object') return normalizeMessage(value.responseJSON || value.message || value.title || value.detail || value.responseText); const textValue = String(value).trim(); return textValue.length ? textValue : null; }
    function text(key, fallback) { return typeof window.Symplify.t === 'function' ? window.Symplify.t('BackOffice.CongressDocuments.Js.' + key, fallback) : fallback; }
    function getCurrentCulture() { const htmlCulture = document.documentElement.getAttribute('lang') || $('html').attr('lang'); if (htmlCulture) return htmlCulture; const segments = window.location.pathname.split('/').filter(Boolean); return segments.length > 0 ? segments[0] : 'tr-TR'; }
    function escapeHtml(value) { return $('<div/>').text(value === null || value === undefined ? '' : value).html(); }
    function escapeAttribute(value) { if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value); return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1'); }

    function ensureReorderStyles() {
        if (document.getElementById('symplify-document-reorder-styles')) return;
        const style = document.createElement('style');
        style.id = 'symplify-document-reorder-styles';
        style.textContent = '.lookup-row-dragging{opacity:.65}.lookup-sort-placeholder td{height:56px;border:2px dashed #6b8cff;background:rgba(59,130,246,.06)}.js-document-drag-handle{cursor:grab;min-width:24px}.js-document-drag-handle:active{cursor:grabbing}.js-document-drag-handle.opacity-50{cursor:not-allowed!important}';
        document.head.appendChild(style);
    }

    return { init: init, reload: reload };
})(jQuery);

$(function () { window.Symplify.CongressDocuments.Index.init(); });
