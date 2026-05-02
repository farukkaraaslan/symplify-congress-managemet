window.Symplify = window.Symplify || {};
window.Symplify.SubmissionTypes = window.Symplify.SubmissionTypes || {};

$(function () {
    'use strict';

    if (window.Symplify.SubmissionTypes.Table) {
        window.Symplify.SubmissionTypes.Table.init();
    }

    if (window.Symplify.SubmissionTypes.Create) {
        window.Symplify.SubmissionTypes.Create.init();
    }

    if (window.Symplify.SubmissionTypes.Update) {
        window.Symplify.SubmissionTypes.Update.init();
    }

    if (window.Symplify.SubmissionTypes.Delete) {
        window.Symplify.SubmissionTypes.Delete.init();
    }
});
