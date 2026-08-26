window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.create = (function ($) {
    'use strict';

    const selectors = {
        form: '#createWorkflowTemplateForm',
        modal: '#createWorkflowTemplateModal'
    };

    function init() {
        const $form = $(selectors.form);
        initializeForm($form);
        bindStatusSwitch($form);

        $(document).off('submit.workflowTemplatesCreate', selectors.form)
            .on('submit.workflowTemplatesCreate', selectors.form, handleSubmit);

        $(document)
            .off('shown.bs.modal.workflowTemplatesCreate', selectors.modal)
            .on('shown.bs.modal.workflowTemplatesCreate', selectors.modal, function () {
                const $modalForm = $(selectors.form);
                initializeForm($modalForm);
                bindStatusSwitch($modalForm);
            });

        $(document)
            .off('hidden.bs.modal.workflowTemplatesCreate', selectors.modal)
            .on('hidden.bs.modal.workflowTemplatesCreate', selectors.modal, function () {
                clearValidationErrors($(selectors.form));
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
                resetForm($form);
                bindStatusSwitch($form);
                $(selectors.modal).modal('hide');
                window.Symplify.WorkflowTemplates.table.reload(true);
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) {
                    return;
                }

                window.Symplify.Ajax.showError(xhr);
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

    function resetForm($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.reset === 'function') {
            window.Symplify.Forms.reset($form);
            return;
        }

        if ($form[0]) $form[0].reset();
        clearValidationErrors($form);
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

    function escapeSelector(value) {
        if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
        return String(value).replace(/([ #;?%&,.+*~\':"!^$[\]()=>|\/@])/g, '\\$1');
    }

    return { init: init };
})(jQuery);
