window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.delete = (function ($) {
    'use strict';

    const selectors = {
        button: '.js-workflow-template-delete-button',
        form: '#deleteWorkflowTemplateForm'
    };

    function init() {
        $(document).off('click.workflowTemplatesDelete', selectors.button)
            .on('click.workflowTemplatesDelete', selectors.button, handleDelete);
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
                window.Symplify.WorkflowTemplates.table.reload(false);
            }).fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
        });
    }

    return { init: init };
})(jQuery);
