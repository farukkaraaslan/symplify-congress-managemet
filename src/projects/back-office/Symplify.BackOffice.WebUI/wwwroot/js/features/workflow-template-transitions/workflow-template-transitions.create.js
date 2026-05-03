window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplateTransitions = window.Symplify.WorkflowTemplateTransitions || {};

window.Symplify.WorkflowTemplateTransitions.create = (function ($) {
    'use strict';

    const selectors = {
        form: '#createWorkflowTemplateTransitionForm',
        modal: '#createWorkflowTemplateTransitionModal'
    };

    function init() {
        $(document).off('submit.workflowTemplateTransitionsCreate', selectors.form)
            .on('submit.workflowTemplateTransitionsCreate', selectors.form, handleSubmit);
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
                window.Symplify.WorkflowTemplateTransitions.table.reload(true);
            })
            .fail(function (xhr) {
                window.Symplify.Ajax.showError(xhr);
            });
    }

    return { init: init };
})(jQuery);
