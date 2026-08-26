window.Symplify = window.Symplify || {};
window.Symplify.WorkflowTemplates = window.Symplify.WorkflowTemplates || {};

(function ($) {
    'use strict';

    $(function () {
        window.Symplify.WorkflowTemplates.table?.init();
        window.Symplify.WorkflowTemplates.create?.init();
        window.Symplify.WorkflowTemplates.update?.init();
        window.Symplify.WorkflowTemplates.delete?.init();
    });
})(jQuery);
