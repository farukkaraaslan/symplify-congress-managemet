window.Symplify = window.Symplify || {};
window.Symplify.CongressImportantDates = window.Symplify.CongressImportantDates || {};

window.Symplify.CongressImportantDates.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressImportantDatePanel',
        table: '#congressImportantDatesTable',
        modalContainer: '#congressImportantDateModalContainer',
        createButton: '#openCreateImportantDateModalButton',
        createForm: '#createCongressImportantDateForm',
        updateForm: '#updateCongressImportantDateForm',
        dragHandle: '.js-important-date-drag-handle'
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
            console.error('DataTables plugin bulunamadı. Important date tablosu başlatılamadı.');
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
                error: function (xhr) {
                    showError(xhr);
                }
            },
            columns: [
                { data: 'rowNumber', name: 'rowNumber', orderable: false, searchable: false, className: 'text-nowrap' },
                { data: null, name: 'dragHandle', orderable: false, searchable: false, className: 'text-center text-nowrap', render: renderDragHandle },
                { data: 'order', name: 'order', orderable: true, searchable: true, className: 'text-nowrap', render: renderOrder },
                { data: 'startDate', name: 'startDate', orderable: true, searchable: false, className: 'text-nowrap', render: renderDate },
                { data: 'endDate', name: 'endDate', orderable: true, searchable: false, className: 'text-nowrap', render: renderDate },
                { data: 'title', name: 'title', orderable: true, searchable: true, render: renderTitle },
                { data: 'description', name: 'description', orderable: false, searchable: true, render: renderDescription },
                { data: null, name: 'language', orderable: false, searchable: false, className: 'text-nowrap', render: renderLanguage },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, className: 'text-nowrap', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            createdRow: function (row, data) {
                if (data && data.id) {
                    $(row)
                        .attr('data-id', data.id)
                        .attr('data-order', data.order || '')
                        .addClass('js-important-date-row');
                }
            },
            drawCallback: function () {
                initializeReorder();
                updateDragHandleState();
            },
            language: getDataTableLanguage()
        });

        $table
            .off('order.dt.congressImportantDates search.dt.congressImportantDates page.dt.congressImportantDates draw.dt.congressImportantDates')
            .on('order.dt.congressImportantDates search.dt.congressImportantDates page.dt.congressImportantDates draw.dt.congressImportantDates', function () {
                window.setTimeout(function () {
                    initializeReorder();
                    updateDragHandleState();
                }, 0);
            });
    }

    function bindEvents() {
        $(document)
            .off('click.congressImportantDatesCreate', selectors.createButton)
            .on('click.congressImportantDatesCreate', selectors.createButton, openCreateModal);

        $(document)
            .off('click.congressImportantDatesEdit', '.js-edit-congress-important-date')
            .on('click.congressImportantDatesEdit', '.js-edit-congress-important-date', openUpdateModal);

        $(document)
            .off('click.congressImportantDatesDelete', '.js-delete-congress-important-date')
            .on('click.congressImportantDatesDelete', '.js-delete-congress-important-date', deleteImportantDate);

        $(document)
            .off('submit.congressImportantDatesCreate', selectors.createForm)
            .on('submit.congressImportantDatesCreate', selectors.createForm, submitForm);

        $(document)
            .off('submit.congressImportantDatesUpdate', selectors.updateForm)
            .on('submit.congressImportantDatesUpdate', selectors.updateForm, submitForm);
    }

    function openCreateModal() {
        const url = $(selectors.panel).data('create-modal-url');

        $.get(url)
            .done(function (html) {
                showModalHtml(html, '#createImportantDateModal');
            })
            .fail(function (xhr) {
                showError(xhr);
            });
    }

    function openUpdateModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('edit-modal-url'), {
            id: $button.data('id'),
            congressId: $panel.data('congress-id')
        })
            .done(function (html) {
                showModalHtml(html, '#updateImportantDateModal');
            })
            .fail(function (xhr) {
                showError(xhr);
            });
    }

    function submitForm(event) {
        event.preventDefault();

        const $form = $(this);

        if (window.Symplify.TempusDominus && typeof window.Symplify.TempusDominus.syncAll === 'function') {
            window.Symplify.TempusDominus.syncAll($form);
        }

        prepareForm($form);

        if (hasJQueryValidation() && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        setBusy($form, true);

        postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) {
                        return;
                    }

                    showError(response);
                    return;
                }

                hideModal($form.closest('.modal'));
                reload(false);
                showSuccess(response.message || text('saved', 'Kayıt kaydedildi.'));
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) {
                    return;
                }

                showError(xhr);
            })
            .always(function () {
                setBusy($form, false);
            });
    }

    function deleteImportantDate() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        confirmAction({
            title: text('deleteConfirmTitle', 'Emin misiniz?'),
            text: text('deleteConfirmText', 'Bu önemli tarih silinecek.'),
            confirmButtonText: text('deleteConfirmButton', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) {
                return;
            }

            $.ajax({
                url: $panel.data('delete-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: {
                    id: $button.data('id'),
                    congressId: $panel.data('congress-id')
                }
            })
                .done(function (response) {
                    if (!response || response.success !== true) {
                        showError(response);
                        return;
                    }

                    reload(false);
                    showSuccess(response.message || text('deleted', 'Kayıt silindi.'));
                })
                .fail(function (xhr) {
                    showError(xhr);
                });
        });
    }

    function showModalHtml(html, modalSelector) {
        cleanupModalArtifacts();
        $(selectors.modalContainer).empty();

        const $html = $(html);
        const $modal = $html.filter(modalSelector).add($html.find(modalSelector)).first();

        if (!$modal.length) {
            showError(text('modalNotFound', 'Modal içeriği yüklenemedi.'));
            return;
        }

        $modal.appendTo(document.body);
        initializeModal($modal);

        const modalElement = $modal[0];

        $modal.one('hidden.bs.modal', function () {
            if (window.Symplify.TempusDominus && typeof window.Symplify.TempusDominus.destroy === 'function') {
                window.Symplify.TempusDominus.destroy($modal);
            }

            const instance = bootstrap.Modal.getInstance(modalElement);

            if (instance) {
                instance.dispose();
            }

            $modal.remove();
            cleanupModalArtifacts();
        });

        bootstrap.Modal.getOrCreateInstance(modalElement, {
            backdrop: true,
            focus: true,
            keyboard: true
        }).show();
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

        if (window.Symplify.TempusDominus && typeof window.Symplify.TempusDominus.initAll === 'function') {
            window.Symplify.TempusDominus.initAll($modal);
        }
    }

    function hideModal($modal) {
        const modalElement = $modal && $modal.length ? $modal[0] : null;

        if (!modalElement) {
            return;
        }

        bootstrap.Modal.getOrCreateInstance(modalElement).hide();
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) {
            return;
        }

        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
    }

    function renderDragHandle() {
        const label = text('dragHandle', 'Sırayı değiştirmek için sürükleyin');

        return '' +
            '<span role="button" tabindex="0" class="d-inline-flex align-items-center justify-content-center text-neutral-500 js-important-date-drag-handle" title="' + escapeHtml(label) + '" aria-label="' + escapeHtml(label) + '">' +
                '<i class="ri-draggable"></i>' +
            '</span>';
    }

    function renderOrder(data, type, row, meta) {
        const numericValue = Number(data);
        const value = Number.isFinite(numericValue) && numericValue > 0
            ? numericValue
            : (meta && typeof meta.row === 'number' ? meta.row + 1 : 1);

        return '<span class="fw-medium text-secondary-light js-important-date-order-value">' + escapeHtml(value) + '</span>';
    }

    function renderDate(data) {
        return '<span class="fw-medium text-secondary-light">' + escapeHtml(data || '-') + '</span>';
    }

    function renderTitle(data, type, row) {
        const title = data || '-';
        const fallback = row && row.isFallback
            ? '<span class="badge bg-warning-light text-warning rounded-pill ms-2">' + text('fallback', 'Fallback') + '</span>'
            : '';

        return '<span class="fw-medium text-secondary-light">' + escapeHtml(title) + '</span>' + fallback;
    }

    function renderDescription(data) {
        const description = data || '-';
        return '<span class="text-neutral-500">' + escapeHtml(truncate(description, 90)) + '</span>';
    }

    function renderLanguage(data, type, row) {
        const culture = row && row.culture ? row.culture : getCurrentCulture();
        return '<span class="badge bg-primary-50 text-primary-600 rounded-pill px-12 py-6">' + escapeHtml(culture) + '</span>';
    }

    function renderStatus(isActive) {
        return isActive
            ? '<span class="badge bg-success-100 text-success-600 rounded-pill px-12 py-6">' + text('active', 'Aktif') + '</span>'
            : '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill px-12 py-6">' + text('passive', 'Pasif') + '</span>';
    }

    function renderActions(data, type, row) {
        const id = row && row.id ? row.id : '';

        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
                '<button type="button" class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-edit-congress-important-date" data-id="' + escapeHtml(id) + '" aria-label="' + escapeHtml(text('edit', 'Düzenle')) + '">' +
                    '<i class="ri-edit-line"></i>' +
                '</button>' +
                '<button type="button" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-delete-congress-important-date" data-id="' + escapeHtml(id) + '" aria-label="' + escapeHtml(text('delete', 'Sil')) + '">' +
                    '<i class="ri-delete-bin-line"></i>' +
                '</button>' +
            '</div>';
    }

    function initializeReorder() {
        if (!table) {
            return;
        }

        if ($.fn.sortable) {
            initializeSortableReorder();
            return;
        }

        initializeNativeReorder();
    }

    function initializeSortableReorder() {
        const $tbody = $(table.table().body());
        $tbody.off('.importantDateNativeReorder');

        if ($tbody.data('ui-sortable')) {
            $tbody.sortable('destroy');
        }

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
                $helper.children().each(function (index) {
                    $(this).width($originals.eq(index).width());
                });
                return $helper;
            },
            start: function (event, ui) {
                if (!isReorderAllowed()) {
                    $(this).sortable('cancel');
                    showReorderNotAllowedMessage();
                    return;
                }

                ui.item.addClass('lookup-row-dragging');
                ui.placeholder.html('<td colspan="10">&nbsp;</td>');
            },
            sort: updateVisibleRowNumbers,
            update: function () {
                updateVisibleRowNumbers();
                persistReorder();
            },
            stop: function (event, ui) {
                ui.item.removeClass('lookup-row-dragging');
                updateVisibleRowNumbers();
            }
        });

        $tbody.sortable(isReorderAllowed() ? 'enable' : 'disable');
    }

    function initializeNativeReorder() {
        const $tbody = $(table.table().body());
        let draggedRow = null;
        let dragChanged = false;

        if ($tbody.data('ui-sortable')) {
            $tbody.sortable('destroy');
        }

        $tbody.off('.importantDateNativeReorder');

        $tbody.on('dragstart.importantDateNativeReorder', selectors.dragHandle, function (event) {
            if (!isReorderAllowed()) {
                event.preventDefault();
                showReorderNotAllowedMessage();
                return false;
            }

            const $row = $(this).closest('tr[data-id]');
            if (!$row.length) {
                event.preventDefault();
                return false;
            }

            draggedRow = $row[0];
            dragChanged = false;
            $row.addClass('lookup-row-dragging');

            if (event.originalEvent && event.originalEvent.dataTransfer) {
                event.originalEvent.dataTransfer.effectAllowed = 'move';
                event.originalEvent.dataTransfer.setData('text/plain', $row.attr('data-id') || '');
            }

            return true;
        });

        $tbody.on('dragover.importantDateNativeReorder', 'tr[data-id]', function (event) {
            if (!draggedRow || draggedRow === this) {
                return;
            }

            event.preventDefault();

            const rect = this.getBoundingClientRect();
            const mouseY = event.originalEvent.clientY;
            const shouldInsertAfter = mouseY > rect.top + rect.height / 2;

            if (shouldInsertAfter) {
                this.parentNode.insertBefore(draggedRow, this.nextSibling);
            } else {
                this.parentNode.insertBefore(draggedRow, this);
            }

            dragChanged = true;
            updateVisibleRowNumbers();
        });

        $tbody.on('drop.importantDateNativeReorder', 'tr[data-id]', function (event) {
            event.preventDefault();
        });

        $tbody.on('dragend.importantDateNativeReorder', selectors.dragHandle, function () {
            if (draggedRow) {
                $(draggedRow).removeClass('lookup-row-dragging');
            }

            if (draggedRow && dragChanged) {
                persistReorder();
            }

            draggedRow = null;
            dragChanged = false;
        });
    }

    function updateDragHandleState() {
        if (!table) {
            return;
        }

        const allowed = isReorderAllowed();
        const $tbody = $(table.table().body());
        const $handles = $tbody.find(selectors.dragHandle);

        if ($.fn.sortable && $tbody.data('ui-sortable')) {
            $tbody.sortable(allowed ? 'enable' : 'disable');
        }

        $handles
            .attr('draggable', allowed ? 'true' : 'false')
            .toggleClass('opacity-50', !allowed)
            .css('cursor', allowed ? 'grab' : 'not-allowed')
            .attr('title', allowed
                ? text('dragHandle', 'Sırayı değiştirmek için sürükleyin')
                : text('reorderNotAllowedShort', 'Sıralama için arama boşken Sıra No kolonunu artan kullanın.'));
    }

    function isReorderAllowed() {
        if (!table) {
            return false;
        }

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
        if (!table) {
            return;
        }

        const pageInfo = table.page.info();

        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const visibleNumber = pageInfo.start + index + 1;
            $(this).find('td').eq(0).text(visibleNumber);
            $(this).find('td').eq(2).find('.js-important-date-order-value').text(visibleNumber);
        });
    }

    function persistReorder() {
        if (!table) {
            return;
        }

        if (!isReorderAllowed()) {
            reload(false);
            return;
        }

        const $table = $(selectors.table);
        const reorderUrl = resolveReorderUrl($table);

        if (!reorderUrl) {
            showError({ responseJSON: { message: text('reorderEndpointMissing', 'Sıralama endpoint adresi bulunamadı.') } });
            reload(false);
            return;
        }

        const pageInfo = table.page.info();
        const items = [];

        $(table.table().body()).find('tr[data-id]').each(function (index) {
            const id = $(this).attr('data-id');
            if (!id) return;
            items.push({ id: id, order: pageInfo.start + index + 1 });
        });

        if (!items.length) {
            reload(false);
            return;
        }

        $.ajax({
            url: reorderUrl,
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            data: JSON.stringify({ items: items }),
            headers: buildAjaxHeaders($(document))
        })
            .done(function (response) {
                if (!response || response.success !== true) {
                    showError(response);
                    reload(false);
                    return;
                }

                showSuccess(response.message || text('reordered', 'Sıralama güncellendi.'));
                reload(false);
            })
            .fail(function (xhr) {
                showError(xhr);
                reload(false);
            });
    }

    function resolveReorderUrl($table) {
        const explicitUrl = $table.data('reorder-url') || $(selectors.panel).data('reorder-url');
        if (explicitUrl) return explicitUrl;

        const sourceUrl = String($(selectors.panel).data('source-url') || $table.data('source-url') || '');
        if (!sourceUrl) return null;

        return sourceUrl.replace(/\/GetList(?:\?.*)?$/i, '/Reorder');
    }

    function reload(resetPaging) {
        if (table) {
            table.ajax.reload(null, resetPaging === true);
        }
    }

    function setBusy($form, isBusy) {
        const $buttons = $form.find('button[type="submit"]');
        $buttons.prop('disabled', isBusy);

        if (isBusy) {
            $buttons.each(function () {
                const $button = $(this);
                if (!$button.attr('data-original-text')) $button.attr('data-original-text', $button.html());
                $button.html('<span class="spinner-border spinner-border-sm me-2"></span>' + escapeHtml(text('saving', 'Kaydediliyor...')));
            });
            return;
        }

        $buttons.each(function () {
            const $button = $(this);
            const originalText = $button.attr('data-original-text');
            if (originalText) $button.html(originalText).removeAttr('data-original-text');
        });
    }

    function prepareForm($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.prepareForSubmit === 'function') {
            window.Symplify.Forms.prepareForSubmit($form);
            return;
        }

        $form.find('.field-validation-error').removeClass('field-validation-error').addClass('field-validation-valid').empty();
        $form.find('.input-validation-error, .is-invalid').removeClass('input-validation-error is-invalid');
    }

    function postForm($form) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.postForm === 'function') {
            return window.Symplify.Ajax.postForm($form);
        }

        return $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            data: $form.serialize(),
            headers: buildAjaxHeaders($form)
        });
    }

    function renderValidationErrors($form, response) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.renderValidationErrors === 'function') {
            return window.Symplify.Forms.renderValidationErrors($form, response);
        }

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

    function focusFirstInvalidField($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.focusFirstInvalidField === 'function') {
            window.Symplify.Forms.focusFirstInvalidField($form);
            return;
        }
        const $field = $form.find('.input-validation-error, .is-invalid').first();
        if ($field.length) $field.trigger('focus');
    }

    function hasJQueryValidation() {
        return typeof $.validator !== 'undefined' && typeof $.validator.unobtrusive !== 'undefined';
    }

    function getDataTableLanguage() {
        return window.Symplify.DataTables?.language || {
            search: text('search', 'Ara:'),
            lengthMenu: text('lengthMenu', '_MENU_ kayıt göster'),
            info: text('info', '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor'),
            infoEmpty: text('infoEmpty', 'Kayıt bulunamadı'),
            zeroRecords: text('zeroRecords', 'Eşleşen kayıt bulunamadı'),
            processing: text('processing', 'Yükleniyor...'),
            paginate: { first: text('first', 'İlk'), last: text('last', 'Son'), next: text('next', 'Sonraki'), previous: text('previous', 'Önceki') }
        };
    }

    function buildAjaxHeaders($source) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.buildAjaxHeaders === 'function') {
            return window.Symplify.Ajax.buildAjaxHeaders($source);
        }

        const headers = { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json', 'X-Culture': getCurrentCulture() };
        const token = $('input[name="__RequestVerificationToken"]').first().val();
        if (token) headers.RequestVerificationToken = token;
        return headers;
    }

    function confirmAction(options) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.confirm === 'function') {
            return window.Symplify.Ajax.confirm(options);
        }
        const confirmed = window.confirm(options && options.text ? options.text : 'Emin misiniz?');
        return Promise.resolve({ isConfirmed: confirmed });
    }

    function showSuccess(message) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showSuccess === 'function') {
            window.Symplify.Ajax.showSuccess(message);
            return;
        }
        console.info(message);
    }

    function showError(response) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.showError === 'function') {
            window.Symplify.Ajax.showError(response);
            return;
        }
        window.alert(normalizeMessage(response) || text('genericError', 'İşlem sırasında hata oluştu.'));
    }

    function normalizeMessage(value) {
        if (!value) return null;
        if (typeof value === 'object') return normalizeMessage(value.responseJSON || value.message || value.title || value.detail || value.responseText);
        const textValue = String(value).trim();
        return textValue.length ? textValue : null;
    }

    function text(key, fallback) {
        if (typeof window.Symplify.t === 'function') {
            return window.Symplify.t('BackOffice.CongressImportantDates.Js.' + key, fallback);
        }
        return fallback;
    }

    function getCurrentCulture() {
        const htmlCulture = document.documentElement.getAttribute('lang') || $('html').attr('lang');
        if (htmlCulture) return htmlCulture;
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : 'tr-TR';
    }

    function truncate(value, length) {
        value = String(value || '');
        return value.length > length ? value.substring(0, length - 3) + '...' : value;
    }

    function escapeHtml(value) {
        return $('<div/>').text(value === null || value === undefined ? '' : value).html();
    }

    function escapeAttribute(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1');
    }

    function ensureReorderStyles() {
        if (document.getElementById('symplify-important-date-reorder-styles')) return;
        const style = document.createElement('style');
        style.id = 'symplify-important-date-reorder-styles';
        style.textContent = '' +
            '.lookup-row-dragging{opacity:.65;}' +
            '.lookup-sort-placeholder td{height:56px;border:2px dashed #6b8cff;background:rgba(59,130,246,.06);}' +
            '.js-important-date-drag-handle{cursor:grab;min-width:24px;}' +
            '.js-important-date-drag-handle:active{cursor:grabbing;}' +
            '.js-important-date-drag-handle.opacity-50{cursor:not-allowed!important;}';
        document.head.appendChild(style);
    }

    return { init: init, reload: reload };
})(jQuery);

$(function () {
    window.Symplify.CongressImportantDates.Index.init();
});
