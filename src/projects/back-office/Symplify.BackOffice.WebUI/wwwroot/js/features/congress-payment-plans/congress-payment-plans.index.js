window.Symplify = window.Symplify || {};
window.Symplify.CongressPaymentPlans = window.Symplify.CongressPaymentPlans || {};

window.Symplify.CongressPaymentPlans.Index = (function ($) {
    'use strict';

    const selectors = {
        panel: '#congressPaymentPlanPanel',
        table: '#congressPaymentPlansTable',
        modalContainer: '#congressPaymentPlanModalContainer',
        createButton: '#openCreatePaymentPlanModalButton',
        createForm: '#createCongressPaymentPlanForm',
        updateForm: '#updateCongressPaymentPlanForm',
        audienceFilter: '.payment-plan-audience-filter',
        categoryFilter: '.payment-plan-category-filter',
        publicFilter: '.payment-plan-public-filter',
        statusFilter: '.payment-plan-status-filter',
        resetFilter: '.payment-plan-filter-reset'
    };

    let table;

    function init() {
        if (!$(selectors.panel).length || !$(selectors.table).length) return;

        loadFilterOptions();
        initializeTable();
        bindEvents();
    }

    function bindEvents() {
        $(document).off('click.paymentPlanCreate', selectors.createButton).on('click.paymentPlanCreate', selectors.createButton, openCreateModal);
        $(document).off('click.paymentPlanEdit', '.js-edit-payment-plan').on('click.paymentPlanEdit', '.js-edit-payment-plan', openUpdateModal);
        $(document).off('click.paymentPlanDelete', '.js-delete-payment-plan').on('click.paymentPlanDelete', '.js-delete-payment-plan', deletePaymentPlan);
        $(document).off('submit.paymentPlanCreate', selectors.createForm).on('submit.paymentPlanCreate', selectors.createForm, submitForm);
        $(document).off('submit.paymentPlanUpdate', selectors.updateForm).on('submit.paymentPlanUpdate', selectors.updateForm, submitForm);
        $(document).off('change.paymentPlanFilters', selectors.audienceFilter + ',' + selectors.categoryFilter + ',' + selectors.publicFilter + ',' + selectors.statusFilter)
            .on('change.paymentPlanFilters', selectors.audienceFilter + ',' + selectors.categoryFilter + ',' + selectors.publicFilter + ',' + selectors.statusFilter, reload);
        $(document).off('click.paymentPlanFilterReset', selectors.resetFilter).on('click.paymentPlanFilterReset', selectors.resetFilter, resetFilters);
    }

    function initializeTable() {
        const $panel = $(selectors.panel);
        const $table = $(selectors.table);

        if (!$.fn.DataTable) {
            console.error('DataTables plugin bulunamadı. Congress payment plans tablosu başlatılamadı.');
            return;
        }

        if ($.fn.DataTable.isDataTable($table)) {
            table = $table.DataTable();
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
            order: [[0, 'asc']],
            ajax: {
                url: $panel.data('source-url'),
                type: 'POST',
                headers: buildAjaxHeaders($panel),
                data: function (data) {
                    data.congressId = $panel.data('congress-id');
                    data.audienceType = $(selectors.audienceFilter).val();
                    data.paymentCategory = $(selectors.categoryFilter).val();
                    data.publicVisibility = $(selectors.publicFilter).val();
                    data.status = $(selectors.statusFilter).val();
                    return data;
                },
                error: showError
            },
            columns: [
                { data: 'order', name: 'order', orderable: true, searchable: false, className: 'text-nowrap', render: renderOrder },
                { data: 'name', name: 'name', orderable: true, searchable: true, render: renderName },
                { data: 'audienceTypeText', name: 'audienceType', orderable: true, searchable: true, className: 'text-nowrap', render: renderText },
                { data: 'paymentCategoryText', name: 'paymentCategory', orderable: true, searchable: true, className: 'text-nowrap', render: renderText },
                { data: 'amountText', name: 'amount', orderable: true, searchable: false, className: 'text-nowrap text-end', render: renderText },
                { data: 'currency', name: 'currency', orderable: true, searchable: true, className: 'text-nowrap', render: renderCurrency },
                { data: 'validityText', name: 'validUntil', orderable: true, searchable: false, className: 'text-nowrap', render: renderText },
                { data: 'isPublicVisible', name: 'isPublicVisible', orderable: true, searchable: false, className: 'text-nowrap', render: renderVisibility },
                { data: 'isActive', name: 'isActive', orderable: true, searchable: false, className: 'text-nowrap', render: renderStatus },
                { data: null, name: 'actions', orderable: false, searchable: false, className: 'text-end text-nowrap', render: renderActions }
            ],
            language: getDataTableLanguage()
        });
    }

    function loadFilterOptions() {
        const url = $(selectors.panel).data('filter-options-url');
        if (!url) return;

        $.get(url)
            .done(function (response) {
                fillSelect($(selectors.audienceFilter), response ? response.audienceTypes : null, text('all', 'Tümü'));
                fillSelect($(selectors.categoryFilter), response ? response.paymentCategories : null, text('all', 'Tümü'));
            })
            .fail(showError);
    }

    function fillSelect($select, items, firstText) {
        const currentValue = $select.val();
        $select.empty();
        $select.append($('<option/>').attr('value', '').text(firstText));

        (items || []).forEach(function (item) {
            if (!item || !item.value) return;
            $select.append($('<option/>').attr('value', item.value).text(item.text || item.value));
        });

        if (currentValue) $select.val(currentValue);
    }

    function openCreateModal() {
        $.get($(selectors.panel).data('create-modal-url'))
            .done(function (html) { showModalHtml(html, '#createPaymentPlanModal'); })
            .fail(showError);
    }

    function openUpdateModal() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        $.get($panel.data('edit-modal-url'), { id: $button.data('id'), congressId: $panel.data('congress-id') })
            .done(function (html) { showModalHtml(html, '#updatePaymentPlanModal'); })
            .fail(showError);
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

    function deletePaymentPlan() {
        const $button = $(this);
        const $panel = $(selectors.panel);

        confirmAction({
            title: text('deleteConfirmTitle', 'Emin misiniz?'),
            text: text('deleteConfirmText', 'Bu ödeme planı silinecek.'),
            confirmButtonText: text('deleteConfirmButton', 'Sil')
        }).then(function (result) {
            if (!result || result.isConfirmed !== true) return;

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
                .fail(showError);
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
            if (instance) instance.dispose();
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
        if (!modalElement) return;
        bootstrap.Modal.getOrCreateInstance(modalElement).hide();
    }

    function reload(resetPaging) {
        if (!table) return;
        table.ajax.reload(null, resetPaging === true);
    }

    function resetFilters() {
        $(selectors.audienceFilter).val('');
        $(selectors.categoryFilter).val('');
        $(selectors.publicFilter).val('');
        $(selectors.statusFilter).val('');
        reload(true);
    }

    function renderOrder(data, type, row, meta) {
        const value = Number(data);
        return '<span class="fw-medium text-secondary-light">' + escapeHtml(Number.isFinite(value) && value > 0 ? value : ((meta && meta.row ? meta.row : 0) + 1)) + '</span>';
    }

    function renderName(data, type, row) {
        const name = escapeHtml(data || '-');
        const code = row && row.code ? '<small class="text-neutral-500 d-block mt-1">' + escapeHtml(row.code) + '</small>' : '';
        const fallback = row && row.isFallback ? ' <span class="badge bg-warning-light text-warning rounded-pill ms-1">fallback</span>' : '';
        const description = row && row.description ? '<small class="text-neutral-500 d-block mt-1">' + escapeHtml(truncate(row.description, 90)) + '</small>' : '';
        return '<span class="fw-medium text-secondary-light">' + name + '</span>' + fallback + code + description;
    }

    function renderCurrency(data) {
        return '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill">' + escapeHtml(data || '-') + '</span>';
    }

    function renderVisibility(data) {
        return data === true
            ? '<span class="badge bg-success-light text-success rounded-pill">' + escapeHtml(text('visible', 'Görünür')) + '</span>'
            : '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill">' + escapeHtml(text('hidden', 'Gizli')) + '</span>';
    }

    function renderStatus(data) {
        return data === true
            ? '<span class="badge bg-success-light text-success rounded-pill">' + escapeHtml(text('active', 'Aktif')) + '</span>'
            : '<span class="badge bg-neutral-200 text-neutral-700 rounded-pill">' + escapeHtml(text('passive', 'Pasif')) + '</span>';
    }

    function renderText(data) {
        return escapeHtml(data || '-');
    }

    function renderActions(data, type, row) {
        return '' +
            '<div class="d-flex align-items-center justify-content-end gap-2">' +
                '<button type="button" aria-label="Düzenle" class="btn btn-primary-100 text-primary-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-edit-payment-plan" data-id="' + escapeHtml(row.id) + '"><i class="ri-edit-line"></i></button>' +
                '<button type="button" aria-label="Sil" class="btn btn-danger-100 text-danger-600 radius-8 px-12 py-8 d-flex align-items-center justify-content-center w-40-px h-40-px js-delete-payment-plan" data-id="' + escapeHtml(row.id) + '"><i class="ri-delete-bin-line"></i></button>' +
            '</div>';
    }

    function postForm($form) {
        if (window.Symplify.Ajax && typeof window.Symplify.Ajax.postForm === 'function') {
            return window.Symplify.Ajax.postForm($form);
        }

        return $.ajax({
            url: $form.attr('action'),
            type: $form.attr('method') || 'POST',
            data: new FormData($form[0]),
            processData: false,
            contentType: false,
            headers: buildAjaxHeaders($form)
        });
    }

    function prepareForm($form) {
        clearValidationErrors($form);
        if (window.Symplify.Forms && typeof window.Symplify.Forms.prepareForSubmit === 'function') {
            window.Symplify.Forms.prepareForSubmit($form);
        }
    }

    function renderValidationErrors($form, response) {
        const payload = response && response.responseJSON ? response.responseJSON : response;
        if (!payload || !payload.errors) return false;

        const errors = payload.errors;
        const orphanMessages = [];
        let hasFieldError = false;

        Object.keys(errors).forEach(function (key) {
            const fieldMessages = Array.isArray(errors[key]) ? errors[key] : [errors[key]];
            const message = fieldMessages.filter(Boolean).join(' ');
            if (!message) return;

            const $field = findField($form, key);
            const $validation = findValidationMessage($form, key);

            if ($field.length || $validation.length) {
                hasFieldError = true;

                if ($field.length) {
                    $field.addClass('is-invalid');
                }

                if ($validation.length) {
                    $validation.text(message);
                }

                return;
            }

            orphanMessages.push(message);
        });

        if (hasFieldError) {
            focusFirstInvalidField($form);
            return true;
        }

        if (orphanMessages.length) {
            showError({ message: orphanMessages.join(' ') });
            return true;
        }

        return true;
    }

    function clearValidationErrors($form) {
        $form.find('.is-invalid').removeClass('is-invalid');
        $form.find('[data-valmsg-for]').empty();
    }

    function findField($form, key) {
        return $form.find('[name="' + escapeAttribute(key) + '"]');
    }

    function findValidationMessage($form, key) {
        return $form.find('[data-valmsg-for="' + escapeAttribute(key) + '"]');
    }

    function focusFirstInvalidField($form) {
        const $field = $form.find('.is-invalid, .input-validation-error').filter(':visible').first();
        if ($field.length) $field.trigger('focus');
    }

    function hasJQueryValidation() {
        return !!($.validator && $.validator.unobtrusive);
    }

    function setBusy($form, isBusy) {
        const $submit = $form.find('[type="submit"]');
        $submit.prop('disabled', isBusy === true);
    }

    function cleanupModalArtifacts() {
        if ($('.modal.show').length) return;
        $('.modal-backdrop').remove();
        $('body').removeClass('modal-open').css({ overflow: '', paddingRight: '' });
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

    function getDataTableLanguage() {
        if (window.Symplify.DataTables && typeof window.Symplify.DataTables.getLanguage === 'function') {
            return window.Symplify.DataTables.getLanguage();
        }
        return {
            search: text('search', 'Ara:'),
            lengthMenu: '_MENU_ ' + text('lengthMenu', 'kayıt göster'),
            info: text('info', '_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor'),
            infoEmpty: text('infoEmpty', 'Kayıt bulunamadı'),
            zeroRecords: text('zeroRecords', 'Eşleşen kayıt bulunamadı'),
            paginate: {
                first: text('first', 'İlk'),
                last: text('last', 'Son'),
                next: text('next', 'Sonraki'),
                previous: text('previous', 'Önceki')
            }
        };
    }

    function text(key, fallback) {
        if (typeof window.Symplify.t === 'function') {
            return window.Symplify.t('BackOffice.CongressPaymentPlans.Js.' + key, fallback);
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
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|\/@])/g, '\\$1');
    }

    return { init: init, reload: reload };
})(jQuery);

$(function () {
    window.Symplify.CongressPaymentPlans.Index.init();
});
