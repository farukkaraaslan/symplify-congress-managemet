window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplateTransitions = window.Symplify.WorkflowTemplateTransitions || {};

window.Symplify.WorkflowTemplateTransitions.update = (function ($) {
    'use strict';

    const selectors = {
        button: '.js-workflow-template-transition-update-button',
        form: '#updateWorkflowTemplateTransitionForm',
        modal: '#updateWorkflowTemplateTransitionModal'
    };

    function init() {
        initializeForm($(selectors.form));
        bindStatusSwitch($(document));

        $(document).off('click.workflowTemplateTransitionsUpdate', selectors.button)
            .on('click.workflowTemplateTransitionsUpdate', selectors.button, handleOpen);

        $(document).off('submit.workflowTemplateTransitionsUpdate', selectors.form)
            .on('submit.workflowTemplateTransitionsUpdate', selectors.form, handleSubmit);

        $(document).off('shown.bs.modal.workflowTemplateTransitionsUpdate', selectors.modal)
            .on('shown.bs.modal.workflowTemplateTransitionsUpdate', selectors.modal, function () {
                initializeForm($(selectors.form));
            });

        $(document).off('hidden.bs.modal.workflowTemplateTransitionsUpdate', selectors.modal)
            .on('hidden.bs.modal.workflowTemplateTransitionsUpdate', selectors.modal, function () {
                clearValidationErrors($(selectors.form));
            });
    }

    function handleOpen() {
        const $button = $(this);
        const $form = $(selectors.form);
        const isActive = String($button.data('is-active')).toLowerCase() === 'true';

        prepareForm($form);
        $form.find('[name="Id"]').val($button.data('id') || '');
        $form.find('[name="WorkflowTemplateId"]').val(window.Symplify.WorkflowTemplateTransitions.workflowTemplateId || $('#workflowTemplateTransitionsTable').data('workflow-template-id') || '');
        $form.find('[name="TransactionStatusTransitionId"]').val($button.data('transition-id') || '');
        $form.find('.js-lookup-status-switch').prop('checked', isActive).trigger('change');

        showModal(selectors.modal);
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
                    if (renderValidationErrors($form, response)) return;
                    window.Symplify.Ajax.showError(response);
                    return;
                }

                window.Symplify.Ajax.showSuccess(response.message);
                hideModal(selectors.modal);
                window.Symplify.WorkflowTemplateTransitions.table.reload(false);
            })
            .fail(function (xhr) {
                if (renderValidationErrors($form, xhr)) return;
                window.Symplify.Ajax.showError(xhr);
            });
    }

    function initializeForm($form) {
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

        return false;
    }

    function clearValidationErrors($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.clearValidationErrors === 'function') {
            window.Symplify.Forms.clearValidationErrors($form);
        }
    }

    function focusFirstInvalidField($form) {
        if (window.Symplify.Forms && typeof window.Symplify.Forms.focusFirstInvalidField === 'function') {
            window.Symplify.Forms.focusFirstInvalidField($form);
        }
    }

    function bindStatusSwitch($container) {
        $container.find('.js-lookup-status-switch').each(function () {
            updateStatusLabel($(this));
        });

        $container
            .off('change.workflowTemplateTransitionStatus')
            .on('change.workflowTemplateTransitionStatus', '.js-lookup-status-switch', function () {
                updateStatusLabel($(this));
            });
    }

    function updateStatusLabel($switch) {
        const $label = $switch.closest('.form-switch').find('.js-lookup-status-label').first();
        const isActive = $switch.is(':checked');

        $label
            .toggleClass('text-success-600', isActive)
            .toggleClass('text-danger-600', !isActive)
            .text(isActive ? getText('active', 'Aktif') : getText('passive', 'Pasif'));
    }

    function showModal(selector) {
        const element = document.querySelector(selector);

        if (!element || typeof bootstrap === 'undefined') {
            $(selector).modal('show');
            return;
        }

        const instance = bootstrap.Modal.getInstance(element) || new bootstrap.Modal(element);
        instance.show();
    }

    function hideModal(selector) {
        const element = document.querySelector(selector);

        if (!element || typeof bootstrap === 'undefined') {
            $(selector).modal('hide');
            return;
        }

        const instance = bootstrap.Modal.getInstance(element) || new bootstrap.Modal(element);
        instance.hide();
    }

    function getText(key, fallback) {
        if (window.Symplify && typeof window.Symplify.t === 'function') {
            return window.Symplify.t('Common.' + key, fallback);
        }

        return fallback;
    }

    return { init: init };
})(jQuery);
