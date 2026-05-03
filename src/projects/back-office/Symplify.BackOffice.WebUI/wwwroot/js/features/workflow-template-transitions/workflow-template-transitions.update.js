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
        $(document).off('click.workflowTemplateTransitionsUpdate', selectors.button)
            .on('click.workflowTemplateTransitionsUpdate', selectors.button, handleOpen);

        $(document).off('submit.workflowTemplateTransitionsUpdate', selectors.form)
            .on('submit.workflowTemplateTransitionsUpdate', selectors.form, handleSubmit);
    }

    function handleOpen() {
        const $button = $(this);
        const $form = $(selectors.form);

        $form.find('[name="Id"]').val($button.data('id') || '');
        $form.find('[name="WorkflowTemplateId"]').val(window.Symplify.WorkflowTemplateTransitions.workflowTemplateId || $('#workflowTemplateTransitionsTable').data('workflow-template-id') || '');
        $form.find('[name="TransactionStatusTransitionId"]').val($button.data('transition-id') || '');
        $form.find('[name="IsActive"]').prop('checked', String($button.data('is-active')).toLowerCase() === 'true');

        $(selectors.modal).modal('show');
    }

    function handleSubmit(event) {
        event.preventDefault();

        const $form = $(this);

        if ($form.valid && !$form.valid()) {
            return;
        }

        window.Symplify.Ajax.postForm($form)
            .done(function (response) {
                if (!response || response.success !== true) {
                    window.Symplify.Ajax.showError(response);
                    return;
                }

                window.Symplify.Ajax.showSuccess(response.message);
                $(selectors.modal).modal('hide');
                window.Symplify.WorkflowTemplateTransitions.table.reload(false);
            })
            .fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
    }

    return { init: init };
})(jQuery);
