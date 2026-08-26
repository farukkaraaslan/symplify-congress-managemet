window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplateTransitions = window.Symplify.WorkflowTemplateTransitions || {};

window.Symplify.WorkflowTemplateTransitions.delete = (function ($) {
    'use strict';

    const selectors = {
        button: '.js-workflow-template-transition-delete-button',
        form: '#deleteWorkflowTemplateTransitionForm'
    };

    function init() {
        $(document).off('click.workflowTemplateTransitionsDelete', selectors.button)
            .on('click.workflowTemplateTransitionsDelete', selectors.button, handleDelete);
    }

    function handleDelete() {
        const id = $(this).data('id');

        if (!id) {
            return;
        }

        window.Symplify.Ajax.confirm().then(function (result) {
            if (!result || result.isConfirmed !== true) {
                return;
            }

            const $form = $(selectors.form);

            $.ajax({
                url: $form.attr('action'),
                type: 'POST',
                data: { id: id },
                headers: window.Symplify.Ajax.buildTokenHeader($form)
            }).done(function (response) {
                if (!response || response.success !== true) {
                    window.Symplify.Ajax.showError(response);
                    return;
                }

                window.Symplify.Ajax.showSuccess(response.message);
                window.Symplify.WorkflowTemplateTransitions.table.reload(false);
            }).fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
        });
    }

    return { init: init };
})(jQuery);
