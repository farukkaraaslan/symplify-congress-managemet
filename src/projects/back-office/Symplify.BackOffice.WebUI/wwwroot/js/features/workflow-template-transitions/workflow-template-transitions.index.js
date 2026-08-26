window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplateTransitions = window.Symplify.WorkflowTemplateTransitions || {};

(function ($) {
    'use strict';

    $(function () {
        window.Symplify.WorkflowTemplateTransitions.table?.init();
        window.Symplify.WorkflowTemplateTransitions.create?.init();
        window.Symplify.WorkflowTemplateTransitions.update?.init();
        window.Symplify.WorkflowTemplateTransitions.delete?.init();
    });
})(jQuery);
