window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

window.Symplify.WorkflowTemplates.create = (function ($) {
    'use strict';

    const selectors = {
        form: '#createWorkflowTemplateForm',
        modal: '#createWorkflowTemplateModal'
    };

    function init() {
        $(document).off('submit.workflowTemplatesCreate', selectors.form)
            .on('submit.workflowTemplatesCreate', selectors.form, handleSubmit);
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
                $form[0].reset();
                $(selectors.modal).modal('hide');
                window.Symplify.WorkflowTemplates.table.reload(true);
            })
            .fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
    }

    return { init: init };
})(jQuery);
