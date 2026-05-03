window.Symplify = window.Symplify || {};
window.Symplify.TransactionStatuses = window.Symplify.TransactionStatuses || {};

window.Symplify.TransactionStatuses.Update = (function ($) {
    'use strict';

    const selectors = { editButton: '.js-transaction-status-update-button', modal: '#updateTransactionStatusModal', form: '#updateTransactionStatusForm', table: '#transactionStatusesTable' };

    function init() {
        initializeValidation($(selectors.form));
        bindSwitches($(selectors.form));
        $(document).off('click.transactionstatusUpdate', selectors.editButton).on('click.transactionstatusUpdate', selectors.editButton, load);
        $(document).off('submit.transactionstatusUpdate', selectors.form).on('submit.transactionstatusUpdate', selectors.form, submitForm);
        $(document).off('shown.bs.modal.transactionstatusUpdate', selectors.modal).on('shown.bs.modal.transactionstatusUpdate', selectors.modal, function () { initializeValidation($(selectors.form)); bindSwitches($(selectors.form)); });
    }

    function load() {
        const id = $(this).data('id'); const url = resolveGetForUpdateUrl(); if (!id || !url) return;
        $.ajax({ url: url, type: 'GET', data: { id: id }, headers: { 'X-Culture': getCurrentCulture() } }).done(function (response) {
            if (!response || response.success === false) { showError(response); return; }
            fillForm(response); showModal(selectors.modal);
        }).fail(showError);
    }

    function submitForm(event) {
        event.preventDefault(); const $form = $(this); initializeValidation($form);
        if (hasJQueryValidation() && !$form.valid()) { focusFirstInvalidField($form); return; }
        clearValidationErrors($form);
        postForm($form).done(function (response) {
            if (!response || response.success !== true) { if (tryRenderValidationErrors($form, response)) return; showError(response); return; }
            hideModal(selectors.modal);
            if (window.Symplify.TransactionStatuses.Table) window.Symplify.TransactionStatuses.Table.reload(false);
            showSuccess(response.message || getText('updated', 'Kayıt güncellendi.'));
        }).fail(function (xhr) { if (tryRenderValidationErrors($form, xhr)) return; showError(xhr); });
    }

    function fillForm(response) {
        const $form = $(selectors.form); if (!$form.length) return; clearValidationErrors($form);
        $form.find('[name="Id"]').val(response.id || '');
        $form.find('[name="Code"]').val(response.code || '');
        $form.find('.js-lookup-status-switch').prop('checked', response.isActive === true).trigger('change');
        $form.find('[name="TransactionStatusPhaseId"]').val(response.transactionStatusPhaseId || ''); $form.find('.js-transaction-status-editable-switch').prop('checked', response.isEditable === true).trigger('change'); $form.find('.js-transaction-status-final-switch').prop('checked', response.isFinal === true).trigger('change');
        clearTranslations($form);
        if (Array.isArray(response.translations)) { response.translations.forEach(function (translation) { const languageId = normalizeGuid(translation.languageId); $form.find('[name$=".Exists"][data-language-id="' + languageId + '"]').val(translation.exists === true ? 'true' : 'false'); $form.find('[name$=".Name"][data-language-id="' + languageId + '"]').val(translation.name || ''); $form.find('[name$=".Description"][data-language-id="' + languageId + '"]').val(translation.description || ''); }); }
        initializeValidation($form); bindSwitches($form); showFirstTab($form);
    }

    function clearTranslations($form) { $form.find('[name$=".Exists"]').val('false'); $form.find('[name$=".Name"]').val(''); $form.find('[name$=".Description"]').val('');  }
    function resolveGetForUpdateUrl() { return $(selectors.table).data('get-for-update-url') || window.Symplify.TransactionStatuses.urls?.getForUpdate || null; }
    function initializeValidation($form) { if (!$form || !$form.length || !hasJQueryValidation()) return; $form.removeData('validator'); $form.removeData('unobtrusiveValidation'); $.validator.unobtrusive.parse($form); }
    function hasJQueryValidation() { return typeof $.validator !== 'undefined' && typeof $.validator.unobtrusive !== 'undefined'; }
    function postForm($form) { return window.Symplify.Ajax?.postForm ? window.Symplify.Ajax.postForm($form) : $.ajax({ url: $form.attr('action'), type: $form.attr('method') || 'POST', data: $form.serialize() }); }
    function tryRenderValidationErrors($form, response) { const errors = (response?.responseJSON || response)?.errors; if (!errors) return false; const validator = $form.data('validator'); const normalizedErrors = {}; Object.keys(errors).forEach(function (key) { const messages = errors[key]; const message = Array.isArray(messages) ? messages[0] : messages; if (message) normalizedErrors[key] = message; }); if (validator) validator.showErrors(normalizedErrors); renderValidationErrorsManually($form, normalizedErrors); focusFirstInvalidField($form); return true; }
    function renderValidationErrorsManually($form, errors) { Object.keys(errors).forEach(function (key) { const message = errors[key]; const $message = $form.find('[data-valmsg-for="' + escapeSelector(key) + '"]'); if ($message.length) $message.removeClass('field-validation-valid').addClass('field-validation-error').text(message); const $field = $form.find('[name="' + escapeSelector(key) + '"]'); if ($field.length) $field.addClass('input-validation-error is-invalid'); }); }
    function clearValidationErrors($form) { $form.find('.field-validation-error').removeClass('field-validation-error').addClass('field-validation-valid').text(''); $form.find('.input-validation-error, .is-invalid').removeClass('input-validation-error is-invalid'); }
    function focusFirstInvalidField($form) { const $field = $form.find('.input-validation-error, .is-invalid').first(); if (!$field.length) return; const $tabPane = $field.closest('.tab-pane'); if ($tabPane.length && !$tabPane.hasClass('active')) { const paneId = $tabPane.attr('id'); const tabButton = document.querySelector('[data-bs-target="#' + paneId + '"]'); if (tabButton && typeof bootstrap !== 'undefined') bootstrap.Tab.getOrCreateInstance(tabButton).show(); } setTimeout(function () { $field.trigger('focus'); }, 150); }
    function bindSwitches($container) { $container.find('.js-lookup-status-switch, .js-bool-switch').each(function () { updateSwitchLabel($(this)); }); $container.off('change.workflowSwitch').on('change.workflowSwitch', '.js-lookup-status-switch, .js-bool-switch', function () { updateSwitchLabel($(this)); }); }
    function updateSwitchLabel($switch) { const $label = $switch.closest('.form-switch').find('.js-lookup-status-label, .js-bool-label').first(); const isChecked = $switch.is(':checked'); if ($switch.hasClass('js-lookup-status-switch')) { $label.toggleClass('text-success-600', isChecked).toggleClass('text-danger-600', !isChecked).text(isChecked ? getText('active', 'Aktif') : getText('passive', 'Pasif')); return; } $label.toggleClass('text-success-600', isChecked).toggleClass('text-danger-600', !isChecked).text(isChecked ? getText('yes', 'Evet') : getText('no', 'Hayır')); }
    function normalizeGuid(value) { return String(value || '').toLowerCase(); }
    function showFirstTab($form) { const firstTab = $form.find('[data-bs-toggle="pill"]').get(0); if (firstTab && typeof bootstrap !== 'undefined') bootstrap.Tab.getOrCreateInstance(firstTab).show(); }
    function showModal(selector) { const el = document.querySelector(selector); if (!el || typeof bootstrap === 'undefined') return; (bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el)).show(); }
    function hideModal(selector) { const el = document.querySelector(selector); if (!el || typeof bootstrap === 'undefined') return; (bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el)).hide(); }
    function getCurrentCulture() { const segments = window.location.pathname.split('/').filter(Boolean); return segments.length > 0 ? segments[0] : ''; }
    function getText(key, fallback) { const texts = window.Symplify.TransactionStatuses.texts || window.Symplify.Texts || window.Symplify.texts || {}; return texts[key] || fallback; }
    function showSuccess(message) { if (window.Symplify.Ajax?.showSuccess) { window.Symplify.Ajax.showSuccess(message); return; } alert(message || getText('successTitle', 'Başarılı')); }
    function showError(response) { if (window.Symplify.Ajax?.showError) { window.Symplify.Ajax.showError(response); return; } alert(response?.responseJSON?.message || response?.message || getText('genericError', 'İşlem sırasında hata oluştu.')); }
    function escapeSelector(value) { return typeof $.escapeSelector === 'function' ? $.escapeSelector(String(value)) : String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|/@])/g, '\\$1'); }

    return { init: init };
})(jQuery);
