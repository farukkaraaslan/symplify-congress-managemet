window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.update = (function ($) {
    'use strict';

    const selectors = {
        button: '.js-workflow-template-update-button',
        form: '#updateWorkflowTemplateForm',
        modal: '#updateWorkflowTemplateModal'
    };

    function init() {
        const $form = $(selectors.form);
        initializeForm($form);
        bindStatusSwitch($form);

        $(document).off('click.workflowTemplatesUpdate', selectors.button)
            .on('click.workflowTemplatesUpdate', selectors.button, handleOpen);

        $(document).off('submit.workflowTemplatesUpdate', selectors.form)
            .on('submit.workflowTemplatesUpdate', selectors.form, handleSubmit);

        $(document)
            .off('shown.bs.modal.workflowTemplatesUpdate', selectors.modal)
            .on('shown.bs.modal.workflowTemplatesUpdate', selectors.modal, function () {
                const $modalForm = $(selectors.form);
                initializeForm($modalForm);
                bindStatusSwitch($modalForm);
            });

        $(document)
            .off('hidden.bs.modal.workflowTemplatesUpdate', selectors.modal)
            .on('hidden.bs.modal.workflowTemplatesUpdate', selectors.modal, function () {
                clearValidationErrors($(selectors.form));
            });
    }

    function handleOpen() {
        const id = $(this).data('id');
        const getForUpdateUrl = $('#workflowTemplatesTable').data('get-for-update-url') || window.Symplify.WorkflowTemplates?.urls?.getForUpdate;

        if (!id || !getForUpdateUrl) {
            return;
        }

        $.ajax({
            url: getForUpdateUrl,
            type: 'GET',
            data: { id: id },
            headers: getAjaxHeaders($(document))
        }).done(function (response) {
            if (!response || response.success !== true) {
                window.Symplify.Ajax.showError(response);
                return;
            }

            fillForm(response);
            clearValidationErrors($(selectors.form));
            $(selectors.modal).modal('show');
        }).fail(function (xhr) {
            window.Symplify.Ajax.showError(xhr);
        });
    }

    function handleSubmit(event) {
        event.preventDefault();

        const $form = $(this);
        prepareForm($form);

        if ($form.valid && !$form.valid()) {
            focusFirstInvalidField($form);
            return;
        }

        window.Symplify.Ajax.postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    if (renderValidationErrors($form, response)) {
                        return;
                    }

                    window.Symplify.Ajax.showError(response);
                    return;
                }

                window.Symplify.Ajax.showSuccess(response.message);
                $(selectors.modal).modal('hide');
                window.Symplify.WorkflowTemplates.table.reload(false);
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) {
                    return;
                }

                window.Symplify.Ajax.showError(xhr);
            });
    }

    function fillForm(response) {
        const $form = $(selectors.form);

        $form.find('[name="Id"]').val(response.id || '');
        $form.find('[name="Code"]').val(response.code || '');
        $form.find('[name="InitialTransactionStatusId"]').val(response.initialTransactionStatusId || '');
        $form.find('[name="IsDefault"]').prop('checked', response.isDefault === true);
        $form.find('[name="IsActive"]').prop('checked', response.isActive === true).trigger('change');

        (response.translations || []).forEach(function (translation, index) {
            $form.find('[name="Translations[' + index + '].LanguageId"]').val(translation.languageId || '');
            $form.find('[name="Translations[' + index + '].Culture"]').val(translation.culture || '');
            $form.find('[name="Translations[' + index + '].LanguageName"]').val(translation.languageName || '');
            $form.find('[name="Translations[' + index + '].IsDefault"]').val(String(translation.isDefault === true));
            $form.find('[name="Translations[' + index + '].Exists"]').val(String(translation.exists === true));
            $form.find('[name="Translations[' + index + '].Name"]').val(translation.name || '');
            $form.find('[name="Translations[' + index + '].Description"]').val(translation.description || '');
        });
    }

    function initializeForm($form) {
        if (!$form.length) return;

        if (window.Symplify.Forms && typeof window.Symplify.Forms.initialize === 'function') {
            window.Symplify.Forms.initialize($form);
        }
    }

    function prepareForm($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.prepareForSubmit === 'function') {
            window.Symplify.Forms.prepareForSubmit($form);
            return;
        }

        clearValidationErrors($form);
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

            $form.find('[data-valmsg-for="' + escapeSelector(fieldName) + '"]')
                .removeClass('field-validation-valid')
                .addClass('field-validation-error')
                .text(message);

            $form.find('[name="' + escapeSelector(fieldName) + '"]')
                .addClass('input-validation-error is-invalid');
        });

        focusFirstInvalidField($form);
        return true;
    }

    function clearValidationErrors($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.clearValidationErrors === 'function') {
            window.Symplify.Forms.clearValidationErrors($form);
            return;
        }

        $form.find('.field-validation-error')
            .removeClass('field-validation-error')
            .addClass('field-validation-valid')
            .empty();

        $form.find('.input-validation-error, .is-invalid')
            .removeClass('input-validation-error is-invalid');
    }

    function focusFirstInvalidField($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.focusFirstInvalidField === 'function') {
            window.Symplify.Forms.focusFirstInvalidField($form);
            return;
        }

        const $field = $form.find('.input-validation-error, .is-invalid').first();
        if ($field.length) $field.trigger('focus');
    }

    function bindStatusSwitch($container) {
        $container.find('.js-lookup-status-switch').each(function () {
            updateStatusLabel($(this));
        });

        $container
            .off('change.workflowTemplateStatus')
            .on('change.workflowTemplateStatus', '.js-lookup-status-switch', function () {
                updateStatusLabel($(this));
            });
    }

    function updateStatusLabel($switch) {
        const $label = $switch.closest('.form-switch').find('.js-lookup-status-label');
        const isActive = $switch.is(':checked');

        $label
            .toggleClass('text-success-600', isActive)
            .toggleClass('text-danger-600', !isActive)
            .text(isActive ? getText('active', 'Aktif') : getText('passive', 'Pasif'));
    }

    function getText(key, fallback) {
        const texts = window.Symplify.WorkflowTemplates.texts || window.Symplify.Texts || window.Symplify.texts || {};
        return texts[key] || fallback;
    }

    function getAjaxHeaders($container) {
        const headers = { 'X-Culture': getCurrentCulture() };
        const token = window.Symplify.Ajax?.getAntiForgeryToken
            ? window.Symplify.Ajax.getAntiForgeryToken($container || $(document))
            : $('input[name="__RequestVerificationToken"]').first().val();

        if (token) {
            headers.RequestVerificationToken = token;
        }

        return headers;
    }

    function getCurrentCulture() {
        const segments = window.location.pathname.split('/').filter(Boolean);
        return segments.length > 0 ? segments[0] : '';
    }

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|\/@])/g, '\\$1');
    }

    return { init: init };
})(jQuery);
